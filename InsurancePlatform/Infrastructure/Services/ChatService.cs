using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

// this class manages chatting between people
public class ChatService : IChatService
{
    private readonly ApplicationDbContext _context;

    public ChatService(ApplicationDbContext context)
    {
        _context = context;
    }

    // find a chat session or join it if not there
    public async Task<Chat> GetOrCreateChatAsync(string policyId, string customerId, string agentId)
    {
        // try to find by policy id
        var chat = await _context.Chats
            .Include(c => c.Messages)
            .FirstOrDefaultAsync(c => c.PolicyId == policyId);

        if (chat == null)
        {
            // if not found get policy details
            var policy = await _context.PolicyApplications
                .Include(p => p.User)
                .Include(p => p.AssignedAgent)
                .FirstOrDefaultAsync(p => p.Id == policyId);

            // create a new chat record
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

            _context.Chats.Add(chat);
            await _context.SaveChangesAsync();
        }

        return chat;
    }

    // save a text message sent by user or agent
    public async Task<ChatMessage> SaveMessageAsync(string policyId, string senderId, string senderRole, string message)
    {
        // find which chat session this message belongs to
        var chat = await _context.Chats.FirstOrDefaultAsync(c => c.PolicyId == policyId);
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

        _context.ChatMessages.Add(chatMessage);
        await _context.SaveChangesAsync();

        return chatMessage;
    }

// gets all policies and chats for the user based on their role
        public async Task<IEnumerable<Chat>> GetUserChatListAsync(string userId, string role)
        {
            // step 1: find all insurance policies this user is involved with
            var policiesQuery = _context.PolicyApplications
                .Include(p => p.User)  // customer details
                .Include(p => p.AssignedAgent)  // agent details
                .AsQueryable();

            // filter based on who is logged in
            if (role == "Customer")
            {
                policiesQuery = policiesQuery.Where(p => p.UserId == userId);  // customer sees their own policies
            }
            else if (role == "Agent")
            {
                policiesQuery = policiesQuery.Where(p => p.AssignedAgentId == userId);  // agent sees assigned policies
            }
            else
            {
                return new List<Chat>();  // other roles don't have chats
        }

        var policies = await policiesQuery.ToListAsync();

        // step 2: get existing chat records from database
        List<Chat> existingChats = new();
        try 
        {
            existingChats = await _context.Chats
                .Where(c => (role == "Customer" && c.CustomerId == userId) || (role == "Agent" && c.AgentId == userId))
                .ToListAsync();
        }
        catch (Exception)
        {
            // if chat table doesn't exist yet just use policies
        }

        // step 3: combine policies with chat data for the list
        var result = policies.Select(p => {
            // try to find existing chat for this policy
            var chat = existingChats.FirstOrDefault(c => c.PolicyId == p.Id);
            
            // count how many unread messages they have
            int unreadCount = 0;
            if (chat != null)
            {
                unreadCount = _context.ChatMessages
                    .Count(m => m.ChatId == chat.Id && !m.IsRead && m.SenderRole != role);
            }

            // if chat exists return it with unread count
            if (chat != null) 
            {
                chat.UnreadCount = unreadCount;
                return chat;
            }

            // if chat doesn't exist create a temporary object for display
            return new Chat
            {
                Id = "new_" + p.Id,  // special id to show it's new
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
            };
        });

        return result.OrderByDescending(r => r.UpdatedAt);  // most recent chats first
    }

    // loads all previous messages for a specific policy chat
    public async Task<Chat?> GetChatHistoryAsync(string policyId)
    {
        return await _context.Chats
            .Include(c => c.Messages.OrderBy(m => m.Timestamp))  // load messages in time order
            .FirstOrDefaultAsync(c => c.PolicyId == policyId);
    }

    // marks all messages as read when user opens the chat
    public async Task MarkMessagesAsReadAsync(string policyId, string readerRole)
    {
        // find the chat session
        var chat = await _context.Chats.FirstOrDefaultAsync(c => c.PolicyId == policyId);
        if (chat == null) return;

        // find messages user hasn't seen yet (sent by other person)
        var unreadMessages = await _context.ChatMessages
            .Where(m => m.ChatId == chat.Id && !m.IsRead && m.SenderRole != readerRole)
            .ToListAsync();

        // mark them all as read
        if (unreadMessages.Any())
        {
            foreach (var msg in unreadMessages)
            {
                msg.IsRead = true;
            }
            await _context.SaveChangesAsync();
        }
    }
}
