using API.Controllers;
using Application.Interfaces;
using Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using Xunit;

namespace API.Tests;

public class AgentControllerTests
{
    private readonly Mock<IPolicyService> _mockPolicyService;
    private readonly AgentController _controller;

    public AgentControllerTests()
    {
        _mockPolicyService = new Mock<IPolicyService>();
        _controller = new AgentController(_mockPolicyService.Object);
    }

    private void SetUser(string userId)
    {
        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId) };
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test")) }
        };
    }

    private void SetNoUser()
    {
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    [Fact]
    public async Task GetMyRequests_WithUser_ReturnsOk()
    {
        // Arrange
        SetUser("agent-1");
        _mockPolicyService.Setup(s => s.GetAgentApplicationsAsync("agent-1"))
            .ReturnsAsync(new List<PolicyApplication> { new PolicyApplication() });

        // Act
        var result = await _controller.GetMyRequests();

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetMyRequests_NoUser_ReturnsUnauthorized()
    {
        SetNoUser();
        var result = await _controller.GetMyRequests();
        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task ReviewRequest_Success_ReturnsOk()
    {
        // Arrange
        SetUser("agent-1");
        _mockPolicyService.Setup(s => s.ReviewApplicationAsync("app-1", "Approved", "agent-1")).ReturnsAsync(true);

        // Act
        var result = await _controller.ReviewRequest(new ReviewApplicationRequest { ApplicationId = "app-1", Status = "Approved" });

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task ReviewRequest_Failure_ReturnsBadRequest()
    {
        // Arrange
        SetUser("agent-1");
        _mockPolicyService.Setup(s => s.ReviewApplicationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(false);

        // Act
        var result = await _controller.ReviewRequest(new ReviewApplicationRequest());

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task ReviewRequest_NoUser_ReturnsUnauthorized()
    {
        SetNoUser();
        var result = await _controller.ReviewRequest(new ReviewApplicationRequest());
        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task GetMyCustomers_WithUser_ReturnsOk()
    {
        // Arrange
        SetUser("agent-1");
        _mockPolicyService.Setup(s => s.GetAgentCustomersAsync("agent-1"))
            .ReturnsAsync(new List<PolicyApplication>());

        // Act
        var result = await _controller.GetMyCustomers();

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetMyCustomers_NoUser_ReturnsUnauthorized()
    {
        SetNoUser();
        var result = await _controller.GetMyCustomers();
        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task GetCommissionStats_WithUser_ReturnsOk()
    {
        // Arrange
        SetUser("agent-1");
        _mockPolicyService.Setup(s => s.GetAgentCommissionStatsAsync("agent-1"))
            .ReturnsAsync(new AgentCommissionDto());

        // Act
        var result = await _controller.GetCommissionStats();

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetCommissionStats_NoUser_ReturnsUnauthorized()
    {
        SetNoUser();
        var result = await _controller.GetCommissionStats();
        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task GetAnalytics_WithUser_ReturnsOk()
    {
        // Arrange
        SetUser("agent-1");
        _mockPolicyService.Setup(s => s.GetAgentAnalyticsAsync("agent-1"))
            .ReturnsAsync(new AgentAnalyticsDto());

        // Act
        var result = await _controller.GetAnalytics();

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetAnalytics_NoUser_ReturnsUnauthorized()
    {
        SetNoUser();
        var result = await _controller.GetAnalytics();
        Assert.IsType<UnauthorizedResult>(result);
    }
}
