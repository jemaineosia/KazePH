using KazePH.Core.Models;

namespace KazePH.Application.Interfaces;

/// <summary>
/// Manages the dispute lifecycle: opening, evidence submission, and admin resolution.
/// </summary>
public interface IDisputeService
{
    /// <summary>
    /// Opens a dispute against the declared outcome of an event, changing the event status
    /// to <see cref="KazePH.Core.Enums.EventStatus.Disputed"/>.
    /// </summary>
    /// <param name="eventId">The event being disputed.</param>
    /// <param name="openedByUserId">Identity ID of the user raising the dispute.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created <see cref="Dispute"/>.</returns>
    Task<Dispute> OpenDisputeAsync(Guid eventId, string openedByUserId, CancellationToken cancellationToken = default);

    /// <summary>Adds an evidence record to an existing open dispute.</summary>
    /// <param name="disputeId">The dispute to attach evidence to.</param>
    /// <param name="submittedByUserId">Identity ID of the evidence submitter.</param>
    /// <param name="evidenceUrl">Supabase Storage URL of the evidence file.</param>
    /// <param name="description">Optional written context for the evidence.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created <see cref="DisputeEvidence"/>.</returns>
    Task<DisputeEvidence> SubmitEvidenceAsync(
        Guid disputeId,
        string submittedByUserId,
        string evidenceUrl,
        string? description,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Admin resolves the dispute by declaring the winning side and issuing any strikes
    /// to the party found to have acted in bad faith.
    /// </summary>
    /// <param name="disputeId">The dispute to resolve.</param>
    /// <param name="adminNote">Admin verdict and explanation.</param>
    /// <param name="winningSide">The declared winning side string.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ResolveDisputeAsync(Guid disputeId, string adminNote, string winningSide, CancellationToken cancellationToken = default);
}
