using Domain.Entities;

namespace Application.Interfaces;

/// <summary>
/// This interface handles the "Customer Support" chat features.
/// It allows users to talk to Agents and keeps a record of all conversations.
/// </summary>
public interface ISupportChatService
{
    // Get a list of all chat conversations for a specific user
    Task<IEnumerable<Chat>> GetUserChatListAsync(string userId, string role);
    
    // Find an existing chat or create a new one between a Customer and an Agent
    Task<Chat> GetOrCreateChatAsync(string policyId, string customerId, string agentId);
    
    // Save a new message sent by either the Customer or the Agent
    Task<ChatMessage> SaveMessageAsync(string policyId, string senderId, string senderRole, string message);
    
    // Get all the previous messages (the whole conversation) for a policy chat
    Task<Chat?> GetChatHistoryAsync(string policyId);
    
    // Mark all messages in a chat as 'Seen' by the reader
    Task MarkMessagesAsReadAsync(string policyId, string readerRole);
}
