using Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    /// <summary>
    /// This interface defines how we store and find chat messages in the database.
    /// It helps users (like Customers and Agents) talk to each other.
    /// </summary>
    public interface IChatRepository
    {
        // Find a chat using the unique Policy ID
        Task<Chat?> GetByPolicyIdAsync(string policyId);
        
        // Get all chats belonging to a specific user (either a Customer or an Agent)
        Task<IEnumerable<Chat>> GetChatsByUserIdAsync(string userId, string role);
        
        // Start a brand new chat session
        Task AddAsync(Chat chat);
        
        // Save a single message (text) into an existing chat
        Task AddMessageAsync(ChatMessage message);
        
        // Count how many messages a user has NOT read yet
        Task<int> GetUnreadCountAsync(string chatId, string readerRole);
        
        // Get all the actual messages that a user hasn't seen yet
        Task<IEnumerable<ChatMessage>> GetUnreadMessagesAsync(string chatId, string readerRole);
        
        // Update chat details (like the 'Last Message' time)
        Task UpdateAsync(Chat chat);
        
        // Tell the database to permanently save all changes made
        Task SaveChangesAsync();
    }
}
