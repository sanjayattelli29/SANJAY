using API.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Xunit;

namespace API.Tests;

public class DashboardControllerTests
{
    private readonly DashboardController _controller;

    public DashboardControllerTests()
    {
        _controller = new DashboardController();
    }

    private void SetRole(string role)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, "user-1"),
            new Claim(ClaimTypes.Role, role)
        };
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test")) }
        };
    }

    [Fact]
    public void GetCustomerDashboard_ReturnsOk()
    {
        // Arrange
        SetRole("Customer");

        // Act
        var result = _controller.GetCustomerDashboard();

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public void GetAgentDashboard_ReturnsOk()
    {
        // Arrange
        SetRole("Agent");

        // Act
        var result = _controller.GetAgentDashboard();

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public void GetClaimOfficerDashboard_ReturnsOk()
    {
        // Arrange
        SetRole("ClaimOfficer");

        // Act
        var result = _controller.GetClaimOfficerDashboard();

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public void GetAdminDashboard_ReturnsOk()
    {
        // Arrange
        SetRole("Admin");

        // Act
        var result = _controller.GetAdminDashboard();

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public void GetCustomerDashboard_ReturnsWelcomeMessage()
    {
        // Act
        var result = _controller.GetCustomerDashboard() as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Customer", result.Value?.ToString() ?? "");
    }

    [Fact]
    public void GetAgentDashboard_ReturnsWelcomeMessage()
    {
        var result = _controller.GetAgentDashboard() as OkObjectResult;
        Assert.NotNull(result);
        Assert.Contains("Agent", result.Value?.ToString() ?? "");
    }

    [Fact]
    public void GetClaimOfficerDashboard_ReturnsWelcomeMessage()
    {
        var result = _controller.GetClaimOfficerDashboard() as OkObjectResult;
        Assert.NotNull(result);
        Assert.Contains("Claim Officer", result.Value?.ToString() ?? "");
    }

    [Fact]
    public void GetAdminDashboard_ReturnsWelcomeMessage()
    {
        var result = _controller.GetAdminDashboard() as OkObjectResult;
        Assert.NotNull(result);
        Assert.Contains("Admin", result.Value?.ToString() ?? "");
    }

    [Fact]
    public void GetCustomerDashboard_StatusCode200()
    {
        var result = _controller.GetCustomerDashboard() as OkObjectResult;
        Assert.Equal(200, result?.StatusCode);
    }

    [Fact]
    public void GetAdminDashboard_StatusCode200()
    {
        var result = _controller.GetAdminDashboard() as OkObjectResult;
        Assert.Equal(200, result?.StatusCode);
    }
}
