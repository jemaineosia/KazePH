namespace KazePH.Core.Enums;

/// <summary>
/// Status of a dispute raised against an event.
/// </summary>
public enum DisputeStatus
{
    /// <summary>Dispute has been filed and is waiting for review.</summary>
    Open,

    /// <summary>Admin is actively reviewing the submitted evidence.</summary>
    UnderReview,

    /// <summary>Admin has resolved the dispute and issued a verdict.</summary>
    Resolved
}
