using CleverBudget.Core.DTOs;
using CleverBudget.Core.Entities;
using CleverBudget.Core.Interfaces;
using CleverBudget.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace CleverBudget.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<User> _userManager;
    private readonly IConfiguration _configuration;
    private readonly AppDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        UserManager<User> userManager, 
        IConfiguration configuration, 
        AppDbContext context,
        IEmailService emailService,
        ILogger<AuthService> logger)
    {
        _userManager = userManager;
        _configuration = configuration;
        _context = context;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<AuthResult> RegisterAsync(RegisterDto registerDto)
    {
        // Validar se as senhas conferem
        if (registerDto.Password != registerDto.ConfirmPassword)
            return new AuthResult 
            { 
                Success = false, 
                ErrorMessage = "As senhas não conferem. Por favor, digite a mesma senha nos dois campos.",
                ErrorCode = "PASSWORD_MISMATCH"
            };

        // Verificar se já existe usuário com este email
        var existingUser = await _userManager.FindByEmailAsync(registerDto.Email);
        if (existingUser != null)
            return new AuthResult 
            { 
                Success = false, 
                ErrorMessage = "Já existe uma conta com esse e-mail. Tente fazer login ou use outro e-mail.",
                ErrorCode = "EMAIL_ALREADY_EXISTS"
            };

        var user = new User
        {
            UserName = registerDto.Email,
            Email = registerDto.Email,
            FirstName = registerDto.FirstName,
            LastName = registerDto.LastName,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, registerDto.Password);

        if (!result.Succeeded)
        {
            // Pegar o primeiro erro do Identity
            var error = result.Errors.FirstOrDefault();
            var errorMessage = error?.Code switch
            {
                "PasswordTooShort" => $"A senha deve ter no mínimo {_userManager.Options.Password.RequiredLength} caracteres.",
                "PasswordRequiresNonAlphanumeric" => "A senha deve conter pelo menos um caractere especial (!@#$%^&*).",
                "PasswordRequiresDigit" => "A senha deve conter pelo menos um número (0-9).",
                "PasswordRequiresUpper" => "A senha deve conter pelo menos uma letra maiúscula (A-Z).",
                "PasswordRequiresLower" => "A senha deve conter pelo menos uma letra minúscula (a-z).",
                "DuplicateUserName" => "Este e-mail já está em uso. Tente fazer login ou use outro e-mail.",
                "InvalidEmail" => "O formato do e-mail é inválido. Por favor, digite um e-mail válido.",
                _ => error?.Description ?? "Falha ao criar conta. Verifique os dados e tente novamente."
            };

            return new AuthResult 
            { 
                Success = false, 
                ErrorMessage = errorMessage,
                ErrorCode = error?.Code ?? "REGISTRATION_FAILED"
            };
        }

        await CreateDefaultCategoriesAsync(user.Id);

        // Enviar email de boas-vindas em background
        _ = Task.Run(async () =>
        {
            try
            {
                var fullName = $"{user.FirstName} {user.LastName}";
                await _emailService.SendWelcomeEmailAsync(user.Email!, fullName);
                _logger.LogInformation($"📧 Email de boas-vindas enviado para {user.Email}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Erro ao enviar email de boas-vindas para {user.Email}");
            }
        });

        return new AuthResult
        {
            Success = true,
            Data = GenerateAuthResponse(user)
        };
    }

    public async Task<AuthResult> LoginAsync(LoginDto loginDto)
    {
        var user = await _userManager.FindByEmailAsync(loginDto.Email);
        
        // Não revelar se o e-mail existe ou não por segurança
        if (user == null)
            return new AuthResult 
            { 
                Success = false, 
                ErrorMessage = "E-mail ou senha incorretos. Verifique seus dados e tente novamente.",
                ErrorCode = "INVALID_CREDENTIALS"
            };

        var isPasswordValid = await _userManager.CheckPasswordAsync(user, loginDto.Password);
        
        if (!isPasswordValid)
            return new AuthResult 
            { 
                Success = false, 
                ErrorMessage = "E-mail ou senha incorretos. Verifique seus dados e tente novamente.",
                ErrorCode = "INVALID_CREDENTIALS"
            };

        return new AuthResult
        {
            Success = true,
            Data = GenerateAuthResponse(user)
        };
    }

    private AuthResponseDto GenerateAuthResponse(User user)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"]!;
        var expirationMinutes = int.Parse(jwtSettings["ExpirationMinutes"]!);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Email, user.Email!),
            new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes);

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials
        );

        return new AuthResponseDto
        {
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            Email = user.Email!,
            FirstName = user.FirstName,
            LastName = user.LastName,
            ExpiresAt = expiresAt
        };
    }

    private async Task CreateDefaultCategoriesAsync(string userId)
    {
        var defaultCategories = new[]
        {
            new Category { UserId = userId, Name = "Alimentação", Icon = "🍔", Color = "#FF6B6B", IsDefault = true, CreatedAt = DateTime.UtcNow },
            new Category { UserId = userId, Name = "Transporte", Icon = "🚗", Color = "#4ECDC4", IsDefault = true, CreatedAt = DateTime.UtcNow },
            new Category { UserId = userId, Name = "Moradia", Icon = "🏠", Color = "#95E1D3", IsDefault = true, CreatedAt = DateTime.UtcNow },
            new Category { UserId = userId, Name = "Lazer", Icon = "🎮", Color = "#F38181", IsDefault = true, CreatedAt = DateTime.UtcNow },
            new Category { UserId = userId, Name = "Saúde", Icon = "💊", Color = "#AA96DA", IsDefault = true, CreatedAt = DateTime.UtcNow },
            new Category { UserId = userId, Name = "Educação", Icon = "📚", Color = "#FCBAD3", IsDefault = true, CreatedAt = DateTime.UtcNow },
            new Category { UserId = userId, Name = "Salário", Icon = "💰", Color = "#38E54D", IsDefault = true, CreatedAt = DateTime.UtcNow },
            new Category { UserId = userId, Name = "Investimentos", Icon = "📈", Color = "#3423A6", IsDefault = true, CreatedAt = DateTime.UtcNow },
            new Category { UserId = userId, Name = "Outros", Icon = "📦", Color = "#9E9E9E", IsDefault = true, CreatedAt = DateTime.UtcNow }
        };

        _context.Categories.AddRange(defaultCategories);
        await _context.SaveChangesAsync();
    }
}