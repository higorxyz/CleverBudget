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

public class BudgetService : IBudgetService
{
    private readonly AppDbContext _context;

    public BudgetService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<BudgetResponseDto>> GetAllAsync(string userId, int? year = null, int? month = null)
    {
        var query = _context.Budgets
            .Include(b => b.Category)
            .Where(b => b.UserId == userId);

        if (year.HasValue)
            query = query.Where(b => b.Year == year.Value);

        if (month.HasValue)
            query = query.Where(b => b.Month == month.Value);

        var budgets = await query
            .OrderByDescending(b => b.Year)
            .ThenByDescending(b => b.Month)
            .ThenBy(b => b.Category.Name)
            .ToListAsync();

        var result = new List<BudgetResponseDto>();
        foreach (var budget in budgets)
        {
            result.Add(await MapToDtoAsync(budget, userId));
        }

        return result;
    }

    public async Task<PagedResult<BudgetResponseDto>> GetPagedAsync(
        string userId, 
        PaginationParams paginationParams, 
        int? year = null, 
        int? month = null)
    {
        var query = _context.Budgets
            .Include(b => b.Category)
            .Where(b => b.UserId == userId);

        if (year.HasValue)
            query = query.Where(b => b.Year == year.Value);

        if (month.HasValue)
            query = query.Where(b => b.Month == month.Value);

        query = ApplySorting(query, paginationParams.SortBy, paginationParams.SortOrder);

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((paginationParams.Page - 1) * paginationParams.PageSize)
            .Take(paginationParams.PageSize)
            .ToListAsync();

        var dtos = new List<BudgetResponseDto>();
        foreach (var budget in items)
        {
            dtos.Add(await MapToDtoAsync(budget, userId));
        }

        return new PagedResult<BudgetResponseDto>(
            dtos,
            paginationParams.Page,
            paginationParams.PageSize,
            totalCount
        );
    }

    public async Task<BudgetResponseDto?> GetByIdAsync(int id, string userId)
    {
        var budget = await _context.Budgets
            .Include(b => b.Category)
            .FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);

        if (budget == null)
            return null;

