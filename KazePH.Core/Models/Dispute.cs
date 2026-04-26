using KazePH.Core.Enums;

namespace KazePH.Core.Models;

/// <summary>
/// Represents a dispute filed by a participant against an event outcome.
/// </summary>
public class Dispute
{
    /// <summary>Primary key.</summary>
    public Guid Id { get; set; }

    /// <summary>Foreign key to the disputed <see cref="Event"/>.</summary>
    public Guid EventId { get; set; }

    /// <summary>Foreign key to the user who opened the dispute.</summary>
    public string OpenedByUserId { get; set; } = string.Empty;

    /// <summary>Current status of the dispute investigation.</summary>
    public DisputeStatus Status { get; set; } = DisputeStatus.Open;

    /// <summary>Admin's note or verdict explaining the resolution.</summary>
    public string? AdminNote { get; set; }

    /// <summary>Date and time the dispute was resolved (UTC); null if still open.</summary>
    public DateTime? ResolvedAt { get; set; }

    /// <summary>Date and time the dispute was filed (UTC).</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Navigation property to the disputed event.</summary>
    public Event? Event { get; set; }

    /// <summary>Navigation property to the user who filed the dispute.</summary>
    public User? OpenedByUser { get; set; }

    /// <summary>All pieces of evidence submitted for this dispute.</summary>
    public ICollection<DisputeEvidence> Evidence { get; set; } = new List<DisputeEvidence>();
}
