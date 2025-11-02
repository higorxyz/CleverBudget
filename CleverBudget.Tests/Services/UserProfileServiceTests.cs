using CleverBudget.Core.Entities;
using CleverBudget.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using CleverBudget.Core.DTOs;

namespace CleverBudget.Tests.Services;

public class UserProfileServiceTests : IDisposable
{
    private readonly Mock<UserManager<User>> _mockUserManager;
    private readonly Mock<ILogger<UserProfileService>> _mockLogger;
    private readonly UserProfileService _service;
    private const string TestUserId = "test-user-id";
    public UserProfileServiceTests()
    {
        var store = new Mock<IUserStore<User>>();
        _mockUserManager = new Mock<UserManager<User>>(
            store.Object, 
            new Mock<IOptions<IdentityOptions>>().Object,
            new Mock<IPasswordHasher<User>>().Object,
            new[] { new Mock<IUserValidator<User>>().Object },
            new[] { new Mock<IPasswordValidator<User>>().Object },
            new Mock<ILookupNormalizer>().Object,
            new Mock<IdentityErrorDescriber>().Object,
            new Mock<IServiceProvider>().Object,
            new Mock<ILogger<UserManager<User>>>().Object);
        _mockLogger = new Mock<ILogger<UserProfileService>>();
        _service = new UserProfileService(_mockUserManager.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetProfileAsync_WithValidUser_ReturnsProfile()
    {
        // Arrange
        var user = new User
        {
            Id = TestUserId,
            FirstName = "John",
            LastName = "Doe",
            Email = "john@example.com",
            PhotoUrl = "https://example.com/photo.jpg",
            CreatedAt = new DateTime(2024, 1, 1)
        };
        _mockUserManager.Setup(m => m.FindByIdAsync(TestUserId))
            .ReturnsAsync(user);

        // Act
        var result = await _service.GetProfileAsync(TestUserId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TestUserId, result.Id);
        Assert.Equal("John", result.FirstName);
        Assert.Equal("Doe", result.LastName);
        Assert.Equal("john@example.com", result.Email);
        Assert.Equal("https://example.com/photo.jpg", result.PhotoUrl);
        Assert.Equal(new DateTime(2024, 1, 1), result.CreatedAt);
    }

    [Fact]
    public async Task GetProfileAsync_WithNonExistentUser_ReturnsNull()
    {
        // Arrange
        _mockUserManager.Setup(m => m.FindByIdAsync(TestUserId))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _service.GetProfileAsync(TestUserId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateProfileAsync_WithValidData_ReturnsTrue()
    {
        // Arrange
        var user = new User
        {
            Id = TestUserId,
            FirstName = "John",
            LastName = "Doe",
            Email = "john@example.com"
        };
        var dto = new UpdateProfileDto
        {
            FirstName = "Jane",
            LastName = "Smith"
        };
        _mockUserManager.Setup(m => m.FindByIdAsync(TestUserId))
            .ReturnsAsync(user);
        _mockUserManager.Setup(m => m.UpdateAsync(It.IsAny<User>()))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _service.UpdateProfileAsync(TestUserId, dto);

        // Assert
        Assert.True(result);
        Assert.Equal("Jane", user.FirstName);
        Assert.Equal("Smith", user.LastName);
    }

    [Fact]
    public async Task UpdateProfileAsync_WithNonExistentUser_ReturnsFalse()
    {
        // Arrange
        var dto = new UpdateProfileDto
        {
            FirstName = "Jane",
            LastName = "Smith"
        };
        _mockUserManager.Setup(m => m.FindByIdAsync(TestUserId))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _service.UpdateProfileAsync(TestUserId, dto);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task UpdateProfileAsync_WhenUpdateFails_ReturnsFalse()
    {
        // Arrange
        var user = new User
        {
            Id = TestUserId,
            FirstName = "John",
            LastName = "Doe"
        };
        var dto = new UpdateProfileDto
        {
            FirstName = "Jane",
            LastName = "Smith"
        };
        _mockUserManager.Setup(m => m.FindByIdAsync(TestUserId))
            .ReturnsAsync(user);
        _mockUserManager.Setup(m => m.UpdateAsync(It.IsAny<User>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Update failed" }));

        // Act
        var result = await _service.UpdateProfileAsync(TestUserId, dto);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ChangePasswordAsync_WithValidData_ReturnsTrue()
    {
        // Arrange
        var user = new User { Id = TestUserId };
        var dto = new ChangePasswordDto
        {
            CurrentPassword = "OldPass123",
            NewPassword = "NewPass123",
            ConfirmPassword = "NewPass123"
        };
        _mockUserManager.Setup(m => m.FindByIdAsync(TestUserId))
            .ReturnsAsync(user);
        _mockUserManager.Setup(m => m.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _service.ChangePasswordAsync(TestUserId, dto);

        // Assert
        Assert.True(result.Success);
    }

    [Fact]
    public async Task ChangePasswordAsync_WithMismatchedPasswords_ReturnsFalse()
    {
        // Arrange
        var user = new User { Id = TestUserId };
        var dto = new ChangePasswordDto
        {
            CurrentPassword = "OldPass123",
            NewPassword = "NewPass123",
            ConfirmPassword = "DifferentPass123"
        };
        _mockUserManager.Setup(m => m.FindByIdAsync(TestUserId))
            .ReturnsAsync(user);

        // Act
        var result = await _service.ChangePasswordAsync(TestUserId, dto);

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
        Assert.Equal("PASSWORD_MISMATCH", result.ErrorCode);
    }

    [Fact]
    public async Task ChangePasswordAsync_WithWrongCurrentPassword_ReturnsFalse()
    {
        // Arrange
        var user = new User { Id = TestUserId };
        var dto = new ChangePasswordDto
        {
            CurrentPassword = "WrongPass",
            NewPassword = "NewPass123",
            ConfirmPassword = "NewPass123"
        };
        _mockUserManager.Setup(m => m.FindByIdAsync(TestUserId))
            .ReturnsAsync(user);
        _mockUserManager.Setup(m => m.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Code = "PasswordMismatch", Description = "Incorrect password" }));

        // Act
        var result = await _service.ChangePasswordAsync(TestUserId, dto);

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public async Task ChangePasswordAsync_WithNonExistentUser_ReturnsFalse()
    {
        // Arrange
        var dto = new ChangePasswordDto
        {
            CurrentPassword = "OldPass123",
            NewPassword = "NewPass123",
            ConfirmPassword = "NewPass123"
        };
        _mockUserManager.Setup(m => m.FindByIdAsync(TestUserId))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _service.ChangePasswordAsync(TestUserId, dto);

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
        Assert.Equal("USER_NOT_FOUND", result.ErrorCode);
    }

    [Fact]
    public async Task UpdatePhotoAsync_WithValidUrl_ReturnsTrue()
    {
        // Arrange
        var user = new User
        {
            Id = TestUserId,
            PhotoUrl = null
        };
        var photoUrl = "https://example.com/new-photo.jpg";
        _mockUserManager.Setup(m => m.FindByIdAsync(TestUserId))
            .ReturnsAsync(user);
        _mockUserManager.Setup(m => m.UpdateAsync(It.IsAny<User>()))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _service.UpdatePhotoAsync(TestUserId, photoUrl);

        // Assert
        Assert.True(result);
        Assert.Equal(photoUrl, user.PhotoUrl);
    }

    [Fact]
    public async Task UpdatePhotoAsync_WithNonExistentUser_ReturnsFalse()
    {
        // Arrange
        var photoUrl = "https://example.com/photo.jpg";
        _mockUserManager.Setup(m => m.FindByIdAsync(TestUserId))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _service.UpdatePhotoAsync(TestUserId, photoUrl);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task UpdatePhotoAsync_WhenUpdateFails_ReturnsFalse()
    {
        // Arrange
        var user = new User { Id = TestUserId };
        var photoUrl = "https://example.com/photo.jpg";
        _mockUserManager.Setup(m => m.FindByIdAsync(TestUserId))
            .ReturnsAsync(user);
        _mockUserManager.Setup(m => m.UpdateAsync(It.IsAny<User>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Update failed" }));

        // Act
        var result = await _service.UpdatePhotoAsync(TestUserId, photoUrl);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetProfileAsync_WithNullEmail_ReturnsEmptyEmail()
    {
        // Arrange
        var user = new User
        {
            Id = TestUserId,
            FirstName = "John",
            LastName = "Doe",
            Email = null,
            CreatedAt = DateTime.UtcNow
        };
        _mockUserManager.Setup(m => m.FindByIdAsync(TestUserId))
            .ReturnsAsync(user);

        // Act
        var result = await _service.GetProfileAsync(TestUserId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(string.Empty, result.Email);
    }

    public void Dispose()
    {
        _mockUserManager.Object.Dispose();
    }
}
