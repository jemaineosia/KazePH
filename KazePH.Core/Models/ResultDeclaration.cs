namespace KazePH.Core.Models;

/// <summary>
/// Records one player's declaration of who won a 1v1 event.
/// When both players have declared, the system auto-resolves:
/// agreement → settle, both claim win → dispute, both claim loss → draw.
/// </summary>
public class ResultDeclaration
{
    /// <summary>Primary key.</summary>
    public Guid Id { get; set; }

    /// <summary>Foreign key to the 1v1 <see cref="Event"/>.</summary>
    public Guid EventId { get; set; }

    /// <summary>Identity ID of the player making this declaration.</summary>
    public string DeclaringUserId { get; set; } = string.Empty;

    /// <summary>"A" or "B" — which side this player declares as the winner.</summary>
    public string DeclaredWinningSide { get; set; } = string.Empty;

    /// <summary>When the declaration was submitted (UTC).</summary>
    public DateTime DeclaredAt { get; set; } = DateTime.UtcNow;

    /// <summary>Navigation property to the event.</summary>
    public Event? Event { get; set; }

    /// <summary>Navigation property to the declaring user.</summary>
    public User? DeclaringUser { get; set; }
}
