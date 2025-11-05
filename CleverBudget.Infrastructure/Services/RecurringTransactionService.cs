using CleverBudget.Core.Common;
using CleverBudget.Core.DTOs;
using CleverBudget.Core.Entities;
using CleverBudget.Core.Enums;
using CleverBudget.Core.Interfaces;
using CleverBudget.Infrastructure.Data;
using CleverBudget.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace CleverBudget.Infrastructure.Services;

public class RecurringTransactionService : IRecurringTransactionService
{
    private readonly AppDbContext _context;

    public RecurringTransactionService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<RecurringTransactionResponseDto>> GetAllAsync(string userId, bool? isActive = null)
    {
        var query = _context.RecurringTransactions
            .Include(r => r.Category)
            .Where(r => r.UserId == userId);

        if (isActive.HasValue)
            query = query.Where(r => r.IsActive == isActive.Value);

        var recurringTransactions = await query
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return recurringTransactions.Select(MapToDto);
    }

    public async Task<PagedResult<RecurringTransactionResponseDto>> GetPagedAsync(
        string userId, 
        PaginationParams paginationParams, 
        bool? isActive = null)
    {
        var query = _context.RecurringTransactions
            .Include(r => r.Category)
            .Where(r => r.UserId == userId);

        if (isActive.HasValue)
            query = query.Where(r => r.IsActive == isActive.Value);

        query = ApplySorting(query, paginationParams.SortBy, paginationParams.SortOrder);

        var pagedQuery = query.Select(r => MapToDto(r));

        return await pagedQuery.ToPagedResultAsync(paginationParams);
    }

    public async Task<RecurringTransactionResponseDto?> GetByIdAsync(int id, string userId)
    {
        var recurringTransaction = await _context.RecurringTransactions
            .Include(r => r.Category)
            .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

        if (recurringTransaction == null)
            return null;

        return MapToDto(recurringTransaction);
    }

