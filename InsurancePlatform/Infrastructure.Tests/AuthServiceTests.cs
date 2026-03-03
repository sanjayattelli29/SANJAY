using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.DTOs;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace Infrastructure.Tests;

public class AuthServiceTests
{
    private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
    private readonly Mock<RoleManager<IdentityRole>> _mockRoleManager;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
        _mockUserManager = new Mock<UserManager<ApplicationUser>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        
        var roleStoreMock = new Mock<IRoleStore<IdentityRole>>();
        _mockRoleManager = new Mock<RoleManager<IdentityRole>>(
            roleStoreMock.Object, null!, null!, null!, null!);

        _mockConfiguration = new Mock<IConfiguration>();
        
        // Mocking IConfiguration values
        _mockConfiguration.Setup(c => c["JWT:SecretKey"]).Returns("very_long_secret_key_for_testing_purposes_only");
        _mockConfiguration.Setup(c => c["JWT:Issuer"]).Returns("TestIssuer");
        _mockConfiguration.Setup(c => c["JWT:Audience"]).Returns("TestAudience");
        _mockConfiguration.Setup(c => c["JWT:ExpiryMinutes"]).Returns("60");

        _authService = new AuthService(_mockUserManager.Object, _mockRoleManager.Object, _mockConfiguration.Object);
    }

    [Fact]
    public async Task RegisterCustomer_Success_ReturnsSuccessStatus()
    {
        // Arrange
        var registerDto = new RegisterCustomerDto 
        { 
            EmailId = "new@test.com", 
            Password = "Password123!", 
            Name = "New User", 
            MobileNumber = "1234567890" 
        };

        _mockUserManager.Setup(u => u.FindByEmailAsync(registerDto.EmailId)).ReturnsAsync((ApplicationUser)null!);
        _mockUserManager.Setup(u => u.CreateAsync(It.IsAny<ApplicationUser>(), registerDto.Password))
            .ReturnsAsync(IdentityResult.Success);
        _mockRoleManager.Setup(r => r.RoleExistsAsync(UserRoles.Customer)).ReturnsAsync(true);

        // Act
        var result = await _authService.RegisterCustomerAsync(registerDto);

        // Assert
        Assert.Equal("Success", result.Status);
        _mockUserManager.Verify(u => u.AddToRoleAsync(It.IsAny<ApplicationUser>(), UserRoles.Customer), Times.Once);
    }

    [Fact]
    public async Task RegisterCustomer_UserExists_ReturnsError()
    {
        // Arrange
        var registerDto = new RegisterCustomerDto { EmailId = "exists@test.com" };
        _mockUserManager.Setup(u => u.FindByEmailAsync(registerDto.EmailId)).ReturnsAsync(new ApplicationUser());

        // Act
        var result = await _authService.RegisterCustomerAsync(registerDto);

        // Assert
        Assert.Equal("Error", result.Status);
        Assert.Equal("User already exists!", result.Message);
    }

    [Fact]
    public async Task Login_Success_ReturnsToken()
    {
        // Arrange
        var loginDto = new LoginDto { EmailId = "user@test.com", Password = "CorrectPassword12!" };
        var user = new ApplicationUser { Id = "user-id", Email = loginDto.EmailId, FullName = "Test User" };
        
        _mockUserManager.Setup(u => u.FindByEmailAsync(loginDto.EmailId)).ReturnsAsync(user);
        _mockUserManager.Setup(u => u.CheckPasswordAsync(user, loginDto.Password)).ReturnsAsync(true);
        _mockUserManager.Setup(u => u.GetRolesAsync(user)).ReturnsAsync(new List<string> { UserRoles.Customer });

        // Act
        var result = await _authService.LoginAsync(loginDto);

        // Assert
        Assert.Equal("Success", result.Status);
        Assert.NotNull(result.Token);
        Assert.Equal(user.Email, result.Email);
    }

    [Fact]
    public async Task Login_InvalidPassword_ReturnsError()
    {
        // Arrange
        var loginDto = new LoginDto { EmailId = "user@test.com", Password = "WrongPassword" };
        var user = new ApplicationUser { Email = loginDto.EmailId };
        
        _mockUserManager.Setup(u => u.FindByEmailAsync(loginDto.EmailId)).ReturnsAsync(user);
        _mockUserManager.Setup(u => u.CheckPasswordAsync(user, loginDto.Password)).ReturnsAsync(false);

        // Act
        var result = await _authService.LoginAsync(loginDto);

        // Assert
        Assert.Equal("Error", result.Status);
        Assert.Equal("Invalid login attempt.", result.Message);
    }

    [Fact]
    public async Task CreateAgent_Success_ReturnsSuccess()
    {
        // Arrange
        var agentDto = new CreateAgentDto { EmailId = "agent@test.com", Password = "AgentPassword123!", Name = "Agent 007" };
        _mockUserManager.Setup(u => u.FindByEmailAsync(agentDto.EmailId)).ReturnsAsync((ApplicationUser)null!);
        _mockUserManager.Setup(u => u.CreateAsync(It.IsAny<ApplicationUser>(), agentDto.Password)).ReturnsAsync(IdentityResult.Success);
        _mockRoleManager.Setup(r => r.RoleExistsAsync(UserRoles.Agent)).ReturnsAsync(true);

        // Act
        var result = await _authService.CreateAgentAsync(agentDto);

        // Assert
        Assert.Equal("Success", result.Status);
        _mockUserManager.Verify(u => u.AddToRoleAsync(It.IsAny<ApplicationUser>(), UserRoles.Agent), Times.Once);
    }

    [Fact]
    public async Task CreateClaimOfficer_Success_ReturnsSuccess()
    {
        // Arrange
        var officerDto = new CreateClaimOfficerDto { EmailId = "officer@test.com", Password = "OfficerPassword123!", Name = "Officer Friendly" };
        _mockUserManager.Setup(u => u.FindByEmailAsync(officerDto.EmailId)).ReturnsAsync((ApplicationUser)null!);
        _mockUserManager.Setup(u => u.CreateAsync(It.IsAny<ApplicationUser>(), officerDto.Password)).ReturnsAsync(IdentityResult.Success);
        _mockRoleManager.Setup(r => r.RoleExistsAsync(UserRoles.ClaimOfficer)).ReturnsAsync(true);

        // Act
        var result = await _authService.CreateClaimOfficerAsync(officerDto);

        // Assert
        Assert.Equal("Success", result.Status);
        _mockUserManager.Verify(u => u.AddToRoleAsync(It.IsAny<ApplicationUser>(), UserRoles.ClaimOfficer), Times.Once);
    }

    [Fact]
    public async Task GetUsersByRole_ReturnsFilteredList()
    {
        // Arrange
        var users = new List<ApplicationUser> 
        { 
            new ApplicationUser { Id = "1", FullName = "User 1", Email = "u1@test.com" },
            new ApplicationUser { Id = "2", FullName = "User 2", Email = "u2@test.com" }
        };
        _mockUserManager.Setup(u => u.GetUsersInRoleAsync(UserRoles.Agent)).ReturnsAsync(users);

        // Act
        var result = await _authService.GetUsersByRoleAsync(UserRoles.Agent);

        // Assert
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task DeleteUser_Success_ReturnsSuccess()
    {
        // Arrange
        var user = new ApplicationUser { Id = "user-id" };
        _mockUserManager.Setup(u => u.FindByIdAsync("user-id")).ReturnsAsync(user);
        _mockUserManager.Setup(u => u.DeleteAsync(user)).ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _authService.DeleteUserAsync("user-id");

        // Assert
        Assert.Equal("Success", result.Status);
    }

    [Fact]
    public async Task DeleteUser_NotFound_ReturnsError()
    {
        // Arrange
        _mockUserManager.Setup(u => u.FindByIdAsync("wrong-id")).ReturnsAsync((ApplicationUser)null!);

        // Act
        var result = await _authService.DeleteUserAsync("wrong-id");

        // Assert
        Assert.Equal("Error", result.Status);
        Assert.Equal("User not found!", result.Message);
    }

    [Fact]
    public async Task RegisterCustomer_RoleMissing_CreatesRole()
    {
        // Arrange
        var registerDto = new RegisterCustomerDto { EmailId = "new@test.com", Password = "Password123!" };
        _mockUserManager.Setup(u => u.FindByEmailAsync(registerDto.EmailId)).ReturnsAsync((ApplicationUser)null!);
        _mockUserManager.Setup(u => u.CreateAsync(It.IsAny<ApplicationUser>(), registerDto.Password)).ReturnsAsync(IdentityResult.Success);
        _mockRoleManager.Setup(r => r.RoleExistsAsync(UserRoles.Customer)).ReturnsAsync(false);
        _mockRoleManager.Setup(r => r.CreateAsync(It.IsAny<IdentityRole>())).ReturnsAsync(IdentityResult.Success);

        // Act
        await _authService.RegisterCustomerAsync(registerDto);

        // Assert
        _mockRoleManager.Verify(r => r.CreateAsync(It.Is<IdentityRole>(ir => ir.Name == UserRoles.Customer)), Times.Once);
    }
}
