using CleverBudget.Core.Common;
using CleverBudget.Core.DTOs;
using CleverBudget.Core.Entities;
using CleverBudget.Core.Enums;
using CleverBudget.Core.Interfaces;
using CleverBudget.Infrastructure.Data;
using CleverBudget.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace CleverBudget.Infrastructure.Services;

public class GoalService : IGoalService
{
    private readonly AppDbContext _context;

    public GoalService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<GoalResponseDto>> GetPagedAsync(
        string userId, 
        PaginationParams paginationParams,
        int? month = null, 
        int? year = null)
    {
        var query = _context.Goals
            .Include(g => g.Category)
            .Where(g => g.UserId == userId);

        if (month.HasValue)
            query = query.Where(g => g.Month == month.Value);

        if (year.HasValue)
            query = query.Where(g => g.Year == year.Value);

        query = ApplySorting(query, paginationParams.SortBy, paginationParams.SortOrder);

        var pagedQuery = query.Select(g => new GoalResponseDto
        {
            Id = g.Id,
            CategoryId = g.CategoryId,
            CategoryName = g.Category.Name,
            CategoryIcon = g.Category.Icon ?? "",
            CategoryColor = g.Category.Color ?? "",
            TargetAmount = g.TargetAmount,
            Month = g.Month,
            Year = g.Year,
            CreatedAt = g.CreatedAt
        });

        return await pagedQuery.ToPagedResultAsync(paginationParams);
    }

    public async Task<IEnumerable<GoalResponseDto>> GetAllAsync(string userId, int? month = null, int? year = null)
    {
        var query = _context.Goals
            .Include(g => g.Category)
            .Where(g => g.UserId == userId);

        if (month.HasValue)
            query = query.Where(g => g.Month == month.Value);

        if (year.HasValue)
            query = query.Where(g => g.Year == year.Value);

        var goals = await query
            .OrderByDescending(g => g.Year)
            .ThenByDescending(g => g.Month)
            .Select(g => new GoalResponseDto
            {
                Id = g.Id,
                CategoryId = g.CategoryId,
                CategoryName = g.Category.Name,
                CategoryIcon = g.Category.Icon ?? "",
                CategoryColor = g.Category.Color ?? "",
                TargetAmount = g.TargetAmount,
                Month = g.Month,
                Year = g.Year,
                CreatedAt = g.CreatedAt
            })
            .ToListAsync();

        return goals;
    }

    public async Task<GoalResponseDto?> GetByIdAsync(int id, string userId)
    {
        var goal = await _context.Goals
            .Include(g => g.Category)
            .FirstOrDefaultAsync(g => g.Id == id && g.UserId == userId);

        if (goal == null)
            return null;

        return new GoalResponseDto
        {
            Id = goal.Id,
            CategoryId = goal.CategoryId,
            CategoryName = goal.Category.Name,
            CategoryIcon = goal.Category.Icon ?? "",
            CategoryColor = goal.Category.Color ?? "",
            TargetAmount = goal.TargetAmount,
            Month = goal.Month,
            Year = goal.Year,
            CreatedAt = goal.CreatedAt
        };
    }

    public async Task<GoalResponseDto?> CreateAsync(CreateGoalDto dto, string userId)
    {
        var categoryExists = await _context.Categories
            .AnyAsync(c => c.Id == dto.CategoryId && c.UserId == userId);

        if (!categoryExists)
            return null;

        var existingGoal = await _context.Goals
            .AnyAsync(g => g.UserId == userId && 
                          g.CategoryId == dto.CategoryId && 
                          g.Month == dto.Month && 
                          g.Year == dto.Year);

        if (existingGoal)
            return null;

        var goal = new Goal
        {
            UserId = userId,
            CategoryId = dto.CategoryId,
            TargetAmount = dto.TargetAmount,
            Month = dto.Month,
            Year = dto.Year,
            CreatedAt = DateTime.UtcNow
        };

        _context.Goals.Add(goal);
        await _context.SaveChangesAsync();

        return await GetByIdAsync(goal.Id, userId);
    }

    public async Task<GoalResponseDto?> UpdateAsync(int id, UpdateGoalDto dto, string userId)
    {
        var goal = await _context.Goals
            .FirstOrDefaultAsync(g => g.Id == id && g.UserId == userId);

        if (goal == null)
            return null;

        if (dto.TargetAmount.HasValue)
            goal.TargetAmount = dto.TargetAmount.Value;

        await _context.SaveChangesAsync();

        return await GetByIdAsync(goal.Id, userId);
    }

    public async Task<bool> DeleteAsync(int id, string userId)
    {
        var goal = await _context.Goals
            .FirstOrDefaultAsync(g => g.Id == id && g.UserId == userId);

        if (goal == null)
            return false;

        _context.Goals.Remove(goal);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<IEnumerable<GoalStatusDto>> GetStatusAsync(string userId, int? month = null, int? year = null)
    {
        var currentMonth = month ?? DateTime.Now.Month;
        var currentYear = year ?? DateTime.Now.Year;

        var goals = await _context.Goals
            .Include(g => g.Category)
            .Where(g => g.UserId == userId && g.Month == currentMonth && g.Year == currentYear)
            .ToListAsync();

        var startDate = new DateTime(currentYear, currentMonth, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        var goalsStatus = new List<GoalStatusDto>();

        foreach (var goal in goals)
        {
            var totalSpent = await _context.Transactions
                .Where(t => t.UserId == userId &&
                           t.CategoryId == goal.CategoryId &&
                           t.Type == TransactionType.Expense &&
                           t.Date >= startDate &&
                           t.Date <= endDate)
                .SumAsync(t => t.Amount);

            var percentage = goal.TargetAmount > 0 ? (totalSpent / goal.TargetAmount) * 100 : 0;

            var status = percentage switch
            {
                < 80 => "OnTrack",
                >= 80 and < 100 => "Warning",
                _ => "Exceeded"
            };

            goalsStatus.Add(new GoalStatusDto
            {
                Id = goal.Id,
                CategoryId = goal.CategoryId,
                CategoryName = goal.Category.Name,
                CategoryIcon = goal.Category.Icon ?? "",
                CategoryColor = goal.Category.Color ?? "",
                TargetAmount = goal.TargetAmount,
                CurrentAmount = totalSpent,
                Percentage = Math.Round(percentage, 2),
                Month = goal.Month,
                Year = goal.Year,
                Status = status
            });
        }

        return goalsStatus.OrderByDescending(g => g.Percentage);
    }

    private IQueryable<Goal> ApplySorting(
        IQueryable<Goal> query,
        string? sortBy,
        string? sortOrder)
    {
        var isDescending = sortOrder?.ToLower() == "desc";

        return sortBy?.ToLower() switch
        {
            "targetamount" => isDescending 
                ? query.OrderByDescending(g => g.TargetAmount) 
                : query.OrderBy(g => g.TargetAmount),
            
            "category" => isDescending 
                ? query.OrderByDescending(g => g.Category.Name) 
                : query.OrderBy(g => g.Category.Name),
            
            "month" => isDescending 
                ? query.OrderByDescending(g => g.Month) 
                : query.OrderBy(g => g.Month),
            
            "year" => isDescending 
                ? query.OrderByDescending(g => g.Year) 
                : query.OrderBy(g => g.Year),
            
            _ => query.OrderByDescending(g => g.Year).ThenByDescending(g => g.Month) // Default
        };
    }
}