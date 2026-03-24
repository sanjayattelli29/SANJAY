using API.Controllers;
using Application.Interfaces;
using Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using Xunit;

namespace API.Tests;

public class NotificationsControllerTests
{
    private readonly Mock<ISystemNotifier> _mockSystemNotifier;
    private readonly Mock<IPolicyManager> _mockPolicyManager;
    private readonly Mock<IClaimProcessor> _mockClaimProcessor;
    private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
    private readonly NotificationsController _controller;

    public NotificationsControllerTests()
    {
        _mockSystemNotifier = new Mock<ISystemNotifier>();
        _mockPolicyManager = new Mock<IPolicyManager>();
        _mockClaimProcessor = new Mock<IClaimProcessor>();

        var store = new Mock<IUserStore<ApplicationUser>>();
        _mockUserManager = new Mock<UserManager<ApplicationUser>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        _controller = new NotificationsController(
            _mockSystemNotifier.Object,
            _mockPolicyManager.Object,
            _mockClaimProcessor.Object,
            _mockUserManager.Object);
    }

    private void SetUser(string userId, string role = "Customer")
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Role, role)
        };
        var identity = new ClaimsIdentity(claims, "Test");
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };

        _mockUserManager.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns(userId);
        _mockUserManager.Setup(m => m.FindByIdAsync(userId)).ReturnsAsync(new ApplicationUser { Id = userId });
        _mockUserManager.Setup(m => m.GetRolesAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(new List<string> { role });
    }

    [Fact]
    public async Task GetUnreadCount_ReturnsOk()
    {
        // Arrange
        SetUser("user-1");
        _mockSystemNotifier.Setup(s => s.GetUnreadCountAsync("user-1")).ReturnsAsync(5);

        // Act
        var result = await _controller.GetUnreadCount();

        // Assert
        var ok = result.Result as OkObjectResult;
        Assert.NotNull(ok);
        Assert.Equal(5, ok.Value);
    }

    [Fact]
    public async Task GetUnreadCount_NoUser_ReturnsUnauthorized()
    {
        // Arrange
        _mockUserManager.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns(string.Empty);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        // Act
        var result = await _controller.GetUnreadCount();

        // Assert
        Assert.IsType<UnauthorizedResult>(result.Result);
    }

    [Fact]
    public async Task MarkAsRead_ReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var result = await _controller.MarkAsRead(id);

        // Assert
        Assert.IsType<OkResult>(result);
    }

    [Fact]
    public async Task MarkAsRead_CallsServiceWithId()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        await _controller.MarkAsRead(id);

        // Assert
        _mockSystemNotifier.Verify(s => s.MarkAsReadAsync(id), Times.Once);
    }

    [Fact]
    public async Task MarkAllAsRead_WithUser_ReturnsOk()
    {
        // Arrange
        SetUser("user-1");

        // Act
        var result = await _controller.MarkAllAsRead();

        // Assert
        Assert.IsType<OkResult>(result);
    }

    [Fact]
    public async Task MarkAllAsRead_NoUser_ReturnsUnauthorized()
    {
        // Arrange
        _mockUserManager.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns(string.Empty);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        // Act
        var result = await _controller.MarkAllAsRead();

        // Assert
        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task MarkAllAsRead_CallsServiceWithUserId()
    {
        // Arrange
        SetUser("user-1");

        // Act
        await _controller.MarkAllAsRead();

        // Assert
        _mockSystemNotifier.Verify(s => s.MarkAllAsReadAsync("user-1"), Times.Once);
    }

    [Fact]
    public async Task GetNotifications_UserNotFound_ReturnsUnauthorized()
    {
        // Arrange
        SetUser("user-1");
        _mockUserManager.Setup(m => m.FindByIdAsync("user-1")).ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _controller.GetNotifications();

        // Assert
        Assert.IsType<UnauthorizedResult>(result.Result);
    }

    [Fact]
    public async Task GetNotifications_NoUser_ReturnsUnauthorized()
    {
        // Arrange
        _mockUserManager.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns(string.Empty);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        // Act
        var result = await _controller.GetNotifications();

        // Assert
        Assert.IsType<UnauthorizedResult>(result.Result);
    }

    [Fact]
    public async Task GetNotifications_ValidUser_ReturnsOk()
    {
        // Arrange
        SetUser("user-1", "Customer");
        _mockSystemNotifier.Setup(s => s.GetUserNotificationsAsync("user-1"))
            .ReturnsAsync(new List<Notification> { new Notification { UserId = "user-1" } });
        _mockPolicyManager.Setup(s => s.GetUserPoliciesAsync("user-1")).ReturnsAsync(new List<PolicyApplication>());

        // Act
        var result = await _controller.GetNotifications();

        // Assert
        Assert.IsType<OkObjectResult>(result.Result);
    }
}