    public async Task<RecurringTransactionResponseDto?> CreateAsync(CreateRecurringTransactionDto dto, string userId)
    {
        var categoryExists = await _context.Categories
            .AnyAsync(c => c.Id == dto.CategoryId && c.UserId == userId);

        if (!categoryExists)
            return null;

        // Validações de frequência
        if (dto.Frequency == RecurrenceFrequency.Monthly && !dto.DayOfMonth.HasValue)
            return null;

        if (dto.Frequency == RecurrenceFrequency.Weekly && !dto.DayOfWeek.HasValue)
            return null;

        var recurringTransaction = new RecurringTransaction
        {
            UserId = userId,
            Amount = dto.Amount,
            Type = dto.Type,
            Description = dto.Description,
            CategoryId = dto.CategoryId,
            Frequency = dto.Frequency,
            StartDate = dto.StartDate.Date,
            EndDate = dto.EndDate?.Date,
            DayOfMonth = dto.DayOfMonth,
            DayOfWeek = dto.DayOfWeek,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.RecurringTransactions.Add(recurringTransaction);
        await _context.SaveChangesAsync();

        return await GetByIdAsync(recurringTransaction.Id, userId);
    }

    public async Task<RecurringTransactionResponseDto?> UpdateAsync(int id, UpdateRecurringTransactionDto dto, string userId)
    {
        var recurringTransaction = await _context.RecurringTransactions
            .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

        if (recurringTransaction == null)
            return null;

        if (dto.Amount.HasValue)
            recurringTransaction.Amount = dto.Amount.Value;

        if (!string.IsNullOrEmpty(dto.Description))
            recurringTransaction.Description = dto.Description;

        if (dto.EndDate.HasValue)
            recurringTransaction.EndDate = dto.EndDate.Value.Date;

        if (dto.IsActive.HasValue)
            recurringTransaction.IsActive = dto.IsActive.Value;

        await _context.SaveChangesAsync();

        return await GetByIdAsync(recurringTransaction.Id, userId);
    }

    public async Task<bool> DeleteAsync(int id, string userId)
    {
        var recurringTransaction = await _context.RecurringTransactions
            .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

        if (recurringTransaction == null)
            return false;

        _context.RecurringTransactions.Remove(recurringTransaction);
        await _context.SaveChangesAsync();

        return true;
    }

    private RecurringTransactionResponseDto MapToDto(RecurringTransaction r)
    {
        return new RecurringTransactionResponseDto
        {
            Id = r.Id,
            Amount = r.Amount,
            Type = r.Type,
            TypeDescription = r.Type == TransactionType.Income ? "Receita" : "Despesa",
            Description = r.Description,
            CategoryId = r.CategoryId,
            CategoryName = r.Category.Name,
            CategoryIcon = r.Category.Icon ?? "",
            CategoryColor = r.Category.Color ?? "",
            Frequency = r.Frequency,
            FrequencyDescription = GetFrequencyDescription(r.Frequency),
            StartDate = r.StartDate,
            EndDate = r.EndDate,
            DayOfMonth = r.DayOfMonth,
            DayOfWeek = r.DayOfWeek,
            DayOfWeekDescription = r.DayOfWeek.HasValue 
                ? CultureInfo.GetCultureInfo("pt-BR").DateTimeFormat.GetDayName(r.DayOfWeek.Value) 
                : null,
            IsActive = r.IsActive,
            LastGeneratedDate = r.LastGeneratedDate,
            NextGenerationDate = CalculateNextGenerationDate(r),
            CreatedAt = r.CreatedAt
        };
    }

    private string GetFrequencyDescription(RecurrenceFrequency frequency)
    {
        return frequency switch
        {
            RecurrenceFrequency.Daily => "Diária",
            RecurrenceFrequency.Weekly => "Semanal",
            RecurrenceFrequency.Monthly => "Mensal",
            RecurrenceFrequency.Yearly => "Anual",
            _ => "Desconhecida"
        };
    }

    private DateTime? CalculateNextGenerationDate(RecurringTransaction r)
    {
        if (!r.IsActive)
            return null;

        var today = DateTime.UtcNow.Date;
        var baseDate = r.LastGeneratedDate?.Date ?? r.StartDate.Date;

        if (baseDate > today)
            return baseDate;

        DateTime nextDate = r.Frequency switch
        {
            RecurrenceFrequency.Daily => baseDate.AddDays(1),
            RecurrenceFrequency.Weekly => baseDate.AddDays(7),
            RecurrenceFrequency.Monthly => baseDate.AddMonths(1),
            RecurrenceFrequency.Yearly => baseDate.AddYears(1),
            _ => baseDate
        };

        while (nextDate <= today)
        {
            nextDate = r.Frequency switch
            {
                RecurrenceFrequency.Daily => nextDate.AddDays(1),
                RecurrenceFrequency.Weekly => nextDate.AddDays(7),
                RecurrenceFrequency.Monthly => nextDate.AddMonths(1),
                RecurrenceFrequency.Yearly => nextDate.AddYears(1),
                _ => nextDate
            };
        }

        if (r.EndDate.HasValue && nextDate > r.EndDate.Value)
            return null;

        return nextDate;
    }

    private IQueryable<RecurringTransaction> ApplySorting(
        IQueryable<RecurringTransaction> query,
        string? sortBy,
        string? sortOrder)
    {
        var isDescending = sortOrder?.ToLower() == "desc";

        return sortBy?.ToLower() switch
        {
            "amount" => isDescending 
                ? query.OrderByDescending(r => r.Amount) 
                : query.OrderBy(r => r.Amount),
            
            "description" => isDescending 
                ? query.OrderByDescending(r => r.Description) 
                : query.OrderBy(r => r.Description),
            
            "frequency" => isDescending 
                ? query.OrderByDescending(r => r.Frequency) 
                : query.OrderBy(r => r.Frequency),
            
            "startdate" => isDescending 
                ? query.OrderByDescending(r => r.StartDate) 
                : query.OrderBy(r => r.StartDate),
            
            _ => query.OrderByDescending(r => r.CreatedAt)
        };
    }
}