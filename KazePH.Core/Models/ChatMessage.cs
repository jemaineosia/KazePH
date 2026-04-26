namespace KazePH.Core.Models;

/// <summary>
/// A chat message sent between users or within an event's group chat.
/// </summary>
public class ChatMessage
{
    /// <summary>Primary key.</summary>
    public Guid Id { get; set; }

    /// <summary>Foreign key to the user who sent this message.</summary>
    public string SenderId { get; set; } = string.Empty;

    /// <summary>
    /// Foreign key to the direct message recipient.
    /// Null if the message is part of an event group chat.
    /// </summary>
    public string? ReceiverId { get; set; }

    /// <summary>
    /// Foreign key to the event this message belongs to (for event group chats).
    /// Null if this is a direct message.
    /// </summary>
    public Guid? EventId { get; set; }

    /// <summary>Text content of the message.</summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>Date and time the message was sent (UTC).</summary>
    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    /// <summary>Whether the recipient has read this message.</summary>
    public bool IsRead { get; set; }

    /// <summary>Navigation property to the sender.</summary>
    public User? Sender { get; set; }

    /// <summary>Navigation property to the direct message recipient, if applicable.</summary>
    public User? Receiver { get; set; }

    /// <summary>Navigation property to the event, if applicable.</summary>
    public Event? Event { get; set; }
}
