using CleverBudget.Core.DTOs;

namespace CleverBudget.Core.Interfaces;

public interface IAuthService
{
    Task<AuthResult> RegisterAsync(RegisterDto registerDto);
    Task<AuthResult> LoginAsync(LoginDto loginDto);
}