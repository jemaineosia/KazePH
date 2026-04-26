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

    /// <summary>Foreign key to the user who submitted this evidence.</summary>
    public string SubmittedByUserId { get; set; } = string.Empty;

    /// <summary>URL of the evidence file in Supabase Storage.</summary>
    public string EvidenceUrl { get; set; } = string.Empty;

    /// <summary>Optional written description or context provided with the evidence.</summary>
    public string? Description { get; set; }

    /// <summary>Date and time the evidence was submitted (UTC).</summary>
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Navigation property to the parent dispute.</summary>
    public Dispute? Dispute { get; set; }

    /// <summary>Navigation property to the submitting user.</summary>
    public User? SubmittedByUser { get; set; }
}
