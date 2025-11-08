using CleverBudget.Core.Common;
using CleverBudget.Core.DTOs;
using CleverBudget.Core.Enums;
using System.IO;

namespace CleverBudget.Core.Interfaces;

public interface ITransactionService
{
    Task<IEnumerable<TransactionResponseDto>> GetAllAsync(
        string userId, 
        TransactionType? type = null, 
        int? categoryId = null, 
        DateTime? startDate = null, 
        DateTime? endDate = null,
        string? search = null,
        decimal? minAmount = null,
        decimal? maxAmount = null,
    bool includeCategory = false);

    Task<PagedResult<TransactionResponseDto>> GetPagedAsync(
        string userId,
        PaginationParams paginationParams,
        TransactionType? type = null,
        int? categoryId = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? search = null,
        decimal? minAmount = null,
        decimal? maxAmount = null,
        bool includeCategory = false);

    Task<TransactionImportResultDto> ImportFromCsvAsync(string userId, Stream csvStream, TransactionImportOptions options);

    Task<TransactionResponseDto?> GetByIdAsync(int id, string userId);
    Task<TransactionResponseDto?> CreateAsync(CreateTransactionDto dto, string userId);
    Task<TransactionResponseDto?> UpdateAsync(int id, UpdateTransactionDto dto, string userId);
    Task<bool> DeleteAsync(int id, string userId);
}