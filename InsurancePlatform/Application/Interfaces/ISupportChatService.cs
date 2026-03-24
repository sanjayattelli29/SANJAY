using Domain.Entities;

namespace Application.Interfaces;

// this interface handles the chat functions for agent and customer
public interface ISupportChatService
{
    // get a list of all chats for a user
    Task<IEnumerable<Chat>> GetUserChatListAsync(string userId, string role);
    // find a chat or make a new one if not exists
    Task<Chat> GetOrCreateChatAsync(string policyId, string customerId, string agentId);
    // save a new message to the database
    Task<ChatMessage> SaveMessageAsync(string policyId, string senderId, string senderRole, string message);
    // get all messages for a specific chat
    Task<Chat?> GetChatHistoryAsync(string policyId);
    // mark that someone has seen the messages
    Task MarkMessagesAsReadAsync(string policyId, string readerRole);
}
