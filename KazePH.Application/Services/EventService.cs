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
            Status = EventStatus.Draft,
            CreatedAt = DateTime.UtcNow
        };

        _db.Events.Add(ev);
        await _db.SaveChangesAsync(cancellationToken);
        return ev;
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
