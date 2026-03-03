using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Data;
using Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Infrastructure.Tests;

public class PolicyServiceTests
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
    private readonly Mock<INotificationService> _mockNotification;
    private readonly PolicyService _policyService;

    public PolicyServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);

        var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
        _mockUserManager = new Mock<UserManager<ApplicationUser>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        
        _mockNotification = new Mock<INotificationService>();

        _policyService = new PolicyService(_context, _mockUserManager.Object, _mockNotification.Object);
    }

    [Fact]
    public async Task CalculatePremium_Individual_BaseCase()
    {
        // Arrange
        var request = new PolicyApplicationRequest
        {
            PolicyCategory = "INDIVIDUAL",
            TierId = "BASIC",
            PaymentMode = "yearly",
            Applicant = new ApplicantDetails
            {
                Age = 30,
                Profession = "White Collar",
                SmokingHabit = "nonsmoker",
                AlcoholHabit = "nondrinker",
                TravelKmPerMonth = 500
            }
        };

        // Act
        var premium = await _policyService.CalculatePremiumAsync(request);

        // Assert
        Assert.True(premium > 0);
        Assert.Equal(1000m, premium); // Based on our test json
    }

    [Fact]
    public async Task ApplyForPolicy_Success_CreatesApplication()
    {
        // Arrange
        var userId = "user-123";
        var request = new PolicyApplicationRequest
        {
            PolicyCategory = "INDIVIDUAL",
            TierId = "BASIC",
            PaymentMode = "yearly",
            Applicant = new ApplicantDetails { Age = 25, Profession = "Student", SmokingHabit = "nonsmoker", AlcoholHabit = "nondrinker" }
        };

        _mockUserManager.Setup(u => u.FindByIdAsync(userId)).ReturnsAsync(new ApplicationUser { Id = userId, Email = "test@test.com" });
        _mockUserManager.Setup(u => u.GetUsersInRoleAsync(UserRoles.Admin)).ReturnsAsync(new List<ApplicationUser>());

        // Act
        var result = await _policyService.ApplyForPolicyAsync(userId, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, _context.PolicyApplications.Count());
    }

    [Fact]
    public async Task ApplyForPolicy_DuplicateActive_ThrowsException()
    {
        // Arrange
        var userId = "user-123";
        _context.PolicyApplications.Add(new PolicyApplication 
        { 
            UserId = userId, 
            PolicyCategory = "INDIVIDUAL", 
            Status = "Active" 
        });
        await _context.SaveChangesAsync();

        var request = new PolicyApplicationRequest { PolicyCategory = "INDIVIDUAL" };

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _policyService.ApplyForPolicyAsync(userId, request));
    }

    [Fact]
    public async Task AssignAgent_UpdatesStatusAndNotifies()
    {
        // Arrange
        var app = new PolicyApplication { Id = "app-1", UserId = "cust-1", TierId = "GOLD" };
        _context.PolicyApplications.Add(app);
        await _context.SaveChangesAsync();

        _mockUserManager.Setup(u => u.FindByIdAsync("agent-1"))
            .ReturnsAsync(new ApplicationUser { Id = "agent-1", Email = "agent@test.com" });
        _mockUserManager.Setup(u => u.FindByIdAsync("cust-1"))
            .ReturnsAsync(new ApplicationUser { Id = "cust-1", Email = "cust@test.com" });

        // Act
        var result = await _policyService.AssignAgentAsync("app-1", "agent-1");

        // Assert
        Assert.True(result);
        var updatedApp = await _context.PolicyApplications.FindAsync("app-1");
        Assert.Equal("Assigned", updatedApp?.Status);
        Assert.Equal("agent-1", updatedApp?.AssignedAgentId);
        _mockNotification.Verify(n => n.SendNotificationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(2));
    }

    [Fact]
    public async Task ReviewApplication_Approve_UpdatesStatusToAwaitingPayment()
    {
        // Arrange
        var app = new PolicyApplication { Id = "app-1", AssignedAgentId = "agent-1", TierId = "GOLD" };
        _context.PolicyApplications.Add(app);
        await _context.SaveChangesAsync();

        // Act
        var result = await _policyService.ReviewApplicationAsync("app-1", "Approved", "agent-1");

        // Assert
        Assert.True(result);
        var updatedApp = await _context.PolicyApplications.FindAsync("app-1");
        Assert.Equal("AwaitingPayment", updatedApp?.Status);
    }

    [Fact]
    public async Task ProcessPayment_ActivatesPolicy()
    {
        // Arrange
        var app = new PolicyApplication 
        { 
            Id = "app-1", 
            Status = "AwaitingPayment", 
            UserId = "cust-1",
            PolicyCategory = "INDIVIDUAL",
            TierId = "BASIC",
            ApplicationDataJson = "{\"paymentMode\":\"monthly\"}"
        };
        _context.PolicyApplications.Add(app);
        await _context.SaveChangesAsync();

        _mockUserManager.Setup(u => u.FindByIdAsync("cust-1")).ReturnsAsync(new ApplicationUser { Id = "cust-1", Email = "cust@test.com" });
        _mockUserManager.Setup(u => u.GetUsersInRoleAsync(UserRoles.Admin)).ReturnsAsync(new List<ApplicationUser>());

        // Act
        var result = await _policyService.ProcessPaymentAsync("app-1", 1000m, "TXN-123");

        // Assert
        Assert.True(result);
        var updatedApp = await _context.PolicyApplications.FindAsync("app-1");
        Assert.Equal("Active", updatedApp?.Status);
        Assert.Equal(1000m, updatedApp?.PaidAmount);
        Assert.NotNull(updatedApp?.ExpiryDate);
    }

    [Fact]
    public async Task GetAgentsWithWorkload_ReturnsCorrectCounts()
    {
        // Arrange
        var agent = new ApplicationUser { Id = "agent-1", Email = "agent@test.com" };
        _mockUserManager.Setup(u => u.GetUsersInRoleAsync(UserRoles.Agent))
            .ReturnsAsync(new List<ApplicationUser> { agent });
        
        _context.PolicyApplications.Add(new PolicyApplication { Id = "app-1", AssignedAgentId = "agent-1" });
        _context.PolicyApplications.Add(new PolicyApplication { Id = "app-2", AssignedAgentId = "agent-1" });
        await _context.SaveChangesAsync();

        // Act
        var result = await _policyService.GetAgentsWithWorkloadAsync();

        // Assert
        var agentWorkload = result.First();
        Assert.Equal(2, agentWorkload.AssignedPolicyCount);
    }

    [Fact]
    public async Task GetAgentAnalytics_CalculatesCorrectTotals()
    {
        // Arrange
        var agentId = "agent-1";
        var userId = "user-1";
        var user = new ApplicationUser { Id = userId, Email = "cust@test.com" };
        _context.Users.Add(user);

        _context.PolicyApplications.Add(new PolicyApplication 
        { 
            Id = "app-1", 
            UserId = userId,
            AssignedAgentId = agentId, 
            Status = "Active", 
            PaidAmount = 5000, 
            CalculatedPremium = 5000,
            TotalCoverageAmount = 100000,
            RemainingCoverageAmount = 100000,
            PaymentDate = DateTime.UtcNow,
            User = user
        });
        await _context.SaveChangesAsync();

        // Act
        var analytics = await _policyService.GetAgentAnalyticsAsync(agentId);

        // Assert
        Assert.Equal(5000, analytics.TotalPremiumCollected);
        Assert.Equal(500, analytics.TotalCommissionEarned); // 10% of 5000
        Assert.Equal(100000, analytics.TotalCoverageProvided);
    }

    [Fact]
    public async Task ReviewApplication_WrongAgent_ReturnsFalse()
    {
        // Arrange
        var app = new PolicyApplication { Id = "app-1", AssignedAgentId = "agent-1" };
        _context.PolicyApplications.Add(app);
        await _context.SaveChangesAsync();

        // Act
        var result = await _policyService.ReviewApplicationAsync("app-1", "Approved", "wrong-agent");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task CalculatePremium_InvalidCategory_ThrowsException()
    {
        // Arrange
        var request = new PolicyApplicationRequest { PolicyCategory = "INVALID" };

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _policyService.CalculatePremiumAsync(request));
    }
}
