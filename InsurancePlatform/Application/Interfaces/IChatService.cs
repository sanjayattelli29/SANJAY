using Domain.Entities; // for chat models

namespace Application.Interfaces; // folder location

// this interface handles the chat functions for agent and customer
public interface IChatService // live chat contract
{
    // get a list of all chats for a user
    Task<IEnumerable<Chat>> GetUserChatListAsync(string userId, string role); // active conversations
    // find a chat or make a new one if not exists
    Task<Chat> GetOrCreateChatAsync(string policyId, string customerId, string agentId); // connection point
    // save a new message to the database
    Task<ChatMessage> SaveMessageAsync(string policyId, string senderId, string senderRole, string message); // persists text
    // get all messages for a specific chat
    Task<Chat?> GetChatHistoryAsync(string policyId); // retrieves messages
    // mark that someone has seen the messages
    Task MarkMessagesAsReadAsync(string policyId, string readerRole); // updates unread count
}
// chat service interface ends
