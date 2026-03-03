using API.Controllers;
using Application.Interfaces;
using Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using Xunit;

namespace API.Tests;

public class ChatControllerTests
{
    private readonly Mock<IChatService> _mockChatService;
    private readonly ChatController _controller;

    public ChatControllerTests()
    {
        _mockChatService = new Mock<IChatService>();
        _controller = new ChatController(_mockChatService.Object);
    }

    private void SetUser(string userId, string role = "Customer")
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Role, role)
        };
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test")) }
        };
    }

    [Fact]
    public async Task GetChatList_WithUser_ReturnsOk()
    {
        // Arrange
        SetUser("user-1");
        _mockChatService.Setup(s => s.GetUserChatListAsync("user-1", "Customer"))
            .ReturnsAsync(new List<Chat> { new Chat() });

        // Act
        var result = await _controller.GetChatList();

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetChatList_NoUser_ReturnsUnauthorized()
    {
        _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        var result = await _controller.GetChatList();
        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task GetChatHistory_Found_ReturnsOk()
    {
        // Arrange
        var chat = new Chat { PolicyId = "pol-1", Messages = new List<ChatMessage>() };
        _mockChatService.Setup(s => s.GetChatHistoryAsync("pol-1")).ReturnsAsync(chat);

        // Act
        var result = await _controller.GetChatHistory("pol-1");

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetChatHistory_NotFound_ReturnsNotFound()
    {
        // Arrange
        _mockChatService.Setup(s => s.GetChatHistoryAsync("pol-1")).ReturnsAsync((Chat?)null);

        // Act
        var result = await _controller.GetChatHistory("pol-1");

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task InitializeChat_WithUser_ReturnsOk()
    {
        // Arrange
        SetUser("user-1");
        _mockChatService.Setup(s => s.GetOrCreateChatAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new Chat());

        // Act
        var result = await _controller.InitializeChat(new ChatInitRequest { PolicyId = "p1", CustomerId = "c1", AgentId = "a1" });

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task InitializeChat_NoUser_ReturnsUnauthorized()
    {
        _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        var result = await _controller.InitializeChat(new ChatInitRequest());
        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task MarkRead_WithRole_ReturnsOk()
    {
        // Arrange
        SetUser("user-1", "Agent");

        // Act
        var result = await _controller.MarkRead("pol-1");

        // Assert
        Assert.IsType<OkResult>(result);
    }

    [Fact]
    public async Task MarkRead_NoRole_ReturnsUnauthorized()
    {
        _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        var result = await _controller.MarkRead("pol-1");
        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task GetChatList_Agent_PassesCorrectRole()
    {
        // Arrange
        SetUser("agent-1", "Agent");
        _mockChatService.Setup(s => s.GetUserChatListAsync("agent-1", "Agent"))
            .ReturnsAsync(new List<Chat>());

        // Act
        await _controller.GetChatList();

        // Assert
        _mockChatService.Verify(s => s.GetUserChatListAsync("agent-1", "Agent"), Times.Once);
    }

    [Fact]
    public async Task InitializeChat_CallsServiceWithCorrectArgs()
    {
        // Arrange
        SetUser("cust-1");
        var req = new ChatInitRequest { PolicyId = "p1", CustomerId = "cust-1", AgentId = "agent-1" };
        _mockChatService.Setup(s => s.GetOrCreateChatAsync("p1", "cust-1", "agent-1")).ReturnsAsync(new Chat());

        // Act
        await _controller.InitializeChat(req);

        // Assert
        _mockChatService.Verify(s => s.GetOrCreateChatAsync("p1", "cust-1", "agent-1"), Times.Once);
    }
}
