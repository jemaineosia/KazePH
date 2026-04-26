namespace KazePH.Core.Models;

/// <summary>
/// Records a single participant's bet entry in an event.
/// </summary>
public class BetEntry
{
    /// <summary>Primary key.</summary>
    public Guid Id { get; set; }

    /// <summary>Foreign key to the <see cref="Event"/> this entry belongs to.</summary>
    public Guid EventId { get; set; }

    /// <summary>Foreign key to the <see cref="User"/> who placed this bet.</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// The side or team the user bet on (e.g., "Team A", "Side 1").
    /// Stored as a string to support flexible event configurations.
    /// </summary>
    public string Side { get; set; } = string.Empty;

    /// <summary>Amount wagered in PHP.</summary>
    public decimal Amount { get; set; }

    /// <summary>Date and time the bet was placed (UTC).</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Navigation property to the parent event.</summary>
    public Event? Event { get; set; }

    /// <summary>Navigation property to the betting user.</summary>
    public User? User { get; set; }
}
