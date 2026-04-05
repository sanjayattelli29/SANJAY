using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities;

// this class is for chat sessions between customer and agent

/// <summary>
/// This class stores a "Chat Session" between a Customer and an Agent.
/// It keeps track of the policy details they are discussing so they can see context while talking.
/// </summary>
public class Chat
{
    // A unique ID identifying this chat room.
    public string Id { get; set; } = Guid.NewGuid().ToString();

    // The ID of the policy the user is asking about.
    public string PolicyId { get; set; } = string.Empty;

    // The unique ID of the customer who started the chat.
    public string CustomerId { get; set; } = string.Empty;

    // The unique ID of the agent assigned to help the customer.
    public string AgentId { get; set; } = string.Empty;
    
    // Quick-access emails so we don't have to keep searching the User database.
    public string? CustomerEmail { get; set; }
    public string? AgentEmail { get; set; }

    // The name of the policy (like "Gold Plan") being discussed.
    public string? PolicyName { get; set; }

    // The type of insurance (e.g., Health, Life, or General).
    public string? Category { get; set; }

    // The maximum money the policy covers (e.g., 5 Lakhs).
    public decimal CoverageAmount { get; set; }

    // The date the policy was officially turned on.
    public DateTime DateActivated { get; set; }
    
    // A preview of the very last message sent in this chat.
    public string? LastMessage { get; set; }

    // The exact time the last message was sent.
    public DateTime? LastMessageTime { get; set; }
    
    // A temporary number showing how many messages are unread (not saved in DB).
    [NotMapped]
    public int UnreadCount { get; set; }
    
    // When this chat session was first created.
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // When the chat was last updated (e.g., when a new message was sent).
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // A list of every single message ever sent in this specific chat.
    public List<ChatMessage> Messages { get; set; } = new();

    // Links to the actual Policy record for full details.
    public PolicyApplication? Policy { get; set; }
}
