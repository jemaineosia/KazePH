using KazePH.Core.Enums;
using KazePH.Core.Models;

namespace KazePH.Application.Interfaces;

/// <summary>
/// Manages betting event creation and lifecycle operations.
/// </summary>
public interface IEventService
{
    /// <summary>
    /// Creates a new betting event. For 1v1 events, the creator's stake is locked immediately
    /// and a BetEntry is created for the creator.
    /// </summary>
    /// <param name="creatorSide">"A" or "B" — which side the creator is betting on (1v1 only).</param>
    /// <param name="creatorStake">Amount the creator is staking (1v1 only).</param>
    /// <param name="opponentStake">Amount the opponent must stake to accept (1v1 only).</param>
    /// <param name="challengedUserId">Optional: direct challenge to a specific user's identity ID.</param>
    /// <param name="challengedUsername">Optional: username of the challenged user for display.</param>
    /// <param name="minStake">Optional minimum bet per entry (Pool only).</param>
    Task<Event> CreateEventAsync(
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
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Accepts an open 1v1 challenge. Locks the opponent's required stake and sets the event to Active.
    /// </summary>
    /// <param name="eventId">The 1v1 event to accept.</param>
    /// <param name="opponentId">Identity ID of the user accepting the challenge.</param>
    Task AcceptChallengeAsync(Guid eventId, string opponentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Rejects an open 1v1 challenge. Cancels the event and returns the creator's locked stake.
    /// Only the specifically challenged user may reject a direct challenge.
    /// For open challenges, any eligible (non-creator) user may reject.
    /// </summary>
    /// <param name="eventId">The 1v1 event to reject.</param>
    /// <param name="rejectingUserId">Identity ID of the user declining the challenge.</param>
    Task RejectChallengeAsync(Guid eventId, string rejectingUserId, CancellationToken cancellationToken = default);

    /// <summary>Returns events, optionally filtered by status and/or type, newest first.</summary>
    Task<List<Event>> GetEventsAsync(
        EventStatus? status = null,
        EventType? type = null,
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

    /// <summary>
    /// Submits one player's declaration of the result for a 1v1 event.
    /// Once both players have declared, the system auto-resolves:
    /// <list type="bullet">
    ///   <item>Both agree on same winner → payouts settled, event Completed.</item>
    ///   <item>Both claim they won → event escalated to Disputed, admin reviews.</item>
    ///   <item>Both claim they lost → draw, all stakes returned, event Completed.</item>
    /// </list>
    /// </summary>
    Task SubmitDeclarationAsync(Guid eventId, string userId, string declaredWinningSide, CancellationToken cancellationToken = default);

    /// <summary>
    /// Declares the winning side for an active event, settles all payouts atomically,
    /// and marks the event as Completed. Only the event creator may call this.
    /// Winners receive their stake back plus a proportional share of the losing pool.
    /// </summary>
    /// <param name="eventId">The active event to settle.</param>
    /// <param name="declaringUserId">Identity ID of the user declaring the result (must be creator).</param>
    /// <param name="winningSide">"A" or "B" — the side that won.</param>
    Task DeclareResultAsync(Guid eventId, string declaringUserId, string winningSide, CancellationToken cancellationToken = default);
}
