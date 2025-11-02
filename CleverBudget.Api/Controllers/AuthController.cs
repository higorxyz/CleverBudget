using CleverBudget.Core.DTOs;
using CleverBudget.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CleverBudget.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Registrar novo usuário
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
    {
        var result = await _authService.RegisterAsync(registerDto);

        if (!result.Success)
            return BadRequest(new 
            { 
                message = result.ErrorMessage,
                errorCode = result.ErrorCode
            });

        return Ok(result.Data);
    }

    /// <summary>
    /// Login de usuário
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
    {
        var result = await _authService.LoginAsync(loginDto);

        if (!result.Success)
            return Unauthorized(new 
            { 
                message = result.ErrorMessage,
                errorCode = result.ErrorCode
            });

        return Ok(result.Data);
    }
}