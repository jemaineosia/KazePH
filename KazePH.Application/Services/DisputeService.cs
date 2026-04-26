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
            .FirstOrDefaultAsync(d => d.Id == disputeId, cancellationToken)
            ?? throw new InvalidOperationException($"Dispute '{disputeId}' not found.");

        if (dispute.Status == DisputeStatus.Resolved)
            throw new InvalidOperationException("Dispute has already been resolved.");

        // Release locked funds to winners
        if (dispute.Event != null)
        {
            var winners = dispute.Event.BetEntries.Where(b => b.Side == winningSide).ToList();
            var losers = dispute.Event.BetEntries.Where(b => b.Side != winningSide).ToList();

            decimal totalPot = dispute.Event.BetEntries.Sum(b => b.Amount);
            decimal winnersStake = winners.Sum(b => b.Amount);

            foreach (var winner in winners)
            {
                var share = winnersStake > 0
                    ? totalPot * (winner.Amount / winnersStake)
                    : 0;
                await _walletService.CreditFundsAsync(winner.UserId, share, cancellationToken);
            }

            // Release losers' locked funds (already taken from locked by bet entry)
            foreach (var loser in losers)
            {
                // Locked funds are already consumed; nothing to return for losers
            }

            dispute.Event.Status = EventStatus.Completed;
        }

        dispute.Status = DisputeStatus.Resolved;
        dispute.AdminNote = adminNote;
        dispute.ResolvedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
    }
}
