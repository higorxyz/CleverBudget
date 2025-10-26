using CleverBudget.Core.Common;
using CleverBudget.Core.DTOs;
using CleverBudget.Core.Enums;

namespace CleverBudget.Core.Interfaces;

public interface ITransactionService
{
    Task<IEnumerable<TransactionResponseDto>> GetAllAsync(
        string userId, 
        TransactionType? type = null, 
        int? categoryId = null, 
        DateTime? startDate = null, 
        DateTime? endDate = null);

    Task<PagedResult<TransactionResponseDto>> GetPagedAsync(
        string userId,
        PaginationParams paginationParams,
        TransactionType? type = null,
        int? categoryId = null,
        DateTime? startDate = null,
        DateTime? endDate = null);

    Task<TransactionResponseDto?> GetByIdAsync(int id, string userId);
    Task<TransactionResponseDto?> CreateAsync(CreateTransactionDto dto, string userId);
    Task<TransactionResponseDto?> UpdateAsync(int id, UpdateTransactionDto dto, string userId);
    Task<bool> DeleteAsync(int id, string userId);
}