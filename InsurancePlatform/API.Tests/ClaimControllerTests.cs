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

public class ClaimControllerTests
{
    private readonly Mock<IClaimProcessor> _mockClaimProcessor;
    private readonly Mock<IFileStorageService> _mockFileStorageService;
    private readonly ClaimController _controller;

    public ClaimControllerTests()
    {
        _mockClaimProcessor = new Mock<IClaimProcessor>();
        _mockFileStorageService = new Mock<IFileStorageService>();
        _controller = new ClaimController(_mockClaimProcessor.Object, _mockFileStorageService.Object);
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
    public async Task GetMyClaims_WithUser_ReturnsOk()
    {
        // Arrange
        SetUser("cust-1");
        _mockClaimProcessor.Setup(s => s.GetCustomerClaimsAsync("cust-1"))
            .ReturnsAsync(new List<InsuranceClaim> { new InsuranceClaim() });

        // Act
        var result = await _controller.GetMyClaims();

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetMyClaims_NoUser_ReturnsUnauthorized()
    {
        _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        var result = await _controller.GetMyClaims();
        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task GetPendingClaims_ReturnsOk()
    {
        // Arrange
        _mockClaimProcessor.Setup(s => s.GetPendingClaimsAsync())
            .ReturnsAsync(new List<InsuranceClaim> { new InsuranceClaim() });

        // Act
        var result = await _controller.GetPendingClaims();

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetClaimOfficers_ReturnsOk()
    {
        // Arrange
        _mockClaimProcessor.Setup(s => s.GetClaimOfficersWithWorkloadAsync())
            .ReturnsAsync(new List<ClaimOfficerWorkloadDto> { new ClaimOfficerWorkloadDto() });

        // Act
        var result = await _controller.GetClaimOfficers();

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task AssignOfficer_Success_ReturnsOk()
    {
        // Arrange
        _mockClaimProcessor.Setup(s => s.AssignClaimOfficerAsync("cl-1", "off-1")).ReturnsAsync(true);

        // Act
        var result = await _controller.AssignOfficer(new AssignOfficerRequest { ClaimId = "cl-1", OfficerId = "off-1" });

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task AssignOfficer_Failure_ReturnsBadRequest()
    {
        // Arrange
        _mockClaimProcessor.Setup(s => s.AssignClaimOfficerAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(false);

        // Act
        var result = await _controller.AssignOfficer(new AssignOfficerRequest());

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task GetOfficerRequests_WithOfficer_ReturnsOk()
    {
        // Arrange
        SetUser("off-1", "ClaimOfficer");
        _mockClaimProcessor.Setup(s => s.GetOfficerClaimsAsync("off-1")).ReturnsAsync(new List<InsuranceClaim>());

        // Act
        var result = await _controller.GetOfficerRequests();

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task ReviewClaim_Success_ReturnsOk()
    {
        // Arrange
        SetUser("off-1", "ClaimOfficer");
        _mockClaimProcessor.Setup(s => s.ReviewClaimAsync("cl-1", "Approved", "off-1", "OK", 1000)).ReturnsAsync(true);

        // Act
        var result = await _controller.ReviewClaim(new ReviewClaimRequest { ClaimId = "cl-1", Status = "Approved", Remarks = "OK", ApprovedAmount = 1000 });

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task ReviewClaim_Failure_ReturnsBadRequest()
    {
        // Arrange
        SetUser("off-1", "ClaimOfficer");
        _mockClaimProcessor.Setup(s => s.ReviewClaimAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>())).ReturnsAsync(false);

        // Act
        var result = await _controller.ReviewClaim(new ReviewClaimRequest());

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task GetClaimByPolicyId_ReturnsOk()
    {
        // Arrange
        _mockClaimProcessor.Setup(s => s.GetClaimByPolicyIdAsync("pol-1")).ReturnsAsync(new InsuranceClaim());

        // Act
        var result = await _controller.GetClaimByPolicyId("pol-1");

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }
}
