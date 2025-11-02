using CleverBudget.Api.Controllers;
using CleverBudget.Core.DTOs;
using CleverBudget.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace CleverBudget.Tests.Controllers;

public class AuthControllerTests
{
    private readonly Mock<IAuthService> _authServiceMock;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _authServiceMock = new Mock<IAuthService>();
        _controller = new AuthController(_authServiceMock.Object);
    }

    [Fact]
    public async Task Register_ValidData_ReturnsOkWithAuthResponse()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            FirstName = "Test",
            LastName = "User",
            Email = "test@example.com",
            Password = "Password123!",
            ConfirmPassword = "Password123!"
        };

        var authResponse = new AuthResponseDto
        {
            Token = "fake-jwt-token",
            Email = registerDto.Email,
            FirstName = registerDto.FirstName,
            LastName = registerDto.LastName
        };

        var authResult = new AuthResult
        {
            Success = true,
            Data = authResponse
        };

        _authServiceMock
            .Setup(s => s.RegisterAsync(registerDto))
            .ReturnsAsync(authResult);

        // Act
        var result = await _controller.Register(registerDto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedResponse = Assert.IsType<AuthResponseDto>(okResult.Value);
        Assert.Equal(authResponse.Token, returnedResponse.Token);
        Assert.Equal(authResponse.Email, returnedResponse.Email);
        Assert.Equal(authResponse.FirstName, returnedResponse.FirstName);
    }

    [Fact]
    public async Task Register_ServiceReturnsNull_ReturnsBadRequest()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            FirstName = "Test",
            LastName = "User",
            Email = "existing@example.com",
            Password = "Password123!",
            ConfirmPassword = "Password123!"
        };

        var authResult = new AuthResult
        {
            Success = false,
            ErrorMessage = "Já existe uma conta com esse e-mail.",
            ErrorCode = "EMAIL_ALREADY_EXISTS"
        };

        _authServiceMock
            .Setup(s => s.RegisterAsync(registerDto))
            .ReturnsAsync(authResult);

        // Act
        var result = await _controller.Register(registerDto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public async Task Register_PasswordMismatch_ReturnsBadRequest()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            FirstName = "Test",
            LastName = "User",
            Email = "test@example.com",
            Password = "Password123!",
            ConfirmPassword = "DifferentPassword123!"
        };

        var authResult = new AuthResult
        {
            Success = false,
            ErrorMessage = "As senhas não conferem.",
            ErrorCode = "PASSWORD_MISMATCH"
        };

        _authServiceMock
            .Setup(s => s.RegisterAsync(registerDto))
            .ReturnsAsync(authResult);

        // Act
        var result = await _controller.Register(registerDto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsOkWithToken()
    {
        // Arrange
        var loginDto = new LoginDto
        {
            Email = "test@example.com",
            Password = "Password123!"
        };

        var authResponse = new AuthResponseDto
        {
            Token = "fake-jwt-token",
            Email = loginDto.Email,
            FirstName = "Test",
            LastName = "User"
        };

        var authResult = new AuthResult
        {
            Success = true,
            Data = authResponse
        };

        _authServiceMock
            .Setup(s => s.LoginAsync(loginDto))
            .ReturnsAsync(authResult);

        // Act
        var result = await _controller.Login(loginDto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedResponse = Assert.IsType<AuthResponseDto>(okResult.Value);
        Assert.Equal(authResponse.Token, returnedResponse.Token);
        Assert.Equal(authResponse.Email, returnedResponse.Email);
    }

    [Fact]
    public async Task Login_InvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        var loginDto = new LoginDto
        {
            Email = "test@example.com",
            Password = "WrongPassword"
        };

        var authResult = new AuthResult
        {
            Success = false,
            ErrorMessage = "E-mail ou senha incorretos.",
            ErrorCode = "INVALID_CREDENTIALS"
        };

        _authServiceMock
            .Setup(s => s.LoginAsync(loginDto))
            .ReturnsAsync(authResult);

        // Act
        var result = await _controller.Login(loginDto);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.NotNull(unauthorizedResult.Value);
    }

    [Fact]
    public async Task Login_NonExistentUser_ReturnsUnauthorized()
    {
        // Arrange
        var loginDto = new LoginDto
        {
            Email = "nonexistent@example.com",
            Password = "Password123!"
        };

        var authResult = new AuthResult
        {
            Success = false,
            ErrorMessage = "E-mail ou senha incorretos.",
            ErrorCode = "INVALID_CREDENTIALS"
        };

        _authServiceMock
            .Setup(s => s.LoginAsync(loginDto))
            .ReturnsAsync(authResult);

        // Act
        var result = await _controller.Login(loginDto);

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task Register_CallsAuthServiceOnce()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            FirstName = "Test",
            LastName = "User",
            Email = "test@example.com",
            Password = "Password123!",
            ConfirmPassword = "Password123!"
        };

        var authResult = new AuthResult
        {
            Success = true,
            Data = new AuthResponseDto()
        };

        _authServiceMock
            .Setup(s => s.RegisterAsync(registerDto))
            .ReturnsAsync(authResult);

        // Act
        await _controller.Register(registerDto);

        // Assert
        _authServiceMock.Verify(s => s.RegisterAsync(registerDto), Times.Once);
    }

    [Fact]
    public async Task Login_CallsAuthServiceOnce()
    {
        // Arrange
        var loginDto = new LoginDto
        {
            Email = "test@example.com",
            Password = "Password123!"
        };

        var authResult = new AuthResult
        {
            Success = true,
            Data = new AuthResponseDto()
        };

        _authServiceMock
            .Setup(s => s.LoginAsync(loginDto))
            .ReturnsAsync(authResult);

        // Act
        await _controller.Login(loginDto);

        // Assert
        _authServiceMock.Verify(s => s.LoginAsync(loginDto), Times.Once);
    }
}
