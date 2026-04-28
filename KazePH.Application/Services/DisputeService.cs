using KazePH.Application.Interfaces;
using KazePH.Core.Enums;
using KazePH.Core.Models;
using KazePH.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KazePH.Application.Services;

/// <summary>
/// Implements <see cref="IDisputeService"/> using <see cref="KazeDbContext"/>.
/// </summary>
public class DisputeService : IDisputeService
{
    private readonly KazeDbContext _db;
    private readonly IWalletService _walletService;

    /// <summary>Initializes a new instance of <see cref="DisputeService"/>.</summary>
    public DisputeService(KazeDbContext db, IWalletService walletService)
    {
        _db = db;
        _walletService = walletService;
    }

    /// <inheritdoc />
    public async Task<Dispute> OpenDisputeAsync(Guid eventId, string openedByUserId, CancellationToken cancellationToken = default)
    {
        var ev = await _db.Events.FirstOrDefaultAsync(e => e.Id == eventId, cancellationToken)
            ?? throw new InvalidOperationException($"Event '{eventId}' not found.");

        ev.Status = EventStatus.Disputed;

        var dispute = new Dispute
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            OpenedByUserId = openedByUserId,
            Status = DisputeStatus.Open,
            CreatedAt = DateTime.UtcNow
        };

        _db.Disputes.Add(dispute);
        await _db.SaveChangesAsync(cancellationToken);
        return dispute;
    }

    /// <inheritdoc />
    public async Task<DisputeEvidence> SubmitEvidenceAsync(
        Guid disputeId,
        string submittedByUserId,
        string evidenceUrl,
        string? description,
        CancellationToken cancellationToken = default)
    {
        var dispute = await _db.Disputes.FirstOrDefaultAsync(d => d.Id == disputeId, cancellationToken)
            ?? throw new InvalidOperationException($"Dispute '{disputeId}' not found.");

        if (dispute.Status == DisputeStatus.Resolved)
            throw new InvalidOperationException("Cannot submit evidence to an already resolved dispute.");

        // Escalate to UnderReview when evidence first arrives
        if (dispute.Status == DisputeStatus.Open)
        {
            dispute.Status = DisputeStatus.UnderReview;
        }

        var evidence = new DisputeEvidence
        {
            Id = Guid.NewGuid(),
            DisputeId = disputeId,
            SubmittedByUserId = submittedByUserId,
            EvidenceUrl = evidenceUrl,
            Description = description,
            SubmittedAt = DateTime.UtcNow
        };

        _db.DisputeEvidence.Add(evidence);
        await _db.SaveChangesAsync(cancellationToken);
        return evidence;
    }

    /// <inheritdoc />
    public async Task ResolveDisputeAsync(Guid disputeId, string adminNote, string winningSide, CancellationToken cancellationToken = default)
    {
        var dispute = await _db.Disputes
            .Include(d => d.Event)
                .ThenInclude(e => e!.BetEntries)
            .Include(d => d.Event)
                .ThenInclude(e => e!.Result)
            .FirstOrDefaultAsync(d => d.Id == disputeId, cancellationToken)
            ?? throw new InvalidOperationException($"Dispute '{disputeId}' not found.");

        if (dispute.Status == DisputeStatus.Resolved)
            throw new InvalidOperationException("Dispute has already been resolved.");

        if (dispute.Event != null)
        {
            var ev = dispute.Event;

            if (winningSide == "Draw")
            {
                foreach (var entry in ev.BetEntries)
                {
                    await _walletService.ReleaseFundsAsync(entry.UserId, entry.Amount, cancellationToken);
                    _walletService.Log(entry.UserId, WalletTransactionType.DrawRefund, entry.Amount,
                        $"Draw refund (dispute resolved): {ev.Title}", ev.Id);
                }
            }
            else
            {
                var winners         = ev.BetEntries.Where(b => b.Side == winningSide).ToList();
                var losers          = ev.BetEntries.Where(b => b.Side != winningSide).ToList();
                decimal loserPot    = losers.Sum(b => b.Amount);
                decimal winnerPot   = winners.Sum(b => b.Amount);
                var winningSideName = winningSide == "A" ? ev.SideA : ev.SideB;
                var losingSideName  = winningSide == "A" ? ev.SideB : ev.SideA;

                foreach (var w in winners)
                {
                    await _walletService.ReleaseFundsAsync(w.UserId, w.Amount, cancellationToken);
                    _walletService.Log(w.UserId, WalletTransactionType.StakeReleased, w.Amount,
                        $"Stake returned (dispute): {ev.Title}", ev.Id);
                    if (loserPot > 0 && winnerPot > 0)
                    {
                        var share = Math.Round((w.Amount / winnerPot) * loserPot, 2, MidpointRounding.ToEven);
                        await _walletService.CreditFundsAsync(w.UserId, share, cancellationToken);
                        _walletService.Log(w.UserId, WalletTransactionType.WinningsReceived, share,
                            $"Won ({winningSideName}) via dispute: {ev.Title}", ev.Id);
                    }
                }

                foreach (var l in losers)
                {
                    var wallet = await _db.Wallets.FirstOrDefaultAsync(w => w.UserId == l.UserId, cancellationToken)
                        ?? throw new InvalidOperationException($"Wallet not found for user {l.UserId}.");
                    wallet.LockedBalance -= l.Amount;
                    wallet.UpdatedAt = DateTime.UtcNow;
                    _walletService.Log(l.UserId, WalletTransactionType.StakeForfeited, l.Amount,
                        $"Lost ({losingSideName}) via dispute: {ev.Title}", ev.Id);
                }
            }

            // Upsert the EventResult so the platform reflects the admin-verified outcome
            if (ev.Result != null)
            {
                ev.Result.DeclaredWinningSide = winningSide;
                ev.Result.AdminVerified       = true;
                ev.Result.DeclaredAt          = DateTime.UtcNow;
            }
            else
            {
                _db.EventResults.Add(new EventResult
                {
                    Id                  = Guid.NewGuid(),
                    EventId             = ev.Id,
                    DeclaredWinningSide = winningSide,
                    AdminVerified       = true,
                    DeclaredAt          = DateTime.UtcNow
                });
            }

            ev.Status = EventStatus.Completed;
        }

        dispute.Status = DisputeStatus.Resolved;
        dispute.AdminNote = adminNote;
        dispute.ResolvedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<DisputeEvidence> AddAdminMessageAsync(
        Guid disputeId,
        string adminUserId,
        string message,
        string? attachmentUrl = null,
        CancellationToken cancellationToken = default)
    {
        var dispute = await _db.Disputes.FirstOrDefaultAsync(d => d.Id == disputeId, cancellationToken)
            ?? throw new InvalidOperationException($"Dispute '{disputeId}' not found.");

        if (dispute.Status == DisputeStatus.Resolved)
            throw new InvalidOperationException("Cannot post to a resolved dispute.");

        // Escalate to UnderReview when admin first engages
        if (dispute.Status == DisputeStatus.Open)
            dispute.Status = DisputeStatus.UnderReview;

        var entry = new DisputeEvidence
        {
            Id                = Guid.NewGuid(),
            DisputeId         = disputeId,
            SubmittedByUserId = null,   // admin messages have no kaze_users FK
            IsAdminMessage    = true,
            Description       = message,
            EvidenceUrl       = attachmentUrl,
            SubmittedAt       = DateTime.UtcNow
        };

        _db.DisputeEvidence.Add(entry);
        await _db.SaveChangesAsync(cancellationToken);
        return entry;
    }
}
