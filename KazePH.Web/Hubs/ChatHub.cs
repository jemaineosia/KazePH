using Microsoft.AspNetCore.SignalR;

namespace KazePH.Web.Hubs;

/// <summary>
/// SignalR hub for real-time chat messages between users and within event group chats.
/// </summary>
public class ChatHub : Hub
{
    /// <summary>
    /// Sends a message to all members of the specified chat group.
    /// </summary>
    /// <param name="groupName">The chat group identifier (e.g., a direct-message pair or event ID).</param>
    /// <param name="user">Display name of the sender.</param>
    /// <param name="message">Message content.</param>
    public async Task SendMessage(string groupName, string user, string message)
    {
        await Clients.Group(groupName).SendAsync("ReceiveMessage", user, message);
    }

    /// <summary>
    /// Adds the calling connection to a named chat group (e.g., on entering a chat room).
    /// </summary>
    /// <param name="groupName">Group to join.</param>
    public async Task JoinGroup(string groupName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
    }

    /// <summary>
    /// Removes the calling connection from a named chat group.
    /// </summary>
    /// <param name="groupName">Group to leave.</param>
    public async Task LeaveGroup(string groupName)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
    }
}
