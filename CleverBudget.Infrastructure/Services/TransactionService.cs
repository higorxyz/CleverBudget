using CleverBudget.Core.Common;
using CleverBudget.Core.DTOs;
using CleverBudget.Core.Entities;
using CleverBudget.Core.Enums;
using CleverBudget.Core.Interfaces;
using CleverBudget.Infrastructure.Data;
using CleverBudget.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace CleverBudget.Infrastructure.Services;

public class TransactionService : ITransactionService
{
    private readonly AppDbContext _context;

    public TransactionService(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Método NOVO com paginação
    /// </summary>
    public async Task<PagedResult<TransactionResponseDto>> GetPagedAsync(
        string userId,
        PaginationParams paginationParams,
        TransactionType? type = null,
        int? categoryId = null,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var query = _context.Transactions
            .Include(t => t.Category)
            .Where(t => t.UserId == userId);

        if (type.HasValue)
            query = query.Where(t => t.Type == type.Value);

        if (categoryId.HasValue)
            query = query.Where(t => t.CategoryId == categoryId.Value);

        if (startDate.HasValue)
            query = query.Where(t => t.Date >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(t => t.Date <= endDate.Value);

        query = ApplySorting(query, paginationParams.SortBy, paginationParams.SortOrder);

        var pagedQuery = query.Select(t => new TransactionResponseDto
        {
            Id = t.Id,
            Amount = t.Amount,
            Type = t.Type,
            Description = t.Description,
            CategoryId = t.CategoryId,
            CategoryName = t.Category.Name,
            CategoryIcon = t.Category.Icon ?? "",
            CategoryColor = t.Category.Color ?? "",
            Date = t.Date,
            CreatedAt = t.CreatedAt
        });

        return await pagedQuery.ToPagedResultAsync(paginationParams);
    }

    public async Task<IEnumerable<TransactionResponseDto>> GetAllAsync(
        string userId, 
        TransactionType? type = null, 
        int? categoryId = null, 
        DateTime? startDate = null, 
        DateTime? endDate = null)
    {
        var query = _context.Transactions
            .Include(t => t.Category)
            .Where(t => t.UserId == userId);

        if (type.HasValue)
            query = query.Where(t => t.Type == type.Value);

        if (categoryId.HasValue)
            query = query.Where(t => t.CategoryId == categoryId.Value);

        if (startDate.HasValue)
            query = query.Where(t => t.Date >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(t => t.Date <= endDate.Value);

        var transactions = await query
            .OrderByDescending(t => t.Date)
            .Select(t => new TransactionResponseDto
            {
                Id = t.Id,
                Amount = t.Amount,
                Type = t.Type,
                Description = t.Description,
                CategoryId = t.CategoryId,
                CategoryName = t.Category.Name,
                CategoryIcon = t.Category.Icon ?? "",
                CategoryColor = t.Category.Color ?? "",
                Date = t.Date,
                CreatedAt = t.CreatedAt
            })
            .ToListAsync();

        return transactions;
    }

    public async Task<TransactionResponseDto?> GetByIdAsync(int id, string userId)
    {
        var transaction = await _context.Transactions
            .Include(t => t.Category)
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

        if (transaction == null)
            return null;

        return new TransactionResponseDto
        {
            Id = transaction.Id,
            Amount = transaction.Amount,
            Type = transaction.Type,
            Description = transaction.Description,
            CategoryId = transaction.CategoryId,
            CategoryName = transaction.Category.Name,
            CategoryIcon = transaction.Category.Icon ?? "",
            CategoryColor = transaction.Category.Color ?? "",
            Date = transaction.Date,
            CreatedAt = transaction.CreatedAt
        };
    }

    public async Task<TransactionResponseDto?> CreateAsync(CreateTransactionDto dto, string userId)
    {
        var categoryExists = await _context.Categories
            .AnyAsync(c => c.Id == dto.CategoryId && c.UserId == userId);

        if (!categoryExists)
            return null;

        var transaction = new Transaction
        {
            UserId = userId,
            Amount = dto.Amount,
            Type = dto.Type,
            Description = dto.Description,
            CategoryId = dto.CategoryId,
            Date = dto.Date,
            CreatedAt = DateTime.UtcNow
        };

        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync();

        return await GetByIdAsync(transaction.Id, userId);
    }

    public async Task<TransactionResponseDto?> UpdateAsync(int id, UpdateTransactionDto dto, string userId)
    {
        var transaction = await _context.Transactions
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

        if (transaction == null)
            return null;

        if (dto.CategoryId.HasValue && dto.CategoryId.Value != transaction.CategoryId)
        {
            var categoryExists = await _context.Categories
                .AnyAsync(c => c.Id == dto.CategoryId.Value && c.UserId == userId);

            if (!categoryExists)
                return null;

            transaction.CategoryId = dto.CategoryId.Value;
        }

        if (dto.Amount.HasValue)
            transaction.Amount = dto.Amount.Value;

        if (dto.Type.HasValue)
            transaction.Type = dto.Type.Value;

        if (!string.IsNullOrEmpty(dto.Description))
            transaction.Description = dto.Description;

        if (dto.Date.HasValue)
            transaction.Date = dto.Date.Value;

        await _context.SaveChangesAsync();

        return await GetByIdAsync(transaction.Id, userId);
    }

    public async Task<bool> DeleteAsync(int id, string userId)
    {
        var transaction = await _context.Transactions
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

        if (transaction == null)
            return false;

        _context.Transactions.Remove(transaction);
        await _context.SaveChangesAsync();

        return true;
    }

    private IQueryable<Transaction> ApplySorting(
        IQueryable<Transaction> query,
        string? sortBy,
        string? sortOrder)
    {
        var isDescending = sortOrder?.ToLower() == "desc";

        return sortBy?.ToLower() switch
        {
            "date" => isDescending 
                ? query.OrderByDescending(t => t.Date) 
                : query.OrderBy(t => t.Date),
            
            "amount" => isDescending 
                ? query.OrderByDescending(t => t.Amount) 
                : query.OrderBy(t => t.Amount),
            
            "description" => isDescending 
                ? query.OrderByDescending(t => t.Description) 
                : query.OrderBy(t => t.Description),
            
            "category" => isDescending 
                ? query.OrderByDescending(t => t.Category.Name) 
                : query.OrderBy(t => t.Category.Name),
            
            _ => query.OrderByDescending(t => t.Date)
        };
    }
}