using CleverBudget.Core.DTOs;
using CleverBudget.Core.Entities;
using CleverBudget.Core.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace CleverBudget.Infrastructure.Services;

public class UserProfileService : IUserProfileService
{
    private readonly UserManager<User> _userManager;
    private readonly ILogger<UserProfileService> _logger;

    public UserProfileService(UserManager<User> userManager, ILogger<UserProfileService> logger)
    {
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<UserProfileDto?> GetProfileAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            _logger.LogWarning("User not found: {UserId}", userId);
            return null;
        }

        return new UserProfileDto
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email ?? string.Empty,
            PhotoUrl = user.PhotoUrl,
            CreatedAt = user.CreatedAt
        };
    }

    public async Task<bool> UpdateProfileAsync(string userId, UpdateProfileDto dto)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            _logger.LogWarning("User not found: {UserId}", userId);
            return false;
        }

        user.FirstName = dto.FirstName;
        user.LastName = dto.LastName;

        var result = await _userManager.UpdateAsync(user);
        
        if (result.Succeeded)
        {
            _logger.LogInformation("Profile updated for user: {UserId}", userId);
            return true;
        }

        _logger.LogError("Failed to update profile for user {UserId}: {Errors}", 
            userId, string.Join(", ", result.Errors.Select(e => e.Description)));
        return false;
    }

    public async Task<bool> ChangePasswordAsync(string userId, ChangePasswordDto dto)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            _logger.LogWarning("User not found: {UserId}", userId);
            return false;
        }

        if (dto.NewPassword != dto.ConfirmPassword)
        {
            _logger.LogWarning("Password confirmation mismatch for user: {UserId}", userId);
            return false;
        }

        var result = await _userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);
        
        if (result.Succeeded)
        {
            _logger.LogInformation("Password changed for user: {UserId}", userId);
            return true;
        }

        _logger.LogError("Failed to change password for user {UserId}: {Errors}", 
            userId, string.Join(", ", result.Errors.Select(e => e.Description)));
        return false;
    }

    public async Task<bool> UpdatePhotoAsync(string userId, string photoUrl)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            _logger.LogWarning("User not found: {UserId}", userId);
            return false;
        }

        user.PhotoUrl = photoUrl;
        var result = await _userManager.UpdateAsync(user);
        
        if (result.Succeeded)
        {
            _logger.LogInformation("Photo updated for user: {UserId}", userId);
            return true;
        }

        _logger.LogError("Failed to update photo for user {UserId}: {Errors}", 
            userId, string.Join(", ", result.Errors.Select(e => e.Description)));
        return false;
    }
}
