using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities;

// this class is for chat sessions between customer and agent
public class Chat
{
    // unique id for the chat
    public string Id { get; set; } = Guid.NewGuid().ToString();
    // link to the policy this chat is about
    public string PolicyId { get; set; } = string.Empty;
    // link to the customer
    public string CustomerId { get; set; } = string.Empty;
    // link to the agent
    public string AgentId { get; set; } = string.Empty;
    
    // we keep email here so we don't have to look up user table every time
    public string? CustomerEmail { get; set; }
    public string? AgentEmail { get; set; }
    // name of the policy
    public string? PolicyName { get; set; }
    // category like health or life
    public string? Category { get; set; }
    // how much money is covered
    public decimal CoverageAmount { get; set; }
    // when the policy started
    public DateTime DateActivated { get; set; }
    
    // the last message sent in this chat
    public string? LastMessage { get; set; }
    // when the last message was sent
    public DateTime? LastMessageTime { get; set; }
    
    // this counts how many messages are not read yet
    [NotMapped]
    public int UnreadCount { get; set; }
    
    // when the chat was started
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    // when any change happened
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // list of all messages in this chat
    public List<ChatMessage> Messages { get; set; } = new();
    // link to the actual policy object
    public PolicyApplication? Policy { get; set; }
}
