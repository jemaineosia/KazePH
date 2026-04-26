using KazePH.Application.Interfaces;
using KazePH.Core.Models;
using KazePH.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KazePH.Application.Services;

/// <summary>
/// Implements <see cref="INotificationService"/> using <see cref="KazeDbContext"/>.
/// </summary>
public class NotificationService : INotificationService
{
    private readonly KazeDbContext _db;

    /// <summary>Initializes a new instance of <see cref="NotificationService"/>.</summary>
    public NotificationService(KazeDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<Notification> SendNotificationAsync(
        string userId,
        string title,
        string message,
        CancellationToken cancellationToken = default)
    {
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = title,
            Message = message,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        _db.Notifications.Add(notification);
        await _db.SaveChangesAsync(cancellationToken);
        return notification;
    }

    /// <inheritdoc />
    public async Task MarkAsReadAsync(Guid notificationId, CancellationToken cancellationToken = default)
    {
        var notification = await _db.Notifications.FirstOrDefaultAsync(n => n.Id == notificationId, cancellationToken)
            ?? throw new InvalidOperationException($"Notification '{notificationId}' not found.");

        notification.IsRead = true;
        await _db.SaveChangesAsync(cancellationToken);
    }
}
