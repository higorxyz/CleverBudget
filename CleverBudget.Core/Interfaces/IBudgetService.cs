using CleverBudget.Core.Common;
using CleverBudget.Core.DTOs;

namespace CleverBudget.Core.Interfaces;

/// <summary>
/// Interface para gerenciamento de orçamentos mensais
/// </summary>
public interface IBudgetService
{
    Task<IEnumerable<BudgetResponseDto>> GetAllAsync(string userId, int? year = null, int? month = null);
    Task<PagedResult<BudgetResponseDto>> GetPagedAsync(string userId, PaginationParams paginationParams, int? year = null, int? month = null);
    Task<BudgetResponseDto?> GetByIdAsync(int id, string userId);
    Task<BudgetResponseDto?> GetByCategoryAndPeriodAsync(int categoryId, int month, int year, string userId);
    Task<BudgetResponseDto?> CreateAsync(CreateBudgetDto dto, string userId);
    Task<BudgetResponseDto?> UpdateAsync(int id, UpdateBudgetDto dto, string userId);
    Task<bool> DeleteAsync(int id, string userId);
    Task<IEnumerable<BudgetResponseDto>> GetCurrentMonthBudgetsAsync(string userId);
    Task<decimal> GetTotalBudgetForMonthAsync(string userId, int month, int year);
    Task<decimal> GetTotalSpentForMonthAsync(string userId, int month, int year);
}