        return await MapToDtoAsync(budget, userId);
    }

    public async Task<BudgetResponseDto?> GetByCategoryAndPeriodAsync(int categoryId, int month, int year, string userId)
    {
        var budget = await _context.Budgets
            .Include(b => b.Category)
            .FirstOrDefaultAsync(b => 
                b.CategoryId == categoryId && 
                b.Month == month && 
                b.Year == year && 
                b.UserId == userId);

        if (budget == null)
            return null;

        return await MapToDtoAsync(budget, userId);
    }

    public async Task<BudgetResponseDto?> CreateAsync(CreateBudgetDto dto, string userId)
    {
        var categoryExists = await _context.Categories
            .AnyAsync(c => c.Id == dto.CategoryId && c.UserId == userId);

        if (!categoryExists)
            return null;

        var existingBudget = await _context.Budgets
            .FirstOrDefaultAsync(b => 
                b.CategoryId == dto.CategoryId && 
                b.Month == dto.Month && 
                b.Year == dto.Year && 
                b.UserId == userId);

        if (existingBudget != null)
            return null;

        var budget = new Budget
        {
            UserId = userId,
            CategoryId = dto.CategoryId,
            Amount = dto.Amount,
            Month = dto.Month,
            Year = dto.Year,
            AlertAt50Percent = dto.AlertAt50Percent,
            AlertAt80Percent = dto.AlertAt80Percent,
            AlertAt100Percent = dto.AlertAt100Percent,
            CreatedAt = DateTime.UtcNow
        };

        _context.Budgets.Add(budget);
        await _context.SaveChangesAsync();

        return await GetByIdAsync(budget.Id, userId);
    }

    public async Task<BudgetResponseDto?> UpdateAsync(int id, UpdateBudgetDto dto, string userId)
    {
        var budget = await _context.Budgets
            .FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);

        if (budget == null)
            return null;

        if (dto.Amount.HasValue)
            budget.Amount = dto.Amount.Value;

        if (dto.AlertAt50Percent.HasValue)
            budget.AlertAt50Percent = dto.AlertAt50Percent.Value;

        if (dto.AlertAt80Percent.HasValue)
            budget.AlertAt80Percent = dto.AlertAt80Percent.Value;

        if (dto.AlertAt100Percent.HasValue)
            budget.AlertAt100Percent = dto.AlertAt100Percent.Value;

        await _context.SaveChangesAsync();

        return await GetByIdAsync(budget.Id, userId);
    }

    public async Task<bool> DeleteAsync(int id, string userId)
    {
        var budget = await _context.Budgets
            .FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);

        if (budget == null)
            return false;

        _context.Budgets.Remove(budget);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<IEnumerable<BudgetResponseDto>> GetCurrentMonthBudgetsAsync(string userId)
    {
        var now = DateTime.UtcNow;
        return await GetAllAsync(userId, now.Year, now.Month);
    }

    public async Task<decimal> GetTotalBudgetForMonthAsync(string userId, int month, int year)
    {
        return await _context.Budgets
            .Where(b => b.UserId == userId && b.Month == month && b.Year == year)
            .SumAsync(b => b.Amount);
    }

    public async Task<decimal> GetTotalSpentForMonthAsync(string userId, int month, int year)
    {
        var startDate = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        return await _context.Transactions
            .Where(t => 
                t.UserId == userId && 
                t.Type == TransactionType.Expense &&
                t.Date >= startDate && 
                t.Date <= endDate)
            .SumAsync(t => t.Amount);
    }

    private async Task<BudgetResponseDto> MapToDtoAsync(Budget budget, string userId)
    {
        var spent = await GetSpentForCategoryAsync(userId, budget.CategoryId, budget.Month, budget.Year);
        var remaining = budget.Amount - spent;
        var percentageUsed = budget.Amount > 0 ? (spent / budget.Amount) * 100 : 0;

        string status;
        if (percentageUsed >= 100)
            status = "Excedido";
        else if (percentageUsed >= 80)
            status = "Crítico";
        else if (percentageUsed >= 50)
            status = "Alerta";
        else
            status = "Normal";

        var monthName = CultureInfo.GetCultureInfo("pt-BR").DateTimeFormat.GetMonthName(budget.Month);

        return new BudgetResponseDto
        {
            Id = budget.Id,
            CategoryId = budget.CategoryId,
            CategoryName = budget.Category.Name,
            CategoryIcon = budget.Category.Icon ?? "",
            CategoryColor = budget.Category.Color ?? "",
            Amount = budget.Amount,
            Month = budget.Month,
            Year = budget.Year,
            MonthName = $"{monthName}/{budget.Year}",
            Spent = spent,
            Remaining = remaining,
            PercentageUsed = Math.Round(percentageUsed, 2),
            Status = status,
            AlertAt50Percent = budget.AlertAt50Percent,
            AlertAt80Percent = budget.AlertAt80Percent,
            AlertAt100Percent = budget.AlertAt100Percent,
            Alert50Sent = budget.Alert50Sent,
            Alert80Sent = budget.Alert80Sent,
            Alert100Sent = budget.Alert100Sent,
            CreatedAt = budget.CreatedAt
        };
    }

    private async Task<decimal> GetSpentForCategoryAsync(string userId, int categoryId, int month, int year)
    {
        var startDate = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        return await _context.Transactions
            .Where(t => 
                t.UserId == userId && 
                t.CategoryId == categoryId &&
                t.Type == TransactionType.Expense &&
                t.Date >= startDate && 
                t.Date <= endDate)
            .SumAsync(t => t.Amount);
    }

    private IQueryable<Budget> ApplySorting(
        IQueryable<Budget> query,
        string? sortBy,
        string? sortOrder)
    {
        var isDescending = sortOrder?.ToLower() == "desc";

        return sortBy?.ToLower() switch
        {
            "amount" => isDescending 
                ? query.OrderByDescending(b => b.Amount) 
                : query.OrderBy(b => b.Amount),
            
            "category" => isDescending 
                ? query.OrderByDescending(b => b.Category.Name) 
                : query.OrderBy(b => b.Category.Name),
            
            "month" => isDescending 
                ? query.OrderByDescending(b => b.Year).ThenByDescending(b => b.Month)
                : query.OrderBy(b => b.Year).ThenBy(b => b.Month),
            
            _ => query.OrderByDescending(b => b.Year)
                      .ThenByDescending(b => b.Month)
                      .ThenBy(b => b.Category.Name)
        };
    }
}
