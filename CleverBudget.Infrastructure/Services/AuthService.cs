using CleverBudget.Core.DTOs;
using CleverBudget.Core.Entities;
using CleverBudget.Core.Interfaces;
using CleverBudget.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
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

    public AuthService(UserManager<User> userManager, IConfiguration configuration, AppDbContext context)
    {
        _userManager = userManager;
        _configuration = configuration;
        _context = context;
    }

    public async Task<AuthResponseDto?> RegisterAsync(RegisterDto registerDto)
    {
        // Validar senhas
        if (registerDto.Password != registerDto.ConfirmPassword)
            return null;

        // Verificar se email já existe
        var existingUser = await _userManager.FindByEmailAsync(registerDto.Email);
        if (existingUser != null)
            return null;

        // Criar usuário
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
            return null;

        // Criar categorias padrão para o usuário
        await CreateDefaultCategoriesAsync(user.Id);

        // Gerar token
        return GenerateAuthResponse(user);
    }

    public async Task<AuthResponseDto?> LoginAsync(LoginDto loginDto)
    {
        var user = await _userManager.FindByEmailAsync(loginDto.Email);
        
        if (user == null)
            return null;

        var isPasswordValid = await _userManager.CheckPasswordAsync(user, loginDto.Password);
        
        if (!isPasswordValid)
            return null;

        return GenerateAuthResponse(user);
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