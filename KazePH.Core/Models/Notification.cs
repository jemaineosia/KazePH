namespace KazePH.Core.Models;

/// <summary>
/// In-app notification delivered to a specific user.
/// </summary>
public class Notification
{
    /// <summary>Primary key.</summary>
    public Guid Id { get; set; }

    /// <summary>Foreign key to the recipient <see cref="User"/>.</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>Short title summarising the notification.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Full notification message body.</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>Whether the user has read the notification.</summary>
    public bool IsRead { get; set; }

    /// <summary>Date and time the notification was created (UTC).</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Navigation property to the recipient user.</summary>
    public User? User { get; set; }
}
