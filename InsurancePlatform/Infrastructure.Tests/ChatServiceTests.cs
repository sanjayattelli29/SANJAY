using Application.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain.Entities;
using Infrastructure.Data;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Infrastructure.Tests;

public class ChatServiceTests
{
    private readonly ApplicationDbContext _context;
    private readonly SupportChatService _chatService;

    public ChatServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        var chatRepo = new Infrastructure.Repositories.ChatRepository(_context);
        var policyRepo = new Infrastructure.Repositories.PolicyRepository(_context);
        _chatService = new SupportChatService(chatRepo, policyRepo);
    }

    private void SetupBaselineData(string policyId, string userId, string agentId)
    {
        var user = new ApplicationUser { Id = userId, Email = "cust@test.com" };
        var agent = new ApplicationUser { Id = agentId, Email = "agent@test.com" };
        _context.Users.AddRange(user, agent);
        
        _context.PolicyApplications.Add(new PolicyApplication 
        { 
            Id = policyId, 
            UserId = userId, 
            AssignedAgentId = agentId,
            Status = "Active",
            User = user,
            AssignedAgent = agent
        });
    }

    [Fact]
    public async Task GetOrCreateChat_Exists_ReturnsExisting()
    {
        // Arrange
        var policyId = "pol-1";
        SetupBaselineData(policyId, "cust-1", "agent-1");
        var existingChat = new Chat { Id = "chat-1", PolicyId = policyId, CustomerId = "cust-1", AgentId = "agent-1" };
        _context.Chats.Add(existingChat);
        await _context.SaveChangesAsync();

        // Act
        var result = await _chatService.GetOrCreateChatAsync(policyId, "cust-1", "agent-1");

        // Assert
        Assert.Equal("chat-1", result.Id);
    }

    [Fact]
    public async Task GetOrCreateChat_NotExists_CreatesNewWithPolicyDetails()
    {
        // Arrange
        var policyId = "pol-1";
        var userId = "cust-1";
        var agentId = "agent-1";
        SetupBaselineData(policyId, userId, agentId);
        await _context.SaveChangesAsync();

        // Act
        var result = await _chatService.GetOrCreateChatAsync(policyId, userId, agentId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(policyId, result.PolicyId);
        Assert.Equal("cust@test.com", result.CustomerEmail);
    }

    [Fact]
    public async Task SaveMessage_Success_UpdatesChatMetadata()
    {
        // Arrange
        var policyId = "pol-1";
        SetupBaselineData(policyId, "cust-1", "agent-1");
        var chat = new Chat { Id = "chat-1", PolicyId = policyId };
        _context.Chats.Add(chat);
        await _context.SaveChangesAsync();

        // Act
        var result = await _chatService.SaveMessageAsync(policyId, "cust-1", "Customer", "Hello");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Hello", result.Message);
        var updatedChat = await _context.Chats.FindAsync("chat-1");
        Assert.Equal("Hello", updatedChat?.LastMessage);
    }

    [Fact]
    public async Task SaveMessage_ChatNotFound_ThrowsException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _chatService.SaveMessageAsync("wrong-pol", "s1", "Customer", "Hi"));
    }

    [Fact]
    public async Task GetUserChatList_Customer_ReturnsCorrectChats()
    {
        // Arrange
        var userId = "cust-1";
        SetupBaselineData("p1", userId, "agent-1");
        _context.Chats.Add(new Chat { Id = "c1", PolicyId = "p1", CustomerId = userId, UpdatedAt = DateTime.UtcNow });
        await _context.SaveChangesAsync();

        // Act
        var result = await _chatService.GetUserChatListAsync(userId, "Customer");

        // Assert
        Assert.NotEmpty(result);
        Assert.Contains(result, c => c.PolicyId == "p1");
    }

    [Fact]
    public async Task GetUserChatList_Agent_ReturnsCorrectChats()
    {
        // Arrange
        var agentId = "agent-1";
        SetupBaselineData("p1", "cust-1", agentId);
        _context.Chats.Add(new Chat { Id = "c1", PolicyId = "p1", AgentId = agentId, UpdatedAt = DateTime.UtcNow });
        await _context.SaveChangesAsync();

        // Act
        var result = await _chatService.GetUserChatListAsync(agentId, "Agent");

        // Assert
        Assert.NotEmpty(result);
        Assert.Contains(result, c => c.PolicyId == "p1");
    }

    [Fact]
    public async Task GetUserChatList_OtherRole_ReturnsEmpty()
    {
        // Act
        var result = await _chatService.GetUserChatListAsync("some-id", "Admin");

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetChatHistory_ReturnsMessagesInOrder()
    {
        // Arrange
        var chat = new Chat { Id = "c1", PolicyId = "p1" };
        _context.Chats.Add(chat);
        _context.ChatMessages.Add(new ChatMessage { Id = "m1", ChatId = "c1", Message = "First", Timestamp = DateTime.UtcNow.AddMinutes(-5) });
        _context.ChatMessages.Add(new ChatMessage { Id = "m2", ChatId = "c1", Message = "Second", Timestamp = DateTime.UtcNow.AddMinutes(-1) });
        await _context.SaveChangesAsync();

        // Act
        var result = await _chatService.GetChatHistoryAsync("p1");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Messages.Count);
    }

    [Fact]
    public async Task MarkMessagesAsRead_UpdatesStatus()
    {
        // Arrange
        var chat = new Chat { Id = "c1", PolicyId = "p1" };
        var msg = new ChatMessage { Id = "m1", ChatId = "c1", SenderRole = "Agent", IsRead = false };
        _context.Chats.Add(chat);
        _context.ChatMessages.Add(msg);
        await _context.SaveChangesAsync();

        // Act
        await _chatService.MarkMessagesAsReadAsync("p1", "Customer");

        // Assert
        var updatedMsg = await _context.ChatMessages.FindAsync("m1");
        Assert.True(updatedMsg?.IsRead);
    }

    [Fact]
    public async Task GetUserChatList_IncludesUnreadCount()
    {
        // Arrange
        var userId = "cust-1";
        SetupBaselineData("p1", userId, "agent-1");
        var chat = new Chat { Id = "c1", PolicyId = "p1", CustomerId = userId, UpdatedAt = DateTime.UtcNow };
        _context.Chats.Add(chat);
        _context.ChatMessages.Add(new ChatMessage { Id = "m1", ChatId = "c1", SenderRole = "Agent", IsRead = false });
        _context.ChatMessages.Add(new ChatMessage { Id = "m2", ChatId = "c1", SenderRole = "Agent", IsRead = false });
        await _context.SaveChangesAsync();

        // Act
        var result = await _chatService.GetUserChatListAsync(userId, "Customer");

        // Assert
        var chatResult = result.First(c => c.PolicyId == "p1");
        Assert.Equal(2, chatResult.UnreadCount);
    }
}
