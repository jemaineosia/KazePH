using Microsoft.AspNetCore.SignalR;

namespace KazePH.Web.Hubs;

/// <summary>
/// SignalR hub for real-time in-app notifications pushed to individual users.
/// </summary>
public class NotificationHub : Hub
{
    /// <summary>
    /// Adds the calling connection to the user's personal notification group so targeted
    /// notifications can be pushed without broadcasting to all clients.
    /// </summary>
    /// <param name="userId">The authenticated user's identity ID.</param>
    public async Task RegisterUser(string userId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
    }

    /// <summary>
    /// Pushes a notification to all connections belonging to the specified user.
    /// </summary>
    /// <param name="userId">Target user's identity ID.</param>
    /// <param name="title">Notification title.</param>
    /// <param name="message">Notification body.</param>
    public async Task SendToUser(string userId, string title, string message)
    {
        await Clients.Group($"user_{userId}").SendAsync("ReceiveNotification", title, message);
    }
}
