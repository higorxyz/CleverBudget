using CleverBudget.Core.Common;
using CleverBudget.Core.DTOs;

namespace CleverBudget.Core.Interfaces;

public interface IRecurringTransactionService
{
    Task<IEnumerable<RecurringTransactionResponseDto>> GetAllAsync(string userId, bool? isActive = null);
    Task<PagedResult<RecurringTransactionResponseDto>> GetPagedAsync(string userId, PaginationParams paginationParams, bool? isActive = null);
    Task<RecurringTransactionResponseDto?> GetByIdAsync(int id, string userId);
    Task<RecurringTransactionResponseDto?> CreateAsync(CreateRecurringTransactionDto dto, string userId);
    Task<RecurringTransactionResponseDto?> UpdateAsync(int id, UpdateRecurringTransactionDto dto, string userId);
    Task<bool> DeleteAsync(int id, string userId);
}