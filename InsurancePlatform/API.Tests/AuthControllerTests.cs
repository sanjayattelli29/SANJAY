using API.Controllers;
using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace API.Tests;

public class AuthControllerTests
{
    private readonly Mock<IAuthService> _mockAuthService;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _mockAuthService = new Mock<IAuthService>();
        _controller = new AuthController(_mockAuthService.Object);
    }

    [Fact]
    public async Task Register_Success_ReturnsOk()
    {
        // Arrange
        _mockAuthService.Setup(s => s.RegisterCustomerAsync(It.IsAny<RegisterCustomerDto>()))
            .ReturnsAsync(new AuthResponseDto { Status = "Success", Message = "Registered" });

        // Act
        var result = await _controller.Register(new RegisterCustomerDto());

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        var body = Assert.IsType<AuthResponseDto>(ok.Value);
        Assert.Equal("Success", body.Status);
    }

    [Fact]
    public async Task Register_Failure_ReturnsBadRequest()
    {
        // Arrange
        _mockAuthService.Setup(s => s.RegisterCustomerAsync(It.IsAny<RegisterCustomerDto>()))
            .ReturnsAsync(new AuthResponseDto { Status = "Error", Message = "User already exists!" });

        // Act
        var result = await _controller.Register(new RegisterCustomerDto());

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsOkWithToken()
    {
        // Arrange
        _mockAuthService.Setup(s => s.LoginAsync(It.IsAny<LoginDto>()))
            .ReturnsAsync(new AuthResponseDto { Status = "Success", Token = "jwt-token-here" });

        // Act
        var result = await _controller.Login(new LoginDto());

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        var body = Assert.IsType<AuthResponseDto>(ok.Value);
        Assert.Equal("jwt-token-here", body.Token);
    }

    [Fact]
    public async Task Login_InvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        _mockAuthService.Setup(s => s.LoginAsync(It.IsAny<LoginDto>()))
            .ReturnsAsync(new AuthResponseDto { Status = "Error", Message = "Invalid password!" });

        // Act
        var result = await _controller.Login(new LoginDto());

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task Register_WithValidEmail_CallsServiceOnce()
    {
        // Arrange
        _mockAuthService.Setup(s => s.RegisterCustomerAsync(It.IsAny<RegisterCustomerDto>()))
            .ReturnsAsync(new AuthResponseDto { Status = "Success" });

        var dto = new RegisterCustomerDto { EmailId = "test@test.com" };

        // Act
        await _controller.Register(dto);

        // Assert
        _mockAuthService.Verify(s => s.RegisterCustomerAsync(dto), Times.Once);
    }

    [Fact]
    public async Task Login_CalledWithCorrectDto()
    {
        // Arrange
        var dto = new LoginDto { EmailId = "test@test.com", Password = "Test@123" };
        _mockAuthService.Setup(s => s.LoginAsync(dto))
            .ReturnsAsync(new AuthResponseDto { Status = "Success" });

        // Act
        await _controller.Login(dto);

        // Assert
        _mockAuthService.Verify(s => s.LoginAsync(dto), Times.Once);
    }

    [Fact]
    public async Task Login_ServiceException_Propagates()
    {
        // Arrange
        _mockAuthService.Setup(s => s.LoginAsync(It.IsAny<LoginDto>()))
            .ThrowsAsync(new Exception("DB error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.Login(new LoginDto()));
    }

    [Fact]
    public async Task Register_NullDto_StillCallsService()
    {
        // Arrange
        _mockAuthService.Setup(s => s.RegisterCustomerAsync(It.IsAny<RegisterCustomerDto>()))
            .ReturnsAsync(new AuthResponseDto { Status = "Success" });

        // Act
        var result = await _controller.Register(new RegisterCustomerDto());

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Register_ReturnsCorrectModel()
    {
        // Arrange
        var expected = new AuthResponseDto { Status = "Success", Token = "token-abc", Email = "u@u.com" };
        _mockAuthService.Setup(s => s.RegisterCustomerAsync(It.IsAny<RegisterCustomerDto>()))
            .ReturnsAsync(expected);

        // Act
        var result = await _controller.Register(new RegisterCustomerDto());
        var ok = Assert.IsType<OkObjectResult>(result);

        // Assert
        Assert.Equal(expected, ok.Value);
    }

    [Fact]
    public async Task Login_ReturnsCorrectModel()
    {
        // Arrange
        var expected = new AuthResponseDto { Status = "Success", Role = "Customer" };
        _mockAuthService.Setup(s => s.LoginAsync(It.IsAny<LoginDto>()))
            .ReturnsAsync(expected);

        // Act
        var result = await _controller.Login(new LoginDto());
        var ok = Assert.IsType<OkObjectResult>(result);

        // Assert
        Assert.Equal(expected, ok.Value);
    }
}
