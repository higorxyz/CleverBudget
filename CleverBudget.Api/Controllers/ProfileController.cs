using Asp.Versioning;
using CleverBudget.Api.Extensions;
using CleverBudget.Core.DTOs;
using CleverBudget.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CleverBudget.Api.Controllers;

[ApiVersion("2.0")]
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly IUserProfileService _userProfileService;
    private readonly IImageUploadService _imageUploadService;
    private readonly ILogger<ProfileController> _logger;

    public ProfileController(IUserProfileService userProfileService, IImageUploadService imageUploadService, ILogger<ProfileController> logger)
    {
        _userProfileService = userProfileService;
        _imageUploadService = imageUploadService;
        _logger = logger;
    }

    /// <summary>
    /// Obter perfil do usuário autenticado
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetProfile()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var profile = await _userProfileService.GetProfileAsync(userId);
        if (profile == null)
            return NotFound(new { message = "Perfil não encontrado" });

        var etag = EtagGenerator.Create(profile);

        if (this.RequestHasMatchingEtag(etag))
        {
            return this.CachedStatus();
        }

        this.SetEtagHeader(etag);
        return Ok(profile);
    }

    /// <summary>
    /// Atualizar perfil do usuário
    /// </summary>
    [HttpPut]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var success = await _userProfileService.UpdateProfileAsync(userId, dto);
        if (!success)
            return BadRequest(new { message = "Falha ao atualizar perfil" });

        return Ok(new { message = "Perfil atualizado com sucesso" });
    }

    /// <summary>
    /// Alterar senha do usuário
    /// </summary>
    [HttpPut("password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var result = await _userProfileService.ChangePasswordAsync(userId, dto);
        
        if (!result.Success)
            return BadRequest(new 
            { 
                message = result.ErrorMessage,
                errorCode = result.ErrorCode
            });

        return Ok(new { message = "Senha alterada com sucesso" });
    }

    /// <summary>
    /// Atualizar foto de perfil (URL) - DEPRECADO: Use UploadPhoto
    /// </summary>
    [HttpPut("photo")]
    public async Task<IActionResult> UpdatePhoto([FromBody] UpdatePhotoDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        if (string.IsNullOrWhiteSpace(dto.PhotoUrl))
            return BadRequest(new { message = "URL da foto é obrigatória" });

        var success = await _userProfileService.UpdatePhotoAsync(userId, dto.PhotoUrl);
        if (!success)
            return BadRequest(new { message = "Falha ao atualizar foto" });

        return Ok(new { message = "Foto atualizada com sucesso", photoUrl = dto.PhotoUrl });
    }

    /// <summary>
    /// Upload de foto de perfil (arquivo)
    /// </summary>
    [HttpPost("photo")]
    [RequestSizeLimit(5 * 1024 * 1024)] // 5 MB limit
    public async Task<IActionResult> UploadPhoto(IFormFile file)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        // Validar arquivo
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "Arquivo não fornecido" });

        // Validar tamanho (5 MB)
        if (file.Length > 5 * 1024 * 1024)
            return BadRequest(new { message = "Arquivo muito grande. Tamanho máximo: 5 MB" });

        // Validar tipo pelo Content-Type
        var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/webp" };
        if (!allowedTypes.Contains(file.ContentType.ToLower()))
            return BadRequest(new { message = "Tipo de arquivo inválido. Use: JPG, PNG ou WebP" });

        // Validar extensão do arquivo
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (string.IsNullOrEmpty(fileExtension) || !allowedExtensions.Contains(fileExtension))
            return BadRequest(new { message = "Extensão de arquivo inválida. Use: .jpg, .jpeg, .png ou .webp" });

        // Validar assinatura do arquivo (magic bytes) - Segurança adicional
        using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream);
        var fileBytes = memoryStream.ToArray();
        
        if (!IsValidImageFile(fileBytes))
            return BadRequest(new { message = "Arquivo não é uma imagem válida" });

        try
        {
            // Upload para Cloudinary
            memoryStream.Position = 0;
            var uploadResponse = await _imageUploadService.UploadImageAsync(memoryStream, $"user-{userId}", "profile-photos");

            if (!uploadResponse.Success)
                return BadRequest(new { message = uploadResponse.ErrorMessage });

            // Atualizar no banco
            var success = await _userProfileService.UpdatePhotoAsync(userId, uploadResponse.ImageUrl!);
            if (!success)
                return BadRequest(new { message = "Falha ao salvar URL da foto" });

            return Ok(new 
            { 
                message = "Foto enviada e atualizada com sucesso", 
                photoUrl = uploadResponse.ImageUrl 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar upload de foto para usuário {UserId}", userId);
            return StatusCode(500, new { message = "Erro ao processar upload. Tente novamente mais tarde." });
        }
    }

    private static bool IsValidImageFile(byte[] fileBytes)
    {
        if (fileBytes.Length < 4)
            return false;

        // JPEG: FF D8 FF
        if (fileBytes[0] == 0xFF && fileBytes[1] == 0xD8 && fileBytes[2] == 0xFF)
            return true;

        // PNG: 89 50 4E 47 0D 0A 1A 0A
        if (fileBytes.Length >= 8 &&
            fileBytes[0] == 0x89 && fileBytes[1] == 0x50 && 
            fileBytes[2] == 0x4E && fileBytes[3] == 0x47 &&
            fileBytes[4] == 0x0D && fileBytes[5] == 0x0A && 
            fileBytes[6] == 0x1A && fileBytes[7] == 0x0A)
            return true;

        // WebP: RIFF ???? WEBP
        if (fileBytes.Length >= 12 &&
            fileBytes[0] == 0x52 && fileBytes[1] == 0x49 && 
            fileBytes[2] == 0x46 && fileBytes[3] == 0x46 &&
            fileBytes[8] == 0x57 && fileBytes[9] == 0x45 && 
            fileBytes[10] == 0x42 && fileBytes[11] == 0x50)
            return true;

        return false;
    }
}

public class UpdatePhotoDto
{
    public string PhotoUrl { get; set; } = string.Empty;
}
