using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities;

public class Chat
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string PolicyId { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public string AgentId { get; set; } = string.Empty;
    
    // Denormalized data for easier dashboard listing
    public string? CustomerEmail { get; set; }
    public string? AgentEmail { get; set; }
    public string? PolicyName { get; set; }
    public string? Category { get; set; }
    public decimal CoverageAmount { get; set; }
    public DateTime DateActivated { get; set; }
    
    public string? LastMessage { get; set; }
    public DateTime? LastMessageTime { get; set; }
    
    [NotMapped]
    public int UnreadCount { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Relations
    public List<ChatMessage> Messages { get; set; } = new();
    public PolicyApplication? Policy { get; set; }
}
