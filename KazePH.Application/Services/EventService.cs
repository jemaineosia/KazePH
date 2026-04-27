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
              .Include(e => e.BetEntries)
              .FirstOrDefaultAsync(e => e.Id == eventId, cancellationToken);

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
            await _walletService.ReleaseFundsAsync(entry.UserId, entry.Amount, cancellationToken);

        ev.Status = EventStatus.Cancelled;
        await _db.SaveChangesAsync(cancellationToken);
    }
}
