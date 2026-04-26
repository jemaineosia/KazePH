namespace KazePH.Core.Enums;

/// <summary>
/// Status of a friend request between two users.
/// </summary>
public enum FriendshipStatus
{
    /// <summary>Friend request has been sent but not yet acted on.</summary>
    Pending,

    /// <summary>The addressee accepted the friend request.</summary>
    Accepted,

    /// <summary>The addressee declined the friend request.</summary>
    Declined
}
