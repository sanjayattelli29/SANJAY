using Domain.Entities;

namespace Application.Interfaces;

public interface IChatService
{
    Task<IEnumerable<Chat>> GetUserChatListAsync(string userId, string role);
    Task<Chat> GetOrCreateChatAsync(string policyId, string customerId, string agentId);
    Task<ChatMessage> SaveMessageAsync(string policyId, string senderId, string senderRole, string message);
    Task<Chat?> GetChatHistoryAsync(string policyId);
}
