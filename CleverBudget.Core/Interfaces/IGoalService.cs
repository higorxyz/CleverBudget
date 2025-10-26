using CleverBudget.Core.Common;
using CleverBudget.Core.DTOs;

namespace CleverBudget.Core.Interfaces;

public interface IGoalService
{
    Task<IEnumerable<GoalResponseDto>> GetAllAsync(string userId, int? month = null, int? year = null);
    Task<PagedResult<GoalResponseDto>> GetPagedAsync(string userId, PaginationParams paginationParams, int? month = null, int? year = null);
    Task<GoalResponseDto?> GetByIdAsync(int id, string userId);
    Task<GoalResponseDto?> CreateAsync(CreateGoalDto dto, string userId);
    Task<GoalResponseDto?> UpdateAsync(int id, UpdateGoalDto dto, string userId);
    Task<bool> DeleteAsync(int id, string userId);
    Task<IEnumerable<GoalStatusDto>> GetStatusAsync(string userId, int? month = null, int? year = null);
}