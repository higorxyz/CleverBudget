using CleverBudget.Core.DTOs;

namespace CleverBudget.Core.Interfaces;

public interface IUserProfileService
{
    Task<UserProfileDto?> GetProfileAsync(string userId);
    Task<bool> UpdateProfileAsync(string userId, UpdateProfileDto dto);
    Task<bool> ChangePasswordAsync(string userId, ChangePasswordDto dto);
    Task<bool> UpdatePhotoAsync(string userId, string photoUrl);
}
