using API.Controllers;
using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace API.Tests;

public class AdminControllerTests
{
    private readonly Mock<IIdentityService> _mockIdentityService;
    private readonly Mock<IPolicyManager> _mockPolicyManager;
    private readonly Mock<IClaimProcessor> _mockClaimProcessor;
    private readonly AdminController _controller;

    public AdminControllerTests()
    {
        _mockIdentityService = new Mock<IIdentityService>();
        _mockPolicyManager = new Mock<IPolicyManager>();
        _mockClaimProcessor = new Mock<IClaimProcessor>();
        _controller = new AdminController(_mockIdentityService.Object, _mockPolicyManager.Object, _mockClaimProcessor.Object);
    }

    [Fact]
    public async Task CreateAgent_ReturnsOk()
    {
        // Arrange
        _mockIdentityService.Setup(s => s.CreateAgentAsync(It.IsAny<CreateAgentDto>()))
            .ReturnsAsync(new AuthResponseDto { Status = "Success" });

        // Act
        var result = await _controller.CreateAgent(new CreateAgentDto());

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task CreateClaimOfficer_ReturnsOk()
    {
        // Arrange
        _mockIdentityService.Setup(s => s.CreateClaimOfficerAsync(It.IsAny<CreateClaimOfficerDto>()))
            .ReturnsAsync(new AuthResponseDto { Status = "Success" });

        // Act
        var result = await _controller.CreateClaimOfficer(new CreateClaimOfficerDto());

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetAgents_ReturnsOkWithList()
    {
        // Arrange
        _mockIdentityService.Setup(s => s.GetUsersByRoleAsync("Agent"))
            .ReturnsAsync(new List<UserListingDto> { new UserListingDto { Email = "agent@test.com" } });

        // Act
        var result = await _controller.GetAgents();

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
    }

    [Fact]
    public async Task GetClaimOfficers_ReturnsOkWithList()
    {
        // Arrange
        _mockIdentityService.Setup(s => s.GetUsersByRoleAsync("ClaimOfficer"))
            .ReturnsAsync(new List<UserListingDto> { new UserListingDto() });

        // Act
        var result = await _controller.GetClaimOfficers();

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task DeleteUser_ReturnsOk()
    {
        // Arrange
        _mockIdentityService.Setup(s => s.DeleteUserAsync("user-1"))
            .ReturnsAsync(new AuthResponseDto { Status = "Success" });

        // Act
        var result = await _controller.DeleteUser("user-1");

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetAllPolicyRequests_ReturnsOkWithList()
    {
        // Arrange
        _mockPolicyManager.Setup(s => s.GetAllApplicationsAsync())
            .ReturnsAsync(new List<PolicyApplication> { new PolicyApplication() });

        // Act
        var result = await _controller.GetAllPolicyRequests();

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task AssignAgent_Success_ReturnsOk()
    {
        // Arrange
        _mockPolicyManager.Setup(s => s.AssignAgentAsync("app-1", "agent-1")).ReturnsAsync(true);

        // Act
        var result = await _controller.AssignAgent(new AssignAgentRequest { ApplicationId = "app-1", AgentId = "agent-1" });

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task AssignAgent_Failure_ReturnsBadRequest()
    {
        // Arrange
        _mockPolicyManager.Setup(s => s.AssignAgentAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(false);

        // Act
        var result = await _controller.AssignAgent(new AssignAgentRequest());

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task GetAllUsers_ReturnsOk()
    {
        // Arrange
        _mockIdentityService.Setup(s => s.GetAllUsersAsync()).ReturnsAsync(new List<UserListingDto>());

        // Act
        var result = await _controller.GetAllUsers();

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetAdminStats_ReturnsOk()
    {
        // Arrange
        _mockClaimProcessor.Setup(s => s.GetAdminStatsAsync()).ReturnsAsync(new AdminDashboardStatsDto());

        // Act
        var result = await _controller.GetAdminStats();

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }
}
