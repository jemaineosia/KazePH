using KazePH.Application.Interfaces;
using KazePH.Core.Enums;
using KazePH.Core.Models;
using KazePH.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KazePH.Application.Services;

/// <summary>
/// Implements <see cref="IEventService"/> using <see cref="KazeDbContext"/>.
/// </summary>
public class EventService : IEventService
{
    private readonly KazeDbContext _db;
    private readonly IWalletService _walletService;

    /// <summary>Initializes a new instance of <see cref="EventService"/>.</summary>
    public EventService(KazeDbContext db, IWalletService walletService)
    {
        _db = db;
        _walletService = walletService;
    }

    /// <inheritdoc />
    public async Task<Event> CreateEventAsync(
        string creatorId,
        string title,
        string description,
        DateTime eventDate,
        EventType eventType,
        string sideA,
        string sideB,
        string? creatorSide = null,
        decimal? creatorStake = null,
        decimal? opponentStake = null,
        string? challengedUserId = null,
        string? challengedUsername = null,
        decimal? minStake = null,
        CancellationToken cancellationToken = default)
    {
        var ev = new Event
        {
            Id = Guid.NewGuid(),
            CreatorId = creatorId,
            Title = title,
            Description = description,
            EventDate = eventDate,
            EventType = eventType,
            SideA = string.IsNullOrWhiteSpace(sideA) ? "Side A" : sideA.Trim(),
            SideB = string.IsNullOrWhiteSpace(sideB) ? "Side B" : sideB.Trim(),
            Status = EventStatus.Open,
            CreatedAt = DateTime.UtcNow
        };

        if (eventType == EventType.OneVsOne)
        {
            if (string.IsNullOrWhiteSpace(creatorSide) || creatorStake is null or <= 0 || opponentStake is null or <= 0)
                throw new InvalidOperationException("1v1 events require a creator side, creator stake, and opponent stake.");

            ev.CreatorSide        = creatorSide;
            ev.CreatorStake       = creatorStake;
            ev.OpponentStake      = opponentStake;
            ev.StakeAmount        = creatorStake; // for display/compat
            ev.ChallengedUserId   = string.IsNullOrWhiteSpace(challengedUserId)   ? null : challengedUserId;
            ev.ChallengedUsername = string.IsNullOrWhiteSpace(challengedUsername) ? null : challengedUsername.Trim();

            // Wrap everything in a transaction: if saving the event or bet entry fails,
            // the wallet lock is rolled back automatically — no orphaned locked funds.
            await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                await _walletService.LockFundsAsync(creatorId, creatorStake.Value, cancellationToken);
                _walletService.Log(creatorId, WalletTransactionType.StakeLocked, creatorStake.Value,
                    $"Stake locked: {ev.Title}", ev.Id);

                _db.Events.Add(ev);
                await _db.SaveChangesAsync(cancellationToken);

                _db.BetEntries.Add(new BetEntry
                {
                    Id        = Guid.NewGuid(),
                    EventId   = ev.Id,
                    UserId    = creatorId,
                    Side      = creatorSide,
                    Amount    = creatorStake.Value,
                    CreatedAt = DateTime.UtcNow
                });
                await _db.SaveChangesAsync(cancellationToken);

                await tx.CommitAsync(cancellationToken);
            }
            catch
            {
                await tx.RollbackAsync(cancellationToken);
                throw;
            }
        }
        else
        {
            ev.StakeAmount = minStake > 0 ? minStake : null;
            _db.Events.Add(ev);
            await _db.SaveChangesAsync(cancellationToken);
        }

