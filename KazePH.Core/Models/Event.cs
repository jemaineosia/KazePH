using KazePH.Core.Enums;

namespace KazePH.Core.Models;

/// <summary>
/// Represents a betting event created by a user. Can be a 1v1 duel or a pool/parimutuel event.
/// </summary>
public class Event
{
    /// <summary>Primary key.</summary>
    public Guid Id { get; set; }

    /// <summary>Foreign key to the user who created the event.</summary>
    public string CreatorId { get; set; } = string.Empty;

    /// <summary>Short descriptive title of the event.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Detailed description of what is being bet on.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Scheduled date and time of the event (UTC).</summary>
    public DateTime EventDate { get; set; }

    /// <summary>Determines whether this is a 1v1 duel or a pool bet.</summary>
    public EventType EventType { get; set; }

    /// <summary>Current lifecycle status of the event.</summary>
    public EventStatus Status { get; set; } = EventStatus.Draft;

    /// <summary>Date and time the event record was created (UTC).</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Navigation property to the event creator.</summary>
    public User? Creator { get; set; }

    /// <summary>All bet entries placed on this event.</summary>
    public ICollection<BetEntry> BetEntries { get; set; } = new List<BetEntry>();
}
