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

    /// <summary>
    /// Posts an admin message into the dispute thread (visible to both parties).
    /// Admin messages are stored as <see cref="KazePH.Core.Models.DisputeEvidence"/> records
    /// with <c>IsAdminMessage = true</c>.
    /// </summary>
    /// <param name="disputeId">The dispute to post into.</param>
    /// <param name="adminUserId">Identity ID of the admin posting.</param>
    /// <param name="message">Thread message body.</param>
    /// <param name="attachmentUrl">Optional URL to an attached file or screenshot.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<DisputeEvidence> AddAdminMessageAsync(
        Guid disputeId,
        string adminUserId,
        string message,
        string? attachmentUrl = null,
        CancellationToken cancellationToken = default);
}
