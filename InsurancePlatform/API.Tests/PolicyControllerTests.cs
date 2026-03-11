using API.Controllers;
using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using Xunit;

namespace API.Tests;

public class PolicyControllerTests
{
    private readonly Mock<IPolicyService> _mockPolicyService;
    private readonly PolicyController _controller;

    public PolicyControllerTests()
    {
        _mockPolicyService = new Mock<IPolicyService>();
        _controller = new PolicyController(_mockPolicyService.Object);
    }

    private void SetUser(string userId)
    {
        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId) };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    [Fact]
    public async Task GetConfiguration_ReturnsOk()
    {
        // Arrange
        _mockPolicyService.Setup(s => s.GetConfigurationAsync()).ReturnsAsync(new PolicyConfiguration());

        // Act
        var result = await _controller.GetConfiguration();

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task CalculatePremium_Success_ReturnsOk()
    {
        // Arrange
        _mockPolicyService.Setup(s => s.CalculatePremiumAsync(It.IsAny<PolicyApplicationRequest>()))
            .ReturnsAsync(1500m);

        // Act
        var result = await _controller.CalculatePremium(new PolicyApplicationRequest());

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
    }

    [Fact]
    public async Task CalculatePremium_Exception_ReturnsBadRequest()
    {
        // Arrange
        _mockPolicyService.Setup(s => s.CalculatePremiumAsync(It.IsAny<PolicyApplicationRequest>()))
            .ThrowsAsync(new Exception("Invalid category"));

        // Act
        var result = await _controller.CalculatePremium(new PolicyApplicationRequest());

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Apply_NoUser_ReturnsUnauthorized()
    {
        // Arrange - no user set on context
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        // Act
        var result = await _controller.Apply(new PolicyApplicationRequest());

        // Assert
        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task Apply_Success_ReturnsOk()
    {
        // Arrange
        SetUser("user-1");
        _mockPolicyService.Setup(s => s.ApplyForPolicyAsync("user-1", It.IsAny<PolicyApplicationRequest>()))
            .ReturnsAsync(new AuthResponseDto { Status = "Success" });

        // Act
        var result = await _controller.Apply(new PolicyApplicationRequest());

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Apply_DuplicatePolicy_ReturnsBadRequest()
    {
        // Arrange
        SetUser("user-1");
        _mockPolicyService.Setup(s => s.ApplyForPolicyAsync(It.IsAny<string>(), It.IsAny<PolicyApplicationRequest>()))
            .ThrowsAsync(new Exception("Already has active policy"));

        // Act
        var result = await _controller.Apply(new PolicyApplicationRequest());

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task GetMyPolicies_NoUser_ReturnsUnauthorized()
    {
        // Arrange
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        // Act
        var result = await _controller.GetMyPolicies();

        // Assert
        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task GetMyPolicies_ReturnsOkWithList()
    {
        // Arrange
        SetUser("user-1");
        _mockPolicyService.Setup(s => s.GetUserPoliciesAsync("user-1"))
            .ReturnsAsync(new List<PolicyApplication> { new PolicyApplication() });

        // Act
        var result = await _controller.GetMyPolicies();

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        var list = Assert.IsAssignableFrom<IEnumerable<PolicyApplication>>(ok.Value);
        Assert.Single(list);
    }

    [Fact]
    public async Task GetConfiguration_CallsServiceOnce()
    {
        // Arrange
        _mockPolicyService.Setup(s => s.GetConfigurationAsync()).ReturnsAsync(new PolicyConfiguration());

        // Act
        await _controller.GetConfiguration();

        // Assert
        _mockPolicyService.Verify(s => s.GetConfigurationAsync(), Times.Once);
    }

    [Fact]
    public async Task Apply_PassesUserIdToService()
    {
        // Arrange
        SetUser("user-abc");
        _mockPolicyService.Setup(s => s.ApplyForPolicyAsync("user-abc", It.IsAny<PolicyApplicationRequest>()))
            .ReturnsAsync(new AuthResponseDto { Status = "Success" });

        // Act
        await _controller.Apply(new PolicyApplicationRequest());

        // Assert
        _mockPolicyService.Verify(s => s.ApplyForPolicyAsync("user-abc", It.IsAny<PolicyApplicationRequest>()), Times.Once);
    }
}
