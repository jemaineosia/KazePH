using KazePH.Core.Enums;

namespace KazePH.Core.Models;

/// <summary>
/// Represents a friend request or established friendship between two users.
/// </summary>
public class Friendship
{
    /// <summary>Primary key.</summary>
    public Guid Id { get; set; }

    /// <summary>Foreign key to the user who sent the friend request.</summary>
    public string RequesterId { get; set; } = string.Empty;

    /// <summary>Foreign key to the user who received the friend request.</summary>
    public string AddresseeId { get; set; } = string.Empty;

    /// <summary>Current state of the friendship request.</summary>
    public FriendshipStatus Status { get; set; } = FriendshipStatus.Pending;

    /// <summary>Date and time the friend request was sent (UTC).</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Navigation property to the requester.</summary>
    public User? Requester { get; set; }

    /// <summary>Navigation property to the addressee.</summary>
    public User? Addressee { get; set; }
}
