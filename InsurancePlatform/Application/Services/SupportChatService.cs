using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.Interfaces;

namespace Application.Services
{
    /// <summary>
    /// This service handles the "Live Chat" between Customers and Agents.
    /// It ensures that messages are saved, history is loaded, and unread counts are tracked.
    /// </summary>
    public class SupportChatService : ISupportChatService
    {
        private readonly IChatRepository _chatRepository;
        private readonly IPolicyRepository _policyRepository;

        public SupportChatService(IChatRepository chatRepository, IPolicyRepository policyRepository)
        {
            _chatRepository = chatRepository;
            _policyRepository = policyRepository;
        }

        /// <summary>
        /// This method finds an existing chat for a policy or creates a new one.
        /// It copies details from the Policy (like Category and Coverage) into the Chat for quick access.
        /// </summary>
        public async Task<Chat> GetOrCreateChatAsync(string policyId, string customerId, string agentId)
        {
            // 1. Check if we already have a chat record for this policy.
            var chat = await _chatRepository.GetByPolicyIdAsync(policyId);

            if (chat == null)
            {
                // 2. If not, get the policy details to "Welcome" the user into the chat.
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

                // 3. Save the new chat session.
                await _chatRepository.AddAsync(chat);
                await _chatRepository.SaveChangesAsync();
            }

            return chat;
        }

        /// <summary>
        /// This method saves a single message (text) sent by a user.
        /// It also updates the "Last Message" preview seen in the chat list.
        /// </summary>
        public async Task<ChatMessage> SaveMessageAsync(string policyId, string senderId, string senderRole, string message)
        {
            // 1. Find the chat session.
            var chat = await _chatRepository.GetByPolicyIdAsync(policyId);
            if (chat == null) throw new Exception("Chat not found");

            // 2. Create the message object.
            var chatMessage = new ChatMessage
            {
                ChatId = chat.Id,
                SenderId = senderId,
                SenderRole = senderRole,
                Message = message,
                Timestamp = DateTime.UtcNow
            };

            // 3. Update the chat's "Last Activity" details.
            chat.LastMessage = message;
            chat.LastMessageTime = chatMessage.Timestamp;
            chat.UpdatedAt = DateTime.UtcNow;

            // 4. Save both the message and the updated chat status.
            await _chatRepository.AddMessageAsync(chatMessage);
            await _chatRepository.UpdateAsync(chat);
            await _chatRepository.SaveChangesAsync();

            return chatMessage;
        }

        /// <summary>
        /// This method gets a list of ALL chats for a specific user.
        /// If a policy doesn't have a chat started yet, it creates a "Dummy" one so the user can start talking.
        /// </summary>
        public async Task<IEnumerable<Chat>> GetUserChatListAsync(string userId, string role)
        {
            // 1. Find all policies belonging to the user.
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

            // 2. Find all REAL chats that have already started.
            var existingChats = await _chatRepository.GetChatsByUserIdAsync(userId, role);

            var resultList = new List<Chat>();

            foreach (var p in policies)
            {
                var chat = existingChats.FirstOrDefault(c => c.PolicyId == p.Id);
                
                if (chat != null)
                {
                    // If chat exists, count how many messages the user hasn't read.
                    chat.UnreadCount = await _chatRepository.GetUnreadCountAsync(chat.Id, role);
                    resultList.Add(chat);
                }
                else
                {
                    // If no chat exists yet, show a placeholder so the user can "Start" a chat.
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

            // 3. Show the most recently updated chats at the top.
            return resultList.OrderByDescending(r => r.UpdatedAt);
        }

        // Get the whole conversation history for a specific policy.
        public async Task<Chat?> GetChatHistoryAsync(string policyId)
        {
            return await _chatRepository.GetByPolicyIdAsync(policyId);
        }

        /// <summary>
        /// This method marks all messages in a chat as "Read" when a user opens the chat box.
        /// </summary>
        public async Task MarkMessagesAsReadAsync(string policyId, string readerRole)
        {
            var chat = await _chatRepository.GetByPolicyIdAsync(policyId);
            if (chat == null) return;

            // Find all messages that were sent to this user but haven't been read.
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
