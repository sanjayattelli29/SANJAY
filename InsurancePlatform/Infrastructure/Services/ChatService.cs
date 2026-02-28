using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class ChatService : IChatService
{
    private readonly ApplicationDbContext _context;

    public ChatService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Chat> GetOrCreateChatAsync(string policyId, string customerId, string agentId)
    {
        var chat = await _context.Chats
            .Include(c => c.Messages)
            .FirstOrDefaultAsync(c => c.PolicyId == policyId);

        if (chat == null)
        {
            var policy = await _context.PolicyApplications
                .Include(p => p.User)
                .Include(p => p.AssignedAgent)
                .FirstOrDefaultAsync(p => p.Id == policyId);

            chat = new Chat
            {
                PolicyId = policyId,
                CustomerId = customerId,
                AgentId = agentId,
                CustomerEmail = policy?.User?.Email,
                AgentEmail = policy?.AssignedAgent?.Email,
                PolicyName = policy?.TierId, // Simplification, could be more detailed
                Category = policy?.PolicyCategory,
                CoverageAmount = policy?.TotalCoverageAmount ?? 0,
                DateActivated = policy?.StartDate ?? DateTime.UtcNow
            };

            _context.Chats.Add(chat);
            await _context.SaveChangesAsync();
        }

        return chat;
    }

    public async Task<ChatMessage> SaveMessageAsync(string policyId, string senderId, string senderRole, string message)
    {
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

    public async Task<IEnumerable<Chat>> GetUserChatListAsync(string userId, string role)
    {
        // 1. Get all policies relevant to this user
        var policiesQuery = _context.PolicyApplications
            .Include(p => p.User)
            .Include(p => p.AssignedAgent)
            .AsQueryable();

        if (role == "Customer")
        {
            policiesQuery = policiesQuery.Where(p => p.UserId == userId);
        }
        else if (role == "Agent")
        {
            policiesQuery = policiesQuery.Where(p => p.AssignedAgentId == userId);
        }
        else
        {
            return new List<Chat>();
        }

        var policies = await policiesQuery.ToListAsync();

        // 2. Get existing chats safely
        List<Chat> existingChats = new();
        try 
        {
            existingChats = await _context.Chats
                .Where(c => (role == "Customer" && c.CustomerId == userId) || (role == "Agent" && c.AgentId == userId))
                .ToListAsync();
        }
        catch (Exception)
        {
            // If tables don't exist yet, we only have policies
        }

        // 3. Merge: Return all policies as Chat objects
        var result = policies.Select(p => {
            var chat = existingChats.FirstOrDefault(c => c.PolicyId == p.Id);
            if (chat != null) return chat;

            // Return a transient Chat object for the UI
            return new Chat
            {
                Id = "new_" + p.Id, // Signal to frontend that it's not yet in DB
                PolicyId = p.Id,
                CustomerId = p.UserId,
                AgentId = p.AssignedAgentId ?? string.Empty,
                CustomerEmail = p.User?.Email,
                AgentEmail = p.AssignedAgent?.Email,
                PolicyName = $"{p.TierId} Plan",
                Category = p.PolicyCategory,
                CoverageAmount = p.TotalCoverageAmount,
                DateActivated = p.StartDate ?? p.SubmissionDate,
                UpdatedAt = p.SubmissionDate
            };
        });

        return result.OrderByDescending(r => r.UpdatedAt);
    }

    public async Task<Chat?> GetChatHistoryAsync(string policyId)
    {
        return await _context.Chats
            .Include(c => c.Messages.OrderBy(m => m.Timestamp))
            .FirstOrDefaultAsync(c => c.PolicyId == policyId);
    }
}
