using System;

namespace Domain.Entities;

// this class stores a single message in a chat

/// <summary>
/// This class represents a single "Text Message" sent inside a Chat room.
/// </summary>
public class ChatMessage
{
    // A unique ID for this specific message.
    public string Id { get; set; } = Guid.NewGuid().ToString();

    // The ID of the Chat room this message belongs to.
    public string ChatId { get; set; } = string.Empty;

    // The unique ID of the person who typed the message.
    public string SenderId { get; set; } = string.Empty;

    // The job of the sender (e.g., "Customer" or "Agent").
    public string SenderRole { get; set; } = string.Empty; 

    // The actual text content of the message.
    public string Message { get; set; } = string.Empty;

    // The date and time when the message was "Sent".
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    // "True" if the other person in the chat has opened/seen this message.
    public bool IsRead { get; set; } = false;

    // Links back to the full Chat session details.
    public Chat? Chat { get; set; }
}
