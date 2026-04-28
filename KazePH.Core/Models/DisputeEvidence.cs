namespace KazePH.Core.Models;

/// <summary>
/// A single piece of evidence submitted by a participant during a dispute investigation.
/// </summary>
public class DisputeEvidence
{
    /// <summary>Primary key.</summary>
    public Guid Id { get; set; }

    /// <summary>Foreign key to the parent <see cref="Dispute"/>.</summary>
    public Guid DisputeId { get; set; }

    /// <summary>Foreign key to the user who submitted this evidence. Null for admin-posted messages.</summary>
    public string? SubmittedByUserId { get; set; }

    /// <summary>URL of the evidence file in Supabase Storage; null for text-only messages.</summary>
    public string? EvidenceUrl { get; set; }

    /// <summary>Optional written description or context provided with the evidence.</summary>
    public string? Description { get; set; }

    /// <summary>True when this record is an admin-posted thread message rather than user-submitted evidence.</summary>
    public bool IsAdminMessage { get; set; }

    /// <summary>Date and time the evidence was submitted (UTC).</summary>
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Navigation property to the parent dispute.</summary>
    public Dispute? Dispute { get; set; }

    /// <summary>Navigation property to the submitting user.</summary>
    public User? SubmittedByUser { get; set; }
}
