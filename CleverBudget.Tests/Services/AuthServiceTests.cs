using CleverBudget.Core.DTOs;
using CleverBudget.Core.Entities;
using CleverBudget.Core.Interfaces;
using CleverBudget.Infrastructure.Data;
using CleverBudget.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CleverBudget.Tests.Services;

public class AuthServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly Mock<UserManager<User>> _userManagerMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly Mock<ILogger<AuthService>> _loggerMock;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);

        var userStoreMock = new Mock<IUserStore<User>>();
        _userManagerMock = new Mock<UserManager<User>>(
            userStoreMock.Object, 
            null!, // IOptions<IdentityOptions>
            null!, // IPasswordHasher<User>
            null!, // IEnumerable<IUserValidator<User>>
            null!, // IEnumerable<IPasswordValidator<User>>
            null!, // ILookupNormalizer
            null!, // IdentityErrorDescriber
            null!, // IServiceProvider
            null!  // ILogger<UserManager<User>>
        );

        // Mock Configuration (JWT Settings)
        _configurationMock = new Mock<IConfiguration>();
        var jwtSection = new Mock<IConfigurationSection>();
        jwtSection.Setup(x => x["SecretKey"]).Returns("TestSecretKeyMinimum32CharactersLong!!");
        jwtSection.Setup(x => x["Issuer"]).Returns("TestIssuer");
        jwtSection.Setup(x => x["Audience"]).Returns("TestAudience");
        jwtSection.Setup(x => x["ExpirationMinutes"]).Returns("60");
        _configurationMock.Setup(x => x.GetSection("JwtSettings")).Returns(jwtSection.Object);

        // Mock Email Service
        _emailServiceMock = new Mock<IEmailService>();
        _emailServiceMock
            .Setup(x => x.SendWelcomeEmailAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        // Mock Logger
        _loggerMock = new Mock<ILogger<AuthService>>();

        _authService = new AuthService(
            _userManagerMock.Object,
            _configurationMock.Object,
            _context,
            _emailServiceMock.Object,
            _loggerMock.Object
        );
    }

    [Fact]
    public async Task RegisterAsync_ValidData_ReturnsAuthResponse()
    {
        var registerDto = new RegisterDto
        {
            FirstName = "João",
            LastName = "Silva",
            Email = "joao@example.com",
            Password = "Senha123",
            ConfirmPassword = "Senha123"
        };

        _userManagerMock
            .Setup(x => x.FindByEmailAsync(registerDto.Email))
            .ReturnsAsync((User?)null);

        _userManagerMock
            .Setup(x => x.CreateAsync(It.IsAny<User>(), registerDto.Password))
            .ReturnsAsync(IdentityResult.Success);

        var result = await _authService.RegisterAsync(registerDto);

        Assert.NotNull(result);
        Assert.Equal(registerDto.Email, result.Email);
        Assert.Equal(registerDto.FirstName, result.FirstName);
        Assert.Equal(registerDto.LastName, result.LastName);
        Assert.NotEmpty(result.Token);
        Assert.True(result.ExpiresAt > DateTime.UtcNow);

        // Verify default categories were created
        var categories = await _context.Categories.ToListAsync();
        Assert.Equal(9, categories.Count);
        Assert.Contains(categories, c => c.Name == "Alimentação");
        Assert.Contains(categories, c => c.Name == "Transporte");
        Assert.Contains(categories, c => c.Name == "Salário");
    }

    [Fact]
    public async Task RegisterAsync_PasswordMismatch_ReturnsNull()
    {
        var registerDto = new RegisterDto
        {
            FirstName = "Maria",
            LastName = "Santos",
            Email = "maria@example.com",
            Password = "Senha123",
            ConfirmPassword = "SenhaDiferente123"
        };

        var result = await _authService.RegisterAsync(registerDto);

        Assert.Null(result);
        _userManagerMock.Verify(x => x.CreateAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_EmailAlreadyExists_ReturnsNull()
    {
        var registerDto = new RegisterDto
        {
            FirstName = "Pedro",
            LastName = "Oliveira",
            Email = "pedro@example.com",
            Password = "Senha123",
            ConfirmPassword = "Senha123"
        };

        var existingUser = new User { Email = registerDto.Email };
        _userManagerMock
            .Setup(x => x.FindByEmailAsync(registerDto.Email))
            .ReturnsAsync(existingUser);

        var result = await _authService.RegisterAsync(registerDto);

        Assert.Null(result);
        _userManagerMock.Verify(x => x.CreateAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_CreateUserFails_ReturnsNull()
    {
        var registerDto = new RegisterDto
        {
            FirstName = "Ana",
            LastName = "Costa",
            Email = "ana@example.com",
            Password = "Senha123",
            ConfirmPassword = "Senha123"
        };

        _userManagerMock
            .Setup(x => x.FindByEmailAsync(registerDto.Email))
            .ReturnsAsync((User?)null);

        _userManagerMock
            .Setup(x => x.CreateAsync(It.IsAny<User>(), registerDto.Password))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Password too weak" }));

        var result = await _authService.RegisterAsync(registerDto);

        Assert.Null(result);
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsAuthResponse()
    {
        var loginDto = new LoginDto
        {
            Email = "usuario@example.com",
            Password = "Senha123"
        };

        var user = new User
        {
            Id = Guid.NewGuid().ToString(),
            Email = loginDto.Email,
            UserName = loginDto.Email,
            FirstName = "Usuário",
            LastName = "Teste"
        };

        _userManagerMock
            .Setup(x => x.FindByEmailAsync(loginDto.Email))
            .ReturnsAsync(user);

        _userManagerMock
            .Setup(x => x.CheckPasswordAsync(user, loginDto.Password))
            .ReturnsAsync(true);

        var result = await _authService.LoginAsync(loginDto);

        Assert.NotNull(result);
        Assert.Equal(loginDto.Email, result.Email);
        Assert.NotEmpty(result.Token);
    }

    [Fact]
    public async Task LoginAsync_UserNotFound_ReturnsNull()
    {
        var loginDto = new LoginDto
        {
            Email = "naoexiste@example.com",
            Password = "Senha123"
        };

        _userManagerMock
            .Setup(x => x.FindByEmailAsync(loginDto.Email))
            .ReturnsAsync((User?)null);

        var result = await _authService.LoginAsync(loginDto);

        Assert.Null(result);
        _userManagerMock.Verify(x => x.CheckPasswordAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_InvalidPassword_ReturnsNull()
    {
        var loginDto = new LoginDto
        {
            Email = "usuario@example.com",
            Password = "SenhaErrada"
        };

        var user = new User
        {
            Id = Guid.NewGuid().ToString(),
            Email = loginDto.Email,
            UserName = loginDto.Email,
            FirstName = "Usuário",
            LastName = "Teste"
        };

        _userManagerMock
            .Setup(x => x.FindByEmailAsync(loginDto.Email))
            .ReturnsAsync(user);

        _userManagerMock
            .Setup(x => x.CheckPasswordAsync(user, loginDto.Password))
            .ReturnsAsync(false);

        var result = await _authService.LoginAsync(loginDto);

        Assert.Null(result);
    }

    [Fact]
    public async Task RegisterAsync_SendsWelcomeEmail()
    {
        var registerDto = new RegisterDto
        {
            FirstName = "Carlos",
            LastName = "Ferreira",
            Email = "carlos@example.com",
            Password = "Senha123",
            ConfirmPassword = "Senha123"
        };

        _userManagerMock
            .Setup(x => x.FindByEmailAsync(registerDto.Email))
            .ReturnsAsync((User?)null);

        _userManagerMock
            .Setup(x => x.CreateAsync(It.IsAny<User>(), registerDto.Password))
            .ReturnsAsync(IdentityResult.Success);

        var result = await _authService.RegisterAsync(registerDto);

        // Wait a bit for async email task
        await Task.Delay(100);

        Assert.NotNull(result);
        _emailServiceMock.Verify(
            x => x.SendWelcomeEmailAsync(
                registerDto.Email,
                It.Is<string>(name => name.Contains(registerDto.FirstName))
            ),
            Times.Once
        );
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
