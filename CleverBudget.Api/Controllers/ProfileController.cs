using CleverBudget.Core.DTOs;
using CleverBudget.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CleverBudget.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly IUserProfileService _userProfileService;

    public ProfileController(IUserProfileService userProfileService)
    {
        _userProfileService = userProfileService;
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

        var success = await _userProfileService.ChangePasswordAsync(userId, dto);
        if (!success)
            return BadRequest(new { message = "Falha ao alterar senha. Verifique se a senha atual está correta." });

        return Ok(new { message = "Senha alterada com sucesso" });
    }

    /// <summary>
    /// Atualizar foto de perfil (URL)
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
}

public class UpdatePhotoDto
{
    public string PhotoUrl { get; set; } = string.Empty;
}
