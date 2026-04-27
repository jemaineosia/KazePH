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
    public EventStatus Status { get; set; } = EventStatus.Open;

    /// <summary>Name of the first side/team participants can bet on.</summary>
    public string SideA { get; set; } = "Side A";

    /// <summary>Name of the second side/team participants can bet on.</summary>
    public string SideB { get; set; } = "Side B";

    /// <summary>
    /// Fixed stake amount per participant. Required for 1v1 events (both sides bet this amount).
    /// For Pool events this is the optional minimum bet per entry.
    /// </summary>
    public decimal? StakeAmount { get; set; }

    // ── 1v1-specific fields ───────────────────────────────────────────────────

    /// <summary>"A" or "B" — which side the creator bet on (1v1 only).</summary>
    public string? CreatorSide { get; set; }

    /// <summary>How much the creator staked. Equal to StakeAmount for 1v1.</summary>
    public decimal? CreatorStake { get; set; }

    /// <summary>Amount the opponent must stake to accept the 1v1 challenge (may differ from creator's stake).</summary>
    public decimal? OpponentStake { get; set; }

    /// <summary>Identity ID of a specific user the creator is challenging (optional direct invite).</summary>
    public string? ChallengedUserId { get; set; }

    /// <summary>Username of the challenged user for display (optional direct invite).</summary>
    public string? ChallengedUsername { get; set; }

    /// <summary>Date and time the event record was created (UTC).</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Navigation property to the event creator.</summary>
    public User? Creator { get; set; }

    /// <summary>All bet entries placed on this event.</summary>
    public ICollection<BetEntry> BetEntries { get; set; } = new List<BetEntry>();

    /// <summary>The declared result of the event once completed.</summary>
    public EventResult? Result { get; set; }

    /// <summary>Individual result declarations from each 1v1 participant.</summary>
    public ICollection<ResultDeclaration> ResultDeclarations { get; set; } = new List<ResultDeclaration>();
}