        return ev;
    }

    /// <inheritdoc />
    public async Task AcceptChallengeAsync(Guid eventId, string opponentId, CancellationToken cancellationToken = default)
    {
        var ev = await _db.Events
            .Include(e => e.BetEntries)
            .FirstOrDefaultAsync(e => e.Id == eventId, cancellationToken)
            ?? throw new InvalidOperationException("Event not found.");

        if (ev.EventType != EventType.OneVsOne)
            throw new InvalidOperationException("Only 1v1 events can be accepted this way.");
        if (ev.Status != EventStatus.Open)
            throw new InvalidOperationException("This challenge is no longer open.");
        if (ev.BetEntries.Count >= 2)
            throw new InvalidOperationException("This challenge already has both players.");
        if (ev.CreatorId == opponentId)
            throw new InvalidOperationException("You cannot accept your own challenge.");
        if (ev.ChallengedUserId is not null && ev.ChallengedUserId != opponentId)
            throw new InvalidOperationException("This challenge was sent to a specific user and cannot be accepted by you.");

        var opponentSide = ev.CreatorSide == "A" ? "B" : "A";
        var opponentStake = ev.OpponentStake
            ?? throw new InvalidOperationException("Opponent stake amount is not set.");

        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await _walletService.LockFundsAsync(opponentId, opponentStake, cancellationToken);
            _walletService.Log(opponentId, WalletTransactionType.StakeLocked, opponentStake,
                $"Stake locked: {ev.Title}", ev.Id);

            _db.BetEntries.Add(new BetEntry
            {
                Id        = Guid.NewGuid(),
                EventId   = ev.Id,
                UserId    = opponentId,
                Side      = opponentSide,
                Amount    = opponentStake,
                CreatedAt = DateTime.UtcNow
            });

            ev.Status = EventStatus.Active;
            await _db.SaveChangesAsync(cancellationToken);

            await tx.CommitAsync(cancellationToken);
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }

    /// <inheritdoc />
    public Task<List<Event>> GetEventsAsync(
        EventStatus? status = null,
        EventType? type = null,
        CancellationToken cancellationToken = default)
    {
        var q = _db.Events
            .Include(e => e.Creator)
            .Include(e => e.BetEntries)
            .AsQueryable();

        if (status.HasValue) q = q.Where(e => e.Status == status.Value);
        if (type.HasValue)   q = q.Where(e => e.EventType == type.Value);

        return q.OrderByDescending(e => e.CreatedAt).ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<Event?> GetEventAsync(Guid eventId, CancellationToken cancellationToken = default)
        => _db.Events
              .Include(e => e.Creator)
              .Include(e => e.BetEntries).ThenInclude(b => b.User)
              .Include(e => e.Result)
              .Include(e => e.ResultDeclarations)
              .FirstOrDefaultAsync(e => e.Id == eventId, cancellationToken);

    /// <inheritdoc />
    public async Task SubmitDeclarationAsync(
        Guid eventId,
        string userId,
        string declaredWinningSide,
        CancellationToken cancellationToken = default)
    {
        var ev = await _db.Events
            .Include(e => e.BetEntries)
            .Include(e => e.ResultDeclarations)
            .FirstOrDefaultAsync(e => e.Id == eventId, cancellationToken)
            ?? throw new InvalidOperationException("Event not found.");

        if (ev.EventType != EventType.OneVsOne)
            throw new InvalidOperationException("Only 1v1 events use the dual-declaration flow.");
        if (ev.Status != EventStatus.Active)
            throw new InvalidOperationException("Can only submit a declaration for an active event.");
        if (declaredWinningSide != "A" && declaredWinningSide != "B")
            throw new InvalidOperationException("Declared winning side must be 'A' or 'B'.");

        var myEntry = ev.BetEntries.FirstOrDefault(b => b.UserId == userId)
            ?? throw new InvalidOperationException("You are not a participant in this event.");
        if (ev.ResultDeclarations.Any(d => d.DeclaringUserId == userId))
            throw new InvalidOperationException("You have already submitted your declaration.");

        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            _db.ResultDeclarations.Add(new ResultDeclaration
            {
                Id                  = Guid.NewGuid(),
                EventId             = ev.Id,
                DeclaringUserId     = userId,
                DeclaredWinningSide = declaredWinningSide,
                DeclaredAt          = DateTime.UtcNow
            });
            await _db.SaveChangesAsync(cancellationToken);

            // Re-read declarations now that ours is saved
            var allDeclarations = await _db.ResultDeclarations
                .Where(d => d.EventId == ev.Id)
                .ToListAsync(cancellationToken);

            if (allDeclarations.Count == 2)
            {
                var decA = allDeclarations.First(d =>
                    ev.BetEntries.First(b => b.UserId == d.DeclaringUserId).Side == "A");
                var decB = allDeclarations.First(d =>
                    ev.BetEntries.First(b => b.UserId == d.DeclaringUserId).Side == "B");

                if (decA.DeclaredWinningSide == decB.DeclaredWinningSide)
                {
                    // Both agree — settle the winner
                    await SettleWinnerInternalAsync(ev, decA.DeclaredWinningSide, cancellationToken);
                }
                else if (decA.DeclaredWinningSide == "A" && decB.DeclaredWinningSide == "B")
                {
                    // Both claim victory — escalate to dispute
                    _db.Disputes.Add(new Dispute
                    {
                        Id             = Guid.NewGuid(),
                        EventId        = ev.Id,
                        OpenedByUserId = ev.CreatorId,
                        Status         = DisputeStatus.Open,
                        CreatedAt      = DateTime.UtcNow
                    });
                    ev.Status = EventStatus.Disputed;
                    await _db.SaveChangesAsync(cancellationToken);
                }
                else
                {
                    // Both declare they lost — draw
                    await SettleDrawInternalAsync(ev, cancellationToken);
                }
            }

            await tx.CommitAsync(cancellationToken);
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private async Task SettleWinnerInternalAsync(Event ev, string winningSide, CancellationToken ct)
    {
        var winners        = ev.BetEntries.Where(b => b.Side == winningSide).ToList();
        var losers         = ev.BetEntries.Where(b => b.Side != winningSide).ToList();
        decimal loserPot   = losers.Sum(b => b.Amount);
        decimal winnerPot  = winners.Sum(b => b.Amount);
        var winningSideName = winningSide == "A" ? ev.SideA : ev.SideB;
        var losingSideName  = winningSide == "A" ? ev.SideB : ev.SideA;

        foreach (var w in winners)
        {
            await _walletService.ReleaseFundsAsync(w.UserId, w.Amount, ct);
            _walletService.Log(w.UserId, WalletTransactionType.StakeReleased, w.Amount,
                $"Stake returned: {ev.Title}", ev.Id);
            if (loserPot > 0 && winnerPot > 0)
            {
                var share = Math.Round((w.Amount / winnerPot) * loserPot, 2, MidpointRounding.ToEven);
                await _walletService.CreditFundsAsync(w.UserId, share, ct);
                _walletService.Log(w.UserId, WalletTransactionType.WinningsReceived, share,
                    $"Won ({winningSideName}): {ev.Title}", ev.Id);
            }
        }

        foreach (var l in losers)
        {
            var wallet = await _db.Wallets.FirstOrDefaultAsync(w => w.UserId == l.UserId, ct)
                ?? throw new InvalidOperationException($"Wallet not found for user {l.UserId}.");
            wallet.LockedBalance -= l.Amount;
            wallet.UpdatedAt = DateTime.UtcNow;
            _walletService.Log(l.UserId, WalletTransactionType.StakeForfeited, l.Amount,
                $"Lost ({losingSideName}): {ev.Title}", ev.Id);
        }
        await _db.SaveChangesAsync(ct);

        _db.EventResults.Add(new EventResult
        {
            Id                  = Guid.NewGuid(),
            EventId             = ev.Id,
            DeclaredWinningSide = winningSide,
            DeclaredAt          = DateTime.UtcNow
        });
        ev.Status = EventStatus.Completed;
        await _db.SaveChangesAsync(ct);
    }

    private async Task SettleDrawInternalAsync(Event ev, CancellationToken ct)
    {
        foreach (var entry in ev.BetEntries)
        {
            await _walletService.ReleaseFundsAsync(entry.UserId, entry.Amount, ct);
            _walletService.Log(entry.UserId, WalletTransactionType.DrawRefund, entry.Amount,
                $"Draw refund: {ev.Title}", ev.Id);
        }

        _db.EventResults.Add(new EventResult
        {
            Id                  = Guid.NewGuid(),
            EventId             = ev.Id,
            DeclaredWinningSide = "Draw",
            DeclaredAt          = DateTime.UtcNow
        });
        ev.Status = EventStatus.Completed;
        await _db.SaveChangesAsync(ct);
    }

    /// <inheritdoc />
    public async Task DeclareResultAsync(
        Guid eventId,
        string declaringUserId,
        string winningSide,
        CancellationToken cancellationToken = default)
    {
        var ev = await _db.Events
            .Include(e => e.BetEntries)
            .FirstOrDefaultAsync(e => e.Id == eventId, cancellationToken)
            ?? throw new InvalidOperationException("Event not found.");

        if (ev.Status != EventStatus.Active)
            throw new InvalidOperationException("Only active events can have a result declared.");
        if (ev.CreatorId != declaringUserId)
            throw new InvalidOperationException("Only the event creator can declare the result.");
        if (winningSide != "A" && winningSide != "B")
            throw new InvalidOperationException("Winning side must be 'A' or 'B'.");

        var winners = ev.BetEntries.Where(b => b.Side == winningSide).ToList();
        var losers  = ev.BetEntries.Where(b => b.Side != winningSide).ToList();
        decimal loserPot  = losers.Sum(b => b.Amount);
        decimal winnerPot = winners.Sum(b => b.Amount);

        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            // Release each winner's stake and credit their proportional share of the loser pot
            var winningSideName2 = winningSide == "A" ? ev.SideA : ev.SideB;
            var losingSideName2  = winningSide == "A" ? ev.SideB : ev.SideA;
            foreach (var w in winners)
            {
                await _walletService.ReleaseFundsAsync(w.UserId, w.Amount, cancellationToken);
                _walletService.Log(w.UserId, WalletTransactionType.StakeReleased, w.Amount,
                    $"Stake returned: {ev.Title}", ev.Id);
                if (loserPot > 0 && winnerPot > 0)
                {
                    var share = Math.Round((w.Amount / winnerPot) * loserPot, 2, MidpointRounding.ToEven);
                    await _walletService.CreditFundsAsync(w.UserId, share, cancellationToken);
                    _walletService.Log(w.UserId, WalletTransactionType.WinningsReceived, share,
                        $"Won ({winningSideName2}): {ev.Title}", ev.Id);
                }
            }

            // Forfeit losers' locked stakes (deducted from locked balance)
            foreach (var l in losers)
            {
                var wallet = await _db.Wallets.FirstOrDefaultAsync(w => w.UserId == l.UserId, cancellationToken)
                    ?? throw new InvalidOperationException($"Wallet not found for user {l.UserId}.");
                wallet.LockedBalance -= l.Amount;
                wallet.UpdatedAt = DateTime.UtcNow;
                _walletService.Log(l.UserId, WalletTransactionType.StakeForfeited, l.Amount,
                    $"Lost ({losingSideName2}): {ev.Title}", ev.Id);
            }
            await _db.SaveChangesAsync(cancellationToken);

            _db.EventResults.Add(new EventResult
            {
                Id                  = Guid.NewGuid(),
                EventId             = ev.Id,
                DeclaredWinningSide = winningSide,
                DeclaredAt          = DateTime.UtcNow
            });
            ev.Status = EventStatus.Completed;
            await _db.SaveChangesAsync(cancellationToken);

            await tx.CommitAsync(cancellationToken);
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task CancelEventAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        var ev = await _db.Events
            .Include(e => e.BetEntries)
            .FirstOrDefaultAsync(e => e.Id == eventId, cancellationToken)
            ?? throw new InvalidOperationException($"Event '{eventId}' not found.");

        if (ev.Status is EventStatus.Completed or EventStatus.Cancelled)
            throw new InvalidOperationException($"Event '{eventId}' cannot be cancelled in its current status.");

        // Return locked funds to each participant
        foreach (var entry in ev.BetEntries)
        {
            await _walletService.ReleaseFundsAsync(entry.UserId, entry.Amount, cancellationToken);
            _walletService.Log(entry.UserId, WalletTransactionType.StakeReleased, entry.Amount,
                $"Cancelled: {ev.Title}", ev.Id);
        }

        ev.Status = EventStatus.Cancelled;
        await _db.SaveChangesAsync(cancellationToken);
    }
}
