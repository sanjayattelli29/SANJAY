using API.Controllers;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace API.Tests;

public class PaymentControllerTests
{
    private readonly Mock<IPolicyManager> _mockPolicyManager;
    private readonly PaymentController _controller;

    public PaymentControllerTests()
    {
        _mockPolicyManager = new Mock<IPolicyManager>();
        _controller = new PaymentController(_mockPolicyManager.Object);
    }

    [Fact]
    public async Task ProcessPayment_Success_ReturnsOk()
    {
        // Arrange
        _mockPolicyManager.Setup(s => s.ProcessPaymentAsync("app-1", 1000m, It.IsAny<string>())).ReturnsAsync(true);

        // Act
        var result = await _controller.ProcessPayment(new PaymentProcessRequest { ApplicationId = "app-1", Amount = 1000 });

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task ProcessPayment_Failure_ReturnsBadRequest()
    {
        // Arrange
        _mockPolicyManager.Setup(s => s.ProcessPaymentAsync(It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string>())).ReturnsAsync(false);

        // Act
        var result = await _controller.ProcessPayment(new PaymentProcessRequest { ApplicationId = "app-1", Amount = 1000 });

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task ProcessPayment_ZeroAmount_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.ProcessPayment(new PaymentProcessRequest { Amount = 0 });

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task ProcessPayment_NegativeAmount_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.ProcessPayment(new PaymentProcessRequest { Amount = -100 });

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
        _mockPolicyManager.Verify(s => s.ProcessPaymentAsync(It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ProcessPayment_Success_ReturnsTxnId()
    {
        // Arrange
        _mockPolicyManager.Setup(s => s.ProcessPaymentAsync(It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string>())).ReturnsAsync(true);

        // Act
        var result = await _controller.ProcessPayment(new PaymentProcessRequest { ApplicationId = "app-1", Amount = 500 }) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Contains("TXN-", result.Value?.ToString() ?? "");
    }

    [Fact]
    public async Task ProcessPayment_ValidAmount_CallsService()
    {
        // Arrange
        _mockPolicyManager.Setup(s => s.ProcessPaymentAsync("app-1", 1000m, It.IsAny<string>())).ReturnsAsync(true);

        // Act
        await _controller.ProcessPayment(new PaymentProcessRequest { ApplicationId = "app-1", Amount = 1000m });

        // Assert
        _mockPolicyManager.Verify(s => s.ProcessPaymentAsync("app-1", 1000m, It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task ProcessPayment_Success_StatusIsSuccess()
    {
        // Arrange
        _mockPolicyManager.Setup(s => s.ProcessPaymentAsync(It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string>())).ReturnsAsync(true);

        // Act
        var result = await _controller.ProcessPayment(new PaymentProcessRequest { ApplicationId = "app-1", Amount = 200 }) as OkObjectResult;
        var value = result?.Value?.ToString() ?? "";

        // Assert
        Assert.Contains("Success", value);
    }

    [Fact]
    public async Task ProcessPayment_VerySmallAmount_Passes()
    {
        // Arrange
        _mockPolicyManager.Setup(s => s.ProcessPaymentAsync(It.IsAny<string>(), 0.01m, It.IsAny<string>())).ReturnsAsync(true);

        // Act
        var result = await _controller.ProcessPayment(new PaymentProcessRequest { Amount = 0.01m });

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task ProcessPayment_LargeAmount_Passes()
    {
        // Arrange
        _mockPolicyManager.Setup(s => s.ProcessPaymentAsync(It.IsAny<string>(), 999999m, It.IsAny<string>())).ReturnsAsync(true);

        // Act
        var result = await _controller.ProcessPayment(new PaymentProcessRequest { Amount = 999999m });

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task ProcessPayment_GeneratesUniqueTxnIds()
    {
        // Arrange
        _mockPolicyManager.Setup(s => s.ProcessPaymentAsync(It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string>())).ReturnsAsync(true);

        // Act
        var result1 = await _controller.ProcessPayment(new PaymentProcessRequest { Amount = 100 }) as OkObjectResult;
        var result2 = await _controller.ProcessPayment(new PaymentProcessRequest { Amount = 200 }) as OkObjectResult;

        // Assert — each value should differ because of Guid
        Assert.NotEqual(result1?.Value?.ToString(), result2?.Value?.ToString());
    }
}
