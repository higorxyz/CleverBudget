using CleverBudget.Core.Common;
using CleverBudget.Core.DTOs;
using CleverBudget.Core.Enums;

namespace CleverBudget.Core.Interfaces;

public interface IGoalService
{
    Task<IEnumerable<GoalResponseDto>> GetAllAsync(
        string userId,
        int? month = null,
        int? year = null,
        int? categoryId = null,
        CategoryKind? categoryKind = null);

    Task<PagedResult<GoalResponseDto>> GetPagedAsync(
        string userId,
        PaginationParams paginationParams,
        int? month = null,
        int? year = null,
        int? categoryId = null,
        CategoryKind? categoryKind = null);
    Task<GoalResponseDto?> GetByIdAsync(int id, string userId);
    Task<GoalResponseDto?> CreateAsync(CreateGoalDto dto, string userId);
    Task<GoalResponseDto?> UpdateAsync(int id, UpdateGoalDto dto, string userId);
    Task<bool> DeleteAsync(int id, string userId);
    Task<IEnumerable<GoalStatusDto>> GetStatusAsync(string userId, int? month = null, int? year = null);
    Task<GoalInsightsSummaryDto> GetInsightsAsync(string userId, GoalInsightsFilterDto filter);
}