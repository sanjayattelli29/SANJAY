using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.Interfaces;

namespace Application.Services
{
    public class SupportChatService : ISupportChatService
    {
        private readonly IChatRepository _chatRepository;
        private readonly IPolicyRepository _policyRepository;

        public SupportChatService(IChatRepository chatRepository, IPolicyRepository policyRepository)
        {
            _chatRepository = chatRepository;
            _policyRepository = policyRepository;
        }

        public async Task<Chat> GetOrCreateChatAsync(string policyId, string customerId, string agentId)
        {
            var chat = await _chatRepository.GetByPolicyIdAsync(policyId);

            if (chat == null)
            {
                var policy = await _policyRepository.GetByIdAsync(policyId, true);

                chat = new Chat
                {
                    PolicyId = policyId,
                    CustomerId = customerId,
                    AgentId = agentId,
                    CustomerEmail = policy?.User?.Email,
                    AgentEmail = policy?.AssignedAgent?.Email,
                    PolicyName = policy?.TierId, 
                    Category = policy?.PolicyCategory,
                    CoverageAmount = policy?.TotalCoverageAmount ?? 0,
                    DateActivated = policy?.StartDate ?? DateTime.UtcNow
                };

                await _chatRepository.AddAsync(chat);
                await _chatRepository.SaveChangesAsync();
            }

            return chat;
        }

        public async Task<ChatMessage> SaveMessageAsync(string policyId, string senderId, string senderRole, string message)
        {
            var chat = await _chatRepository.GetByPolicyIdAsync(policyId);
            if (chat == null) throw new Exception("Chat not found");

            var chatMessage = new ChatMessage
            {
                ChatId = chat.Id,
                SenderId = senderId,
                SenderRole = senderRole,
                Message = message,
                Timestamp = DateTime.UtcNow
            };

            chat.LastMessage = message;
            chat.LastMessageTime = chatMessage.Timestamp;
            chat.UpdatedAt = DateTime.UtcNow;

            await _chatRepository.AddMessageAsync(chatMessage);
            await _chatRepository.UpdateAsync(chat);
            await _chatRepository.SaveChangesAsync();

            return chatMessage;
        }

        public async Task<IEnumerable<Chat>> GetUserChatListAsync(string userId, string role)
        {
            IEnumerable<PolicyApplication> policies;
            if (role == "Customer")
            {
                policies = await _policyRepository.GetUserPoliciesAsync(userId);
            }
            else if (role == "Agent")
            {
                policies = await _policyRepository.GetAgentApplicationsAsync(userId);
            }
            else
            {
                return new List<Chat>();
            }

            var existingChats = await _chatRepository.GetChatsByUserIdAsync(userId, role);

            var resultList = new List<Chat>();

            foreach (var p in policies)
            {
                var chat = existingChats.FirstOrDefault(c => c.PolicyId == p.Id);
                
                if (chat != null)
                {
                    chat.UnreadCount = await _chatRepository.GetUnreadCountAsync(chat.Id, role);
                    resultList.Add(chat);
                }
                else
                {
                    resultList.Add(new Chat
                    {
                        Id = "new_" + p.Id,
                        PolicyId = p.Id,
                        CustomerId = p.UserId,
                        AgentId = p.AssignedAgentId ?? string.Empty,
                        CustomerEmail = p.User?.Email,
                        AgentEmail = p.AssignedAgent?.Email,
                        PolicyName = $"{p.TierId} Plan",
                        Category = p.PolicyCategory,
                        CoverageAmount = p.TotalCoverageAmount,
                        DateActivated = p.StartDate ?? p.SubmissionDate,
                        UpdatedAt = p.SubmissionDate,
                        UnreadCount = 0
                    });
                }
            }

            return resultList.OrderByDescending(r => r.UpdatedAt);
        }

        public async Task<Chat?> GetChatHistoryAsync(string policyId)
        {
            return await _chatRepository.GetByPolicyIdAsync(policyId);
        }

        public async Task MarkMessagesAsReadAsync(string policyId, string readerRole)
        {
            var chat = await _chatRepository.GetByPolicyIdAsync(policyId);
            if (chat == null) return;

            var unreadMessages = await _chatRepository.GetUnreadMessagesAsync(chat.Id, readerRole);

            if (unreadMessages.Any())
            {
                foreach (var msg in unreadMessages)
                {
                    msg.IsRead = true;
                }
                await _chatRepository.SaveChangesAsync();
            }
        }
    }
}
