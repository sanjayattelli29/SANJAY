using Application.Interfaces.Infrastructure;
using Application.Interfaces.Services;
using Application.Interfaces;
using Application.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain.Entities;
using Infrastructure.Data;
using Infrastructure.Hubs;
using Infrastructure.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Infrastructure.Tests;

public class NotificationServiceTests
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<INotificationBroadcaster> _mockBroadcaster;
    private readonly SystemNotifier _notificationService;

    public NotificationServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);

        var repo = new Infrastructure.Repositories.NotificationRepository(_context);
        _mockBroadcaster = new Mock<INotificationBroadcaster>();

        _notificationService = new SystemNotifier(repo, _mockBroadcaster.Object);
    }

    [Fact]
    public async Task SendNotification_SavesToDatabase()
    {
        // Act
        await _notificationService.SendNotificationAsync("user-1", "Title", "Message");

        // Assert
        Assert.Equal(1, _context.Notifications.Count());
        var notification = _context.Notifications.First();
        Assert.Equal("user-1", notification.UserId);
        Assert.Equal("Title", notification.Title);
    }

    [Fact]
    public async Task SendNotification_NotifiesViaSignalR()
    {
        // Act
        await _notificationService.SendNotificationAsync("user-1", "Title", "Message");

        // Assert
        _mockBroadcaster.Verify(
            b => b.BroadcastNotificationAsync("user-1", It.IsAny<Notification>()),
            Times.Once);
    }

    [Fact]
    public async Task GetUserNotifications_ReturnsOnlyTargetUser()
    {
        // Arrange
        _context.Notifications.Add(new Notification { UserId = "user-1", Title = "T1" });
        _context.Notifications.Add(new Notification { UserId = "user-2", Title = "T2" });
        await _context.SaveChangesAsync();

        // Act
        var result = await _notificationService.GetUserNotificationsAsync("user-1");

        // Assert
        Assert.Single(result);
        Assert.Equal("user-1", result.First().UserId);
    }

    [Fact]
    public async Task GetUserNotifications_RespectsLimit()
    {
        // Arrange
        for (int i = 0; i < 60; i++)
        {
            _context.Notifications.Add(new Notification { UserId = "user-1", Title = $"T{i}" });
        }
        await _context.SaveChangesAsync();

        // Act
        var result = await _notificationService.GetUserNotificationsAsync("user-1");

        // Assert
        Assert.Equal(50, result.Count());
    }

    [Fact]
    public async Task GetUnreadCount_ReturnsCorrectCount()
    {
        // Arrange
        _context.Notifications.Add(new Notification { UserId = "user-1", IsRead = false });
        _context.Notifications.Add(new Notification { UserId = "user-1", IsRead = false });
        _context.Notifications.Add(new Notification { UserId = "user-1", IsRead = true });
        await _context.SaveChangesAsync();

        // Act
        var count = await _notificationService.GetUnreadCountAsync("user-1");

        // Assert
        Assert.Equal(2, count);
    }

    [Fact]
    public async Task MarkAsRead_UpdatesSpecificNotification()
    {
        // Arrange
        var id = Guid.NewGuid();
        _context.Notifications.Add(new Notification { Id = id, IsRead = false });
        await _context.SaveChangesAsync();

        // Act
        await _notificationService.MarkAsReadAsync(id);

        // Assert
        var n = await _context.Notifications.FindAsync(id);
        Assert.True(n?.IsRead);
    }

    [Fact]
    public async Task MarkAllAsRead_UpdatesAllUserNotifications()
    {
        // Arrange
        _context.Notifications.Add(new Notification { UserId = "user-1", IsRead = false });
        _context.Notifications.Add(new Notification { UserId = "user-1", IsRead = false });
        _context.Notifications.Add(new Notification { UserId = "user-2", IsRead = false });
        await _context.SaveChangesAsync();

        // Act
        await _notificationService.MarkAllAsReadAsync("user-1");

        // Assert
        Assert.Equal(0, await _notificationService.GetUnreadCountAsync("user-1"));
        Assert.Equal(1, await _notificationService.GetUnreadCountAsync("user-2"));
    }

    [Fact]
    public async Task GetUserNotifications_OrderedByDateDescending()
    {
        // Arrange
        var n1 = new Notification { UserId = "user-1", CreatedAt = DateTime.UtcNow.AddMinutes(-10) };
        var n2 = new Notification { UserId = "user-1", CreatedAt = DateTime.UtcNow };
        _context.Notifications.AddRange(n1, n2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _notificationService.GetUserNotificationsAsync("user-1");

        // Assert
        Assert.Equal(n2.Id, result.First().Id);
    }

    [Fact]
    public async Task MarkAsRead_NonExistent_DoesNothing()
    {
        // Act
        var id = Guid.NewGuid();
        await _notificationService.MarkAsReadAsync(id);
        // Should not throw
    }

    [Fact]
    public async Task SendNotification_DefaultType_IsGeneral()
    {
        // Act
        await _notificationService.SendNotificationAsync("user-1", "T", "M");

        // Assert
        var n = _context.Notifications.First();
        Assert.Equal("General", n.NotificationType);
    }
}
