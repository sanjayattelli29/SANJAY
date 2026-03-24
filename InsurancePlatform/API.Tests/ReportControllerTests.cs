using API.Controllers;
using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace API.Tests;

public class ReportControllerTests
{
    private readonly Mock<IPolicyManager> _mockPolicyManager;
    private readonly ReportController _controller;

    public ReportControllerTests()
    {
        _mockPolicyManager = new Mock<IPolicyManager>();
        _controller = new ReportController(_mockPolicyManager.Object);
    }

    [Fact]
    public async Task GetUnifiedPayments_ReturnsOk()
    {
        // Arrange
        _mockPolicyManager.Setup(s => s.GetUnifiedPaymentsAsync())
            .ReturnsAsync(new List<UnifiedPaymentDto>());

        // Act
        var result = await _controller.GetUnifiedPayments();

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetUnifiedPayments_WithData_ReturnsAllRecords()
    {
        // Arrange
        _mockPolicyManager.Setup(s => s.GetUnifiedPaymentsAsync())
            .ReturnsAsync(new List<UnifiedPaymentDto>
            {
                new UnifiedPaymentDto { ApplicationId = "p1", PaidAmount = 1000 },
                new UnifiedPaymentDto { ApplicationId = "p2", PaidAmount = 2000 }
            });

        // Act
        var result = await _controller.GetUnifiedPayments() as OkObjectResult;

        // Assert
        var payments = Assert.IsAssignableFrom<IEnumerable<UnifiedPaymentDto>>(result?.Value);
        Assert.Equal(2, payments.Count());
    }

    [Fact]
    public async Task GetUnifiedPayments_EmptyList_ReturnsOkWithEmpty()
    {
        // Arrange
        _mockPolicyManager.Setup(s => s.GetUnifiedPaymentsAsync())
            .ReturnsAsync(new List<UnifiedPaymentDto>());

        // Act
        var result = await _controller.GetUnifiedPayments() as OkObjectResult;

        // Assert
        var payments = Assert.IsAssignableFrom<IEnumerable<UnifiedPaymentDto>>(result?.Value);
        Assert.Empty(payments);
    }

    [Fact]
    public async Task GetUnifiedPayments_CallsServiceOnce()
    {
        // Arrange
        _mockPolicyManager.Setup(s => s.GetUnifiedPaymentsAsync())
            .ReturnsAsync(new List<UnifiedPaymentDto>());

        // Act
        await _controller.GetUnifiedPayments();

        // Assert
        _mockPolicyManager.Verify(s => s.GetUnifiedPaymentsAsync(), Times.Once);
    }

    [Fact]
    public async Task GetUnifiedPayments_ServiceException_Propagates()
    {
        // Arrange
        _mockPolicyManager.Setup(s => s.GetUnifiedPaymentsAsync())
            .ThrowsAsync(new Exception("DB error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.GetUnifiedPayments());
    }

    [Fact]
    public async Task GetUnifiedPayments_StatusCode200()
    {
        // Arrange
        _mockPolicyManager.Setup(s => s.GetUnifiedPaymentsAsync()).ReturnsAsync(new List<UnifiedPaymentDto>());

        // Act
        var result = await _controller.GetUnifiedPayments() as OkObjectResult;

        // Assert
        Assert.Equal(200, result?.StatusCode);
    }

    [Fact]
    public async Task GetUnifiedPayments_SortedByAmount_ReturnsAll()
    {
        // Arrange
        var data = Enumerable.Range(1, 5)
            .Select(i => new UnifiedPaymentDto { ApplicationId = $"p{i}", PaidAmount = i * 100 })
            .ToList();
        _mockPolicyManager.Setup(s => s.GetUnifiedPaymentsAsync()).ReturnsAsync(data);

        // Act
        var result = await _controller.GetUnifiedPayments() as OkObjectResult;

        // Assert
        var payments = Assert.IsAssignableFrom<IEnumerable<UnifiedPaymentDto>>(result?.Value);
        Assert.Equal(5, payments.Count());
    }

    [Fact]
    public async Task GetUnifiedPayments_LargeDataset_ReturnsAll()
    {
        // Arrange
        var largeList = Enumerable.Range(1, 100).Select(i => new UnifiedPaymentDto()).ToList();
        _mockPolicyManager.Setup(s => s.GetUnifiedPaymentsAsync()).ReturnsAsync(largeList);

        // Act
        var result = await _controller.GetUnifiedPayments() as OkObjectResult;

        // Assert
        var payments = Assert.IsAssignableFrom<IEnumerable<UnifiedPaymentDto>>(result?.Value);
        Assert.Equal(100, payments.Count());
    }

    [Fact]
    public async Task GetUnifiedPayments_SingleRecord_ReturnsOne()
    {
        // Arrange
        _mockPolicyManager.Setup(s => s.GetUnifiedPaymentsAsync())
            .ReturnsAsync(new List<UnifiedPaymentDto> { new UnifiedPaymentDto { ApplicationId = "p1" } });

        // Act
        var result = await _controller.GetUnifiedPayments() as OkObjectResult;

        // Assert
        var payments = Assert.IsAssignableFrom<IEnumerable<UnifiedPaymentDto>>(result?.Value);
        Assert.Single(payments);
    }

    [Fact]
    public async Task GetUnifiedPayments_DoesNotModifyData()
    {
        // Arrange
        var original = new List<UnifiedPaymentDto> { new UnifiedPaymentDto { ApplicationId = "app-xyz", PaidAmount = 9999 } };
        _mockPolicyManager.Setup(s => s.GetUnifiedPaymentsAsync()).ReturnsAsync(original);

        // Act
        var result = await _controller.GetUnifiedPayments() as OkObjectResult;

        // Assert
        var payments = (result?.Value as IEnumerable<UnifiedPaymentDto>)?.ToList();
        Assert.Equal("app-xyz", payments?.First().ApplicationId);
        Assert.Equal(9999m, payments?.First().PaidAmount);
    }
}
