using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.Interfaces;

namespace Infrastructure.Repositories
{
    /// <summary>
    /// This class is the "Storage Room" for all Chat messages and sessions.
    /// It handles saving and finding chats in the database.
    /// </summary>
    public class ChatRepository : IChatRepository
    {
        private readonly ApplicationDbContext _context;

        // Constructor sets up the database connection.
        public ChatRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        // Find a specific chat based on the Policy ID it belongs to.
        public async Task<Chat?> GetByPolicyIdAsync(string policyId)
        {
            return await _context.Chats
                .Include(c => c.Messages)
                .FirstOrDefaultAsync(c => c.PolicyId == policyId);
        }

        // Get all chats for a specific person depending on if they are a Customer or Agent.
        public async Task<IEnumerable<Chat>> GetChatsByUserIdAsync(string userId, string role)
        {
            if (role == "Customer")
            {
                // Find chats where this user is the Customer.
                return await _context.Chats
                    .Where(c => c.CustomerId == userId)
                    .ToListAsync();
            }
            else if (role == "Agent")
            {
                // Find chats assigned to this Agent.
                return await _context.Chats
                    .Where(c => c.AgentId == userId)
                    .ToListAsync();
            }
            return new List<Chat>();
        }

        // Save a brand new chat session to the database.
        public async Task AddAsync(Chat chat)
        {
            await _context.Chats.AddAsync(chat);
        }

        // Save a new message (text) into an existing chat.
        public async Task AddMessageAsync(ChatMessage message)
        {
            await _context.ChatMessages.AddAsync(message);
        }

        // Count how many messages the user HAS NOT read yet.
        public async Task<int> GetUnreadCountAsync(string chatId, string readerRole)
        {
            return await _context.ChatMessages
                .CountAsync(m => m.ChatId == chatId && !m.IsRead && m.SenderRole != readerRole);
        }

        // Get a list of all messages that are still marked as "Unread".
        public async Task<IEnumerable<ChatMessage>> GetUnreadMessagesAsync(string chatId, string readerRole)
        {
            return await _context.ChatMessages
                .Where(m => m.ChatId == chatId && !m.IsRead && m.SenderRole != readerRole)
                .ToListAsync();
        }

        // Update the chat details (e.g., if a new message was added).
        public async Task UpdateAsync(Chat chat)
        {
            _context.Chats.Update(chat);
            await Task.CompletedTask;
        }

        // Finalize and save all changes to the SQL database.
        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
