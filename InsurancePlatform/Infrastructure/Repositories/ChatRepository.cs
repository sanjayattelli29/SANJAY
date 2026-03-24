using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.Interfaces;

namespace Infrastructure.Repositories
{
    public class ChatRepository : IChatRepository
    {
        private readonly ApplicationDbContext _context;

        public ChatRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Chat?> GetByPolicyIdAsync(string policyId)
        {
            return await _context.Chats
                .Include(c => c.Messages)
                .FirstOrDefaultAsync(c => c.PolicyId == policyId);
        }

        public async Task<IEnumerable<Chat>> GetChatsByUserIdAsync(string userId, string role)
        {
            if (role == "Customer")
            {
                return await _context.Chats
                    .Where(c => c.CustomerId == userId)
                    .ToListAsync();
            }
            else if (role == "Agent")
            {
                return await _context.Chats
                    .Where(c => c.AgentId == userId)
                    .ToListAsync();
            }
            return new List<Chat>();
        }

        public async Task AddAsync(Chat chat)
        {
            await _context.Chats.AddAsync(chat);
        }

        public async Task AddMessageAsync(ChatMessage message)
        {
            await _context.ChatMessages.AddAsync(message);
        }

        public async Task<int> GetUnreadCountAsync(string chatId, string readerRole)
        {
            return await _context.ChatMessages
                .CountAsync(m => m.ChatId == chatId && !m.IsRead && m.SenderRole != readerRole);
        }

        public async Task<IEnumerable<ChatMessage>> GetUnreadMessagesAsync(string chatId, string readerRole)
        {
            return await _context.ChatMessages
                .Where(m => m.ChatId == chatId && !m.IsRead && m.SenderRole != readerRole)
                .ToListAsync();
        }

        public async Task UpdateAsync(Chat chat)
        {
            _context.Chats.Update(chat);
            await Task.CompletedTask;
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
