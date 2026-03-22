using Application.Interfaces.Infrastructure;
using Application.Interfaces.Services;
using Application.Interfaces;
using Application.Services;
using System;
using System.Collections.Generic;
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

public class ClaimServiceTests
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
    private readonly Mock<IFileStorageService> _mockFileStorage;
    private readonly Mock<ISystemNotifier> _mockNotification;
    private readonly ClaimProcessor _claimService;

    public ClaimServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);

        var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
        _mockUserManager = new Mock<UserManager<ApplicationUser>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        
        _mockFileStorage = new Mock<IFileStorageService>();
        _mockNotification = new Mock<ISystemNotifier>();

        var claimRepo = new Infrastructure.Repositories.ClaimRepository(_context);
        var policyRepo = new Infrastructure.Repositories.PolicyRepository(_context);
        _claimService = new ClaimProcessor(claimRepo, policyRepo, _mockUserManager.Object, _mockFileStorage.Object, _mockNotification.Object);
    }

    [Fact]
    public async Task AssignClaimOfficer_ShouldUpdateStatusAndNotify()
    {
        // arrange
        var policy = new PolicyApplication { Id = "policy-123", UserId = "user-123" };
        var claim = new InsuranceClaim 
        { 
            Id = "claim-123", 
            Status = "Pending", 
            UserId = "user-123", 
            PolicyApplicationId = "policy-123",
            IncidentType = "Accident"
        };
        
        _context.PolicyApplications.Add(policy);
        _context.InsuranceClaims.Add(claim);
        await _context.SaveChangesAsync();

        _mockUserManager.Setup(u => u.FindByIdAsync(It.IsAny<string>()))
            .ReturnsAsync(new ApplicationUser { Id = "officer-456", Email = "officer@test.com" });

        // act
        var result = await _claimService.AssignClaimOfficerAsync("claim-123", "officer-456");

        // assert
        Assert.True(result);
        var updatedClaim = await _context.InsuranceClaims.FindAsync("claim-123");
        Assert.Equal("Assigned", updatedClaim?.Status);
        Assert.Equal("officer-456", updatedClaim?.AssignedClaimOfficerId);
    }

    [Fact]
    public async Task RaiseClaim_Success_CreatesClaimAndNotifies()
    {
        // Arrange
        var userId = "user-1";
        var policy = new PolicyApplication 
        { 
            Id = "pol-1", 
            UserId = userId, 
            Status = "Active", 
            ExpiryDate = DateTime.UtcNow.AddYears(1) 
        };
        _context.PolicyApplications.Add(policy);
        await _context.SaveChangesAsync();

        var request = new RaiseClaimRequest 
        { 
            PolicyApplicationId = "pol-1", 
            IncidentType = "Theft", 
            RequestedAmount = 1000 
        };

        _mockUserManager.Setup(u => u.GetUsersInRoleAsync(UserRoles.Admin)).ReturnsAsync(new List<ApplicationUser>());
        _mockUserManager.Setup(u => u.FindByIdAsync(userId)).ReturnsAsync(new ApplicationUser { Id = userId, Email = "cust@test.com" });

        // Act
        var result = await _claimService.RaiseClaimAsync(userId, request);

        // Assert
        Assert.Equal("Success", result.Status);
        Assert.Equal(1, _context.InsuranceClaims.Count());
        _mockNotification.Verify(n => n.SendNotificationAsync(userId, "Claim Raised", It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task RaiseClaim_PolicyExpired_ThrowsException()
    {
        // Arrange
        var userId = "user-1";
        var policy = new PolicyApplication 
        { 
            Id = "pol-1", 
            UserId = userId, 
            Status = "Active", 
            ExpiryDate = DateTime.UtcNow.AddDays(-1) 
        };
        _context.PolicyApplications.Add(policy);
        await _context.SaveChangesAsync();

        var request = new RaiseClaimRequest { PolicyApplicationId = "pol-1" };

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _claimService.RaiseClaimAsync(userId, request));
    }

    [Fact]
    public async Task ReviewClaim_Approve_UpdatesStatusAndCoverage()
    {
        // Arrange
        var officerId = "off-1";
        var policy = new PolicyApplication 
        { 
            Id = "pol-1", 
            TotalCoverageAmount = 10000, 
            RemainingCoverageAmount = 10000,
            TotalApprovedClaimsAmount = 0
        };
        var claim = new InsuranceClaim 
        { 
            Id = "cl-1", 
            PolicyApplicationId = "pol-1", 
            AssignedClaimOfficerId = officerId,
            Status = "Assigned",
            IncidentType = "Accident",
            UserId = "cust-1"
        };
        _context.PolicyApplications.Add(policy);
        _context.InsuranceClaims.Add(claim);
        await _context.SaveChangesAsync();

        // Act
        var result = await _claimService.ReviewClaimAsync("cl-1", "Approved", officerId, "Good proof", 5000);

        // Assert
        Assert.True(result);
        var updatedClaim = await _context.InsuranceClaims.FindAsync("cl-1");
        var updatedPolicy = await _context.PolicyApplications.FindAsync("pol-1");
        Assert.Equal("Approved", updatedClaim?.Status);
        Assert.Equal(5000, updatedClaim?.ApprovedAmount);
        Assert.Equal(5000, updatedPolicy?.RemainingCoverageAmount);
        Assert.Equal(5000, updatedPolicy?.TotalApprovedClaimsAmount);
    }

    [Fact]
    public async Task ReviewClaim_ExceedsCoverage_ThrowsException()
    {
        // Arrange
        var officerId = "off-1";
        var policy = new PolicyApplication { Id = "pol-1", RemainingCoverageAmount = 1000 };
        var claim = new InsuranceClaim { Id = "cl-1", PolicyApplicationId = "pol-1", AssignedClaimOfficerId = officerId };
        _context.PolicyApplications.Add(policy);
        _context.InsuranceClaims.Add(claim);
        await _context.SaveChangesAsync();

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _claimService.ReviewClaimAsync("cl-1", "Approved", officerId, "Remarks", 2000));
    }

    [Fact]
    public async Task GetClaimOfficersWithWorkload_ReturnsCorrectCounts()
    {
        // Arrange
        var officer = new ApplicationUser { Id = "off-1", Email = "off@test.com" };
        _mockUserManager.Setup(u => u.GetUsersInRoleAsync(UserRoles.ClaimOfficer))
            .ReturnsAsync(new List<ApplicationUser> { officer });
        
        _context.InsuranceClaims.Add(new InsuranceClaim { Id = "cl-1", AssignedClaimOfficerId = "off-1" });
        await _context.SaveChangesAsync();

        // Act
        var workload = await _claimService.GetClaimOfficersWithWorkloadAsync();

        // Assert
        var result = workload.First();
        Assert.Equal(1, result.AssignedClaimsCount);
    }

    [Fact]
    public async Task ReviewClaim_WrongOfficer_ReturnsFalse()
    {
        // Arrange
        var claim = new InsuranceClaim { Id = "cl-1", AssignedClaimOfficerId = "off-1" };
        _context.InsuranceClaims.Add(claim);
        await _context.SaveChangesAsync();

        // Act
        var result = await _claimService.ReviewClaimAsync("cl-1", "Approved", "wrong-off", "Remarks", 100);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task RaiseClaim_PolicyNotFound_ThrowsException()
    {
        // Arrange
        var request = new RaiseClaimRequest { PolicyApplicationId = "wrong-pol" };

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _claimService.RaiseClaimAsync("user-1", request));
    }

    [Fact]
    public async Task ReviewClaim_Reject_UpdatesStatusOnly()
    {
        // Arrange
        var officerId = "off-1";
        var policy = new PolicyApplication { Id = "pol-1" };
        var claim = new InsuranceClaim 
        { 
            Id = "cl-1", 
            PolicyApplicationId = "pol-1",
            AssignedClaimOfficerId = officerId,
            IncidentType = "Accident",
            UserId = "cust-1" 
        };
        _context.PolicyApplications.Add(policy);
        _context.InsuranceClaims.Add(claim);
        await _context.SaveChangesAsync();

        // Act
        var result = await _claimService.ReviewClaimAsync("cl-1", "Rejected", officerId, "No proof", 0);

        // Assert
        Assert.True(result);
        var updatedClaim = await _context.InsuranceClaims.FindAsync("cl-1");
        Assert.Equal("Rejected", updatedClaim?.Status);
    }

    [Fact]
    public async Task GetAdminStats_ReturnsData()
    {
        // Arrange
        _mockUserManager.Setup(u => u.GetUsersInRoleAsync(UserRoles.Customer)).ReturnsAsync(new List<ApplicationUser> { new ApplicationUser() });
        _mockUserManager.Setup(u => u.GetUsersInRoleAsync(UserRoles.Agent)).ReturnsAsync(new List<ApplicationUser>());
        _mockUserManager.Setup(u => u.GetUsersInRoleAsync(UserRoles.ClaimOfficer)).ReturnsAsync(new List<ApplicationUser>());
        
        _context.PolicyApplications.Add(new PolicyApplication { Id = "p1", PaidAmount = 1000, Status = "Active" });
        _context.InsuranceClaims.Add(new InsuranceClaim { Id = "c1", Status = "Approved", ApprovedAmount = 500 });
        await _context.SaveChangesAsync();

        // Act
        var stats = await _claimService.GetAdminStatsAsync();

        // Assert
        Assert.Equal(1, stats.TotalCustomers);
        Assert.Equal(1, stats.TotalPolicies);
        Assert.Equal(1000, stats.TotalPremiumCollected);
        Assert.Equal(500, stats.TotalClaimedAmount);
    }
}
