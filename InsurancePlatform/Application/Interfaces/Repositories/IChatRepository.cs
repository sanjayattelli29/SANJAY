using Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Interfaces.Repositories
{
    public interface IChatRepository
    {
        Task<Chat?> GetByPolicyIdAsync(string policyId);
        Task<IEnumerable<Chat>> GetChatsByUserIdAsync(string userId, string role);
        Task AddAsync(Chat chat);
        Task AddMessageAsync(ChatMessage message);
        Task<int> GetUnreadCountAsync(string chatId, string readerRole);
        Task<IEnumerable<ChatMessage>> GetUnreadMessagesAsync(string chatId, string readerRole);
        Task UpdateAsync(Chat chat);
        Task SaveChangesAsync();
    }
}
