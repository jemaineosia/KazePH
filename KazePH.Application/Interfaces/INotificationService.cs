using KazePH.Core.Models;

namespace KazePH.Application.Interfaces;

/// <summary>
/// Sends and manages in-app notifications for platform users.
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Creates and persists a new notification for the specified user.
    /// </summary>
    /// <param name="userId">Identity ID of the notification recipient.</param>
    /// <param name="title">Short heading for the notification.</param>
    /// <param name="message">Full notification body text.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The persisted <see cref="Notification"/>.</returns>
    Task<Notification> SendNotificationAsync(
        string userId,
        string title,
        string message,
        CancellationToken cancellationToken = default);

    /// <summary>Marks a specific notification as read.</summary>
    /// <param name="notificationId">The notification's primary key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task MarkAsReadAsync(Guid notificationId, CancellationToken cancellationToken = default);
}
