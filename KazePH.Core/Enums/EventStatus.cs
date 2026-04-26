namespace KazePH.Core.Enums;

/// <summary>
/// Lifecycle status of a betting event.
/// </summary>
public enum EventStatus
{
    /// <summary>Event is being set up, not yet visible to others.</summary>
    Draft,

    /// <summary>Event is open and accepting participants.</summary>
    Open,

    /// <summary>Event is in progress; no more entries accepted.</summary>
    Active,

    /// <summary>Event has concluded and payouts have been released.</summary>
    Completed,

    /// <summary>Event was cancelled before conclusion; funds returned.</summary>
    Cancelled,

    /// <summary>A dispute has been raised and is under admin review.</summary>
    Disputed
}
