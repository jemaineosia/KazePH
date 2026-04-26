using KazePH.Core.Enums;
using KazePH.Core.Models;

namespace KazePH.Application.Interfaces;

/// <summary>
/// Manages betting event creation and lifecycle operations.
/// </summary>
public interface IEventService
{
    /// <summary>
    /// Creates a new betting event in <see cref="EventStatus.Draft"/> status.
    /// </summary>
    /// <param name="creatorId">Identity ID of the user creating the event.</param>
    /// <param name="title">Short title for the event.</param>
    /// <param name="description">Detailed description of what is being bet on.</param>
    /// <param name="eventDate">Scheduled date and time of the event (UTC).</param>
    /// <param name="eventType">Whether this is a 1v1 or pool event.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The newly created <see cref="Event"/>.</returns>
    Task<Event> CreateEventAsync(
        string creatorId,
        string title,
        string description,
        DateTime eventDate,
        EventType eventType,
        CancellationToken cancellationToken = default);

    /// <summary>Retrieves a single event by its primary key.</summary>
    /// <param name="eventId">The event's primary key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The event, or <c>null</c> if not found.</returns>
    Task<Event?> GetEventAsync(Guid eventId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels an event and returns any locked funds to participants.
    /// For 1v1 events, both parties must have agreed before calling this method.
    /// </summary>
    /// <param name="eventId">The event's primary key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task CancelEventAsync(Guid eventId, CancellationToken cancellationToken = default);
}
