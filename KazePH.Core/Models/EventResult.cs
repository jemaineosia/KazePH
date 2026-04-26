namespace KazePH.Core.Models;

/// <summary>
/// Records the declared outcome of an event, including optional admin verification.
/// </summary>
public class EventResult
{
    /// <summary>Primary key.</summary>
    public Guid Id { get; set; }

    /// <summary>Foreign key to the resolved <see cref="Event"/>.</summary>
    public Guid EventId { get; set; }

    /// <summary>The winning side as declared by the event creator or admin.</summary>
    public string DeclaredWinningSide { get; set; } = string.Empty;

    /// <summary>URL of the proof upload in Supabase Storage (screenshot, photo, etc.).</summary>
    public string? ProofUrl { get; set; }

    /// <summary>Date and time the result was declared (UTC).</summary>
    public DateTime DeclaredAt { get; set; } = DateTime.UtcNow;

    /// <summary>Whether an admin has reviewed and confirmed the result.</summary>
    public bool AdminVerified { get; set; }

    /// <summary>Navigation property to the parent event.</summary>
    public Event? Event { get; set; }
}
