using System.Security.Claims;
using System.Text.Json;
using CleverBudget.Api.Controllers;
using CleverBudget.Core.DTOs;
using CleverBudget.Core.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CleverBudget.Tests.Controllers;

public class ProfileControllerTests
{
    private readonly Mock<IUserProfileService> _mockService;
    private readonly Mock<IImageUploadService> _mockImageService;
    private readonly Mock<ILogger<ProfileController>> _mockLogger;
    private readonly ProfileController _controller;
    private const string TestUserId = "test-user-id";

    public ProfileControllerTests()
    {
        _mockService = new Mock<IUserProfileService>();
        _mockImageService = new Mock<IImageUploadService>();
        _mockLogger = new Mock<ILogger<ProfileController>>();
        _controller = new ProfileController(_mockService.Object, _mockImageService.Object, _mockLogger.Object);
        
        // Setup user claims
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, TestUserId)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };
    }

    private static string? GetMessageFromResult(BadRequestObjectResult result)
    {
        if (result.Value == null) return null;
        
        var json = JsonSerializer.Serialize(result.Value);
        var doc = JsonDocument.Parse(json);
        
        if (doc.RootElement.TryGetProperty("message", out var messageElement))
        {
            return messageElement.GetString();
        }
        
        return null;
    }

    private static MemoryStream CreateFakeJpegStream()
    {
        var ms = new MemoryStream();
        // JPEG magic bytes: FF D8 FF E0
        ms.Write(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46 });
        // Add some dummy content
        ms.Write(System.Text.Encoding.UTF8.GetBytes("fake jpeg content"));
        ms.Position = 0;
        return ms;
    }

    private static MemoryStream CreateFakePngStream()
    {
        var ms = new MemoryStream();
        // PNG magic bytes: 89 50 4E 47 0D 0A 1A 0A
        ms.Write(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });
        // Add some dummy content
        ms.Write(System.Text.Encoding.UTF8.GetBytes("fake png content"));
        ms.Position = 0;
        return ms;
    }

    private static MemoryStream CreateFakeWebPStream()
    {
        var ms = new MemoryStream();
        // WebP magic bytes: RIFF ???? WEBP
        ms.Write(new byte[] { 0x52, 0x49, 0x46, 0x46, 0x00, 0x00, 0x00, 0x00, 0x57, 0x45, 0x42, 0x50 });
        // Add some dummy content
        ms.Write(System.Text.Encoding.UTF8.GetBytes("fake webp content"));
        ms.Position = 0;
        return ms;
    }

    [Fact]
    public async Task GetProfile_WithValidUser_ReturnsOk()
    {
        // Arrange
        var profile = new UserProfileDto
        {
            Id = TestUserId,
            FirstName = "John",
            LastName = "Doe",
            Email = "john@example.com",
            CreatedAt = DateTime.UtcNow
        };
        _mockService.Setup(s => s.GetProfileAsync(TestUserId))
            .ReturnsAsync(profile);

        // Act
        var result = await _controller.GetProfile();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedProfile = Assert.IsType<UserProfileDto>(okResult.Value);
        Assert.Equal("John", returnedProfile.FirstName);
        Assert.Equal("Doe", returnedProfile.LastName);
    }

    [Fact]
    public async Task GetProfile_WhenNotFound_ReturnsNotFound()
    {
        // Arrange
        _mockService.Setup(s => s.GetProfileAsync(TestUserId))
            .ReturnsAsync((UserProfileDto?)null);

        // Act
        var result = await _controller.GetProfile();

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task UpdateProfile_WithValidData_ReturnsOk()
    {
        // Arrange
        var dto = new UpdateProfileDto
        {
            FirstName = "Jane",
            LastName = "Smith"
        };
        _mockService.Setup(s => s.UpdateProfileAsync(TestUserId, dto))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.UpdateProfile(dto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task UpdateProfile_WhenFails_ReturnsBadRequest()
    {
        // Arrange
        var dto = new UpdateProfileDto
        {
            FirstName = "Jane",
            LastName = "Smith"
        };
        _mockService.Setup(s => s.UpdateProfileAsync(TestUserId, dto))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.UpdateProfile(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task ChangePassword_WithValidData_ReturnsOk()
    {
        // Arrange
        var dto = new ChangePasswordDto
        {
            CurrentPassword = "OldPass123",
            NewPassword = "NewPass123",
            ConfirmPassword = "NewPass123"
        };
        _mockService.Setup(s => s.ChangePasswordAsync(TestUserId, dto))
            .ReturnsAsync(OperationResult.SuccessResult());

        // Act
        var result = await _controller.ChangePassword(dto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task ChangePassword_WhenFails_ReturnsBadRequest()
    {
        // Arrange
        var dto = new ChangePasswordDto
        {
            CurrentPassword = "WrongPass",
            NewPassword = "NewPass123",
            ConfirmPassword = "NewPass123"
        };
        _mockService.Setup(s => s.ChangePasswordAsync(TestUserId, dto))
            .ReturnsAsync(OperationResult.FailureResult("A senha atual está incorreta.", "PasswordMismatch"));

        // Act
        var result = await _controller.ChangePassword(dto);

        // Assert
        var badResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badResult.Value);
    }

    [Fact]
    public async Task UpdatePhoto_WithValidUrl_ReturnsOk()
    {
        // Arrange
        var dto = new UpdatePhotoDto
        {
            PhotoUrl = "https://example.com/photo.jpg"
        };
        _mockService.Setup(s => s.UpdatePhotoAsync(TestUserId, dto.PhotoUrl))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.UpdatePhoto(dto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task UpdatePhoto_WithEmptyUrl_ReturnsBadRequest()
    {
        // Arrange
        var dto = new UpdatePhotoDto
        {
            PhotoUrl = ""
        };

        // Act
        var result = await _controller.UpdatePhoto(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task UpdatePhoto_WithWhitespaceUrl_ReturnsBadRequest()
    {
        // Arrange
        var dto = new UpdatePhotoDto
        {
            PhotoUrl = "   "
        };

        // Act
        var result = await _controller.UpdatePhoto(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task UpdatePhoto_WhenServiceFails_ReturnsBadRequest()
    {
        // Arrange
        var dto = new UpdatePhotoDto
        {
            PhotoUrl = "https://example.com/photo.jpg"
        };
        _mockService.Setup(s => s.UpdatePhotoAsync(TestUserId, dto.PhotoUrl))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.UpdatePhoto(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task GetProfile_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var controller = new ProfileController(_mockService.Object, _mockImageService.Object, _mockLogger.Object);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        // Act
        var result = await controller.GetProfile();

        // Assert
        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task UpdateProfile_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var controller = new ProfileController(_mockService.Object, _mockImageService.Object, _mockLogger.Object);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        var dto = new UpdateProfileDto
        {
            FirstName = "Test",
            LastName = "User"
        };

        // Act
        var result = await controller.UpdateProfile(dto);

        // Assert
        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task ChangePassword_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var controller = new ProfileController(_mockService.Object, _mockImageService.Object, _mockLogger.Object);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        var dto = new ChangePasswordDto
        {
            CurrentPassword = "Old123",
            NewPassword = "New123",
            ConfirmPassword = "New123"
        };

        // Act
        var result = await controller.ChangePassword(dto);

        // Assert
        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task UpdatePhoto_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var controller = new ProfileController(_mockService.Object, _mockImageService.Object, _mockLogger.Object);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        var dto = new UpdatePhotoDto
        {
            PhotoUrl = "https://example.com/photo.jpg"
        };

        // Act
        var result = await controller.UpdatePhoto(dto);

        // Assert
        Assert.IsType<UnauthorizedResult>(result);
    }

    // Testes do novo endpoint de upload de foto com Cloudinary

    [Fact]
    public async Task UploadPhoto_WithValidFile_ReturnsOk()
    {
        // Arrange
        var fileMock = new Mock<IFormFile>();
        var fileName = "test.jpg";
        var ms = CreateFakeJpegStream();

        fileMock.Setup(_ => _.OpenReadStream()).Returns(ms);
        fileMock.Setup(_ => _.FileName).Returns(fileName);
        fileMock.Setup(_ => _.Length).Returns(ms.Length);
        fileMock.Setup(_ => _.ContentType).Returns("image/jpeg");
        fileMock.Setup(_ => _.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .Returns((Stream target, CancellationToken token) =>
            {
                ms.Position = 0;
                return ms.CopyToAsync(target, token);
            });

        var cloudinaryUrl = "https://res.cloudinary.com/test/image/upload/v123456/photo.jpg";
        _mockImageService.Setup(s => s.UploadImageAsync(
            It.IsAny<Stream>(),
            It.IsAny<string>(),
            It.IsAny<string>()))
            .ReturnsAsync(new ImageUploadResponse 
            { 
                Success = true, 
                ImageUrl = cloudinaryUrl 
            });

        _mockService.Setup(s => s.UpdatePhotoAsync(TestUserId, cloudinaryUrl))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.UploadPhoto(fileMock.Object);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        
        // Verify service calls
        _mockImageService.Verify(s => s.UploadImageAsync(
            It.IsAny<Stream>(),
            It.IsAny<string>(),
            It.IsAny<string>()), Times.Once);
        _mockService.Verify(s => s.UpdatePhotoAsync(TestUserId, cloudinaryUrl), Times.Once);
    }

    [Fact]
    public async Task UploadPhoto_WithNullFile_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.UploadPhoto(null!);

        // Assert
        var badResult = Assert.IsType<BadRequestObjectResult>(result);
        var message = GetMessageFromResult(badResult);
        Assert.Equal("Arquivo não fornecido", message);
    }

    [Fact]
    public async Task UploadPhoto_WithEmptyFile_ReturnsBadRequest()
    {
        // Arrange
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(_ => _.Length).Returns(0);
        fileMock.Setup(_ => _.FileName).Returns("test.jpg");

        // Act
        var result = await _controller.UploadPhoto(fileMock.Object);

        // Assert
        var badResult = Assert.IsType<BadRequestObjectResult>(result);
        var message = GetMessageFromResult(badResult);
        Assert.Equal("Arquivo não fornecido", message);
    }

    [Fact]
    public async Task UploadPhoto_WithFileTooLarge_ReturnsBadRequest()
    {
        // Arrange
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(_ => _.Length).Returns(6 * 1024 * 1024); // 6MB
        fileMock.Setup(_ => _.FileName).Returns("test.jpg");
        fileMock.Setup(_ => _.ContentType).Returns("image/jpeg");

        // Act
        var result = await _controller.UploadPhoto(fileMock.Object);

        // Assert
        var badResult = Assert.IsType<BadRequestObjectResult>(result);
        var message = GetMessageFromResult(badResult);
        Assert.Equal("Arquivo muito grande. Tamanho máximo: 5 MB", message);
    }

    [Fact]
    public async Task UploadPhoto_WithInvalidFileType_ReturnsBadRequest()
    {
        // Arrange
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(_ => _.Length).Returns(1024);
        fileMock.Setup(_ => _.FileName).Returns("test.pdf");
        fileMock.Setup(_ => _.ContentType).Returns("application/pdf");

        // Act
        var result = await _controller.UploadPhoto(fileMock.Object);

        // Assert
        var badResult = Assert.IsType<BadRequestObjectResult>(result);
        var message = GetMessageFromResult(badResult);
        Assert.Equal("Tipo de arquivo inválido. Use: JPG, PNG ou WebP", message);
    }

    [Fact]
    public async Task UploadPhoto_WhenCloudinaryFails_ReturnsBadRequest()
    {
        // Arrange
        var fileMock = new Mock<IFormFile>();
        var fileName = "test.jpg";
        var ms = CreateFakeJpegStream();

        fileMock.Setup(_ => _.FileName).Returns(fileName);
        fileMock.Setup(_ => _.Length).Returns(ms.Length);
        fileMock.Setup(_ => _.ContentType).Returns("image/jpeg");
        fileMock.Setup(_ => _.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .Returns((Stream target, CancellationToken token) =>
            {
                ms.Position = 0;
                return ms.CopyToAsync(target, token);
            });

        _mockImageService.Setup(s => s.UploadImageAsync(
            It.IsAny<Stream>(),
            It.IsAny<string>(),
            It.IsAny<string>()))
            .ReturnsAsync(new ImageUploadResponse 
            { 
                Success = false, 
                ErrorMessage = "Falha ao fazer upload da imagem" 
            });

        // Act
        var result = await _controller.UploadPhoto(fileMock.Object);

        // Assert
        var badResult = Assert.IsType<BadRequestObjectResult>(result);
        var message = GetMessageFromResult(badResult);
        Assert.Equal("Falha ao fazer upload da imagem", message);
    }

    [Fact]
    public async Task UploadPhoto_WhenUpdatePhotoFails_ReturnsBadRequest()
    {
        // Arrange
        var fileMock = new Mock<IFormFile>();
        var fileName = "test.jpg";
        var ms = CreateFakeJpegStream();

        fileMock.Setup(_ => _.FileName).Returns(fileName);
        fileMock.Setup(_ => _.Length).Returns(ms.Length);
        fileMock.Setup(_ => _.ContentType).Returns("image/jpeg");
        fileMock.Setup(_ => _.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .Returns((Stream target, CancellationToken token) =>
            {
                ms.Position = 0;
                return ms.CopyToAsync(target, token);
            });

        var cloudinaryUrl = "https://res.cloudinary.com/test/image/upload/v123456/photo.jpg";
        _mockImageService.Setup(s => s.UploadImageAsync(
            It.IsAny<Stream>(),
            It.IsAny<string>(),
            It.IsAny<string>()))
            .ReturnsAsync(new ImageUploadResponse 
            { 
                Success = true, 
                ImageUrl = cloudinaryUrl 
            });

        _mockService.Setup(s => s.UpdatePhotoAsync(TestUserId, cloudinaryUrl))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.UploadPhoto(fileMock.Object);

        // Assert
        var badResult = Assert.IsType<BadRequestObjectResult>(result);
        var message = GetMessageFromResult(badResult);
        Assert.Equal("Falha ao salvar URL da foto", message);
    }

    [Fact]
    public async Task UploadPhoto_WithPngFile_ReturnsOk()
    {
        // Arrange
        var fileMock = new Mock<IFormFile>();
        var fileName = "test.png";
        var ms = CreateFakePngStream();

        fileMock.Setup(_ => _.FileName).Returns(fileName);
        fileMock.Setup(_ => _.Length).Returns(ms.Length);
        fileMock.Setup(_ => _.ContentType).Returns("image/png");
        fileMock.Setup(_ => _.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .Returns((Stream target, CancellationToken token) =>
            {
                ms.Position = 0;
                return ms.CopyToAsync(target, token);
            });

        var cloudinaryUrl = "https://res.cloudinary.com/test/image/upload/v123456/photo.png";
        _mockImageService.Setup(s => s.UploadImageAsync(
            It.IsAny<Stream>(),
            It.IsAny<string>(),
            It.IsAny<string>()))
            .ReturnsAsync(new ImageUploadResponse 
            { 
                Success = true, 
                ImageUrl = cloudinaryUrl 
            });

        _mockService.Setup(s => s.UpdatePhotoAsync(TestUserId, cloudinaryUrl))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.UploadPhoto(fileMock.Object);

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task UploadPhoto_WithWebPFile_ReturnsOk()
    {
        // Arrange
        var fileMock = new Mock<IFormFile>();
        var fileName = "test.webp";
        var ms = CreateFakeWebPStream();

        fileMock.Setup(_ => _.FileName).Returns(fileName);
        fileMock.Setup(_ => _.Length).Returns(ms.Length);
        fileMock.Setup(_ => _.ContentType).Returns("image/webp");
        fileMock.Setup(_ => _.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .Returns((Stream target, CancellationToken token) =>
            {
                ms.Position = 0;
                return ms.CopyToAsync(target, token);
            });

        var cloudinaryUrl = "https://res.cloudinary.com/test/image/upload/v123456/photo.webp";
        _mockImageService.Setup(s => s.UploadImageAsync(
            It.IsAny<Stream>(),
            It.IsAny<string>(),
            It.IsAny<string>()))
            .ReturnsAsync(new ImageUploadResponse 
            { 
                Success = true, 
                ImageUrl = cloudinaryUrl 
            });

        _mockService.Setup(s => s.UpdatePhotoAsync(TestUserId, cloudinaryUrl))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.UploadPhoto(fileMock.Object);

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task UploadPhoto_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var controller = new ProfileController(_mockService.Object, _mockImageService.Object, _mockLogger.Object);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(_ => _.Length).Returns(1024);
        fileMock.Setup(_ => _.FileName).Returns("test.jpg");
        fileMock.Setup(_ => _.ContentType).Returns("image/jpeg");

        // Act
        var result = await controller.UploadPhoto(fileMock.Object);

        // Assert
        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task UploadPhoto_WhenModerationRejectsImage_ReturnsBadRequestWithCustomMessage()
    {
        // Arrange
        var fileMock = new Mock<IFormFile>();
        var fileName = "inappropriate.jpg";
        var ms = CreateFakeJpegStream();

        fileMock.Setup(_ => _.FileName).Returns(fileName);
        fileMock.Setup(_ => _.Length).Returns(ms.Length);
        fileMock.Setup(_ => _.ContentType).Returns("image/jpeg");
        fileMock.Setup(_ => _.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .Returns((Stream target, CancellationToken token) =>
            {
                ms.Position = 0;
                return ms.CopyToAsync(target, token);
            });

        // Simular rejeição por moderação
        _mockImageService.Setup(s => s.UploadImageAsync(
            It.IsAny<Stream>(),
            It.IsAny<string>(),
            It.IsAny<string>()))
            .ReturnsAsync(new ImageUploadResponse 
            { 
                Success = false, 
                ErrorMessage = "Imagem rejeitada: conteúdo impróprio detectado. Por favor, escolha outra imagem." 
            });

        // Act
        var result = await _controller.UploadPhoto(fileMock.Object);

        // Assert
        var badResult = Assert.IsType<BadRequestObjectResult>(result);
        var message = GetMessageFromResult(badResult);
        Assert.Equal("Imagem rejeitada: conteúdo impróprio detectado. Por favor, escolha outra imagem.", message);
        
        // Verificar que NÃO tentou salvar no banco
        _mockService.Verify(s => s.UpdatePhotoAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task UploadPhoto_WhenModerationApproves_SavesSuccessfully()
    {
        // Arrange
        var fileMock = new Mock<IFormFile>();
        var fileName = "safe.jpg";
        var ms = CreateFakeJpegStream();

        fileMock.Setup(_ => _.FileName).Returns(fileName);
        fileMock.Setup(_ => _.Length).Returns(ms.Length);
        fileMock.Setup(_ => _.ContentType).Returns("image/jpeg");
        fileMock.Setup(_ => _.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .Returns((Stream target, CancellationToken token) =>
            {
                ms.Position = 0;
                return ms.CopyToAsync(target, token);
            });

        var cloudinaryUrl = "https://res.cloudinary.com/test/image/upload/v123456/safe.jpg";
        
        // Simular aprovação por moderação
        _mockImageService.Setup(s => s.UploadImageAsync(
            It.IsAny<Stream>(),
            It.IsAny<string>(),
            It.IsAny<string>()))
            .ReturnsAsync(new ImageUploadResponse 
            { 
                Success = true, 
                ImageUrl = cloudinaryUrl 
            });

        _mockService.Setup(s => s.UpdatePhotoAsync(TestUserId, cloudinaryUrl))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.UploadPhoto(fileMock.Object);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        
        // Verificar que salvou no banco
        _mockService.Verify(s => s.UpdatePhotoAsync(TestUserId, cloudinaryUrl), Times.Once);
    }
}
