using CleverBudget.Core.Common;
using CleverBudget.Core.DTOs;
using CleverBudget.Core.Entities;
using CleverBudget.Core.Enums;
using CleverBudget.Core.Interfaces;
using CleverBudget.Infrastructure.Data;
using CleverBudget.Infrastructure.Extensions;
using CleverBudget.Infrastructure.Helpers;
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
        int? year = null,
        int? categoryId = null,
        CategoryKind? categoryKind = null)
    {
        var query = _context.Goals
            .Include(g => g.Category)
            .Where(g => g.UserId == userId);

        if (month.HasValue)
            query = query.Where(g => g.Month == month.Value);

        if (year.HasValue)
            query = query.Where(g => g.Year == year.Value);

        if (categoryId.HasValue)
            query = query.Where(g => g.CategoryId == categoryId.Value);

        if (categoryKind.HasValue)
            query = query.Where(g => g.Category.Kind == categoryKind.Value);

        query = ApplySorting(query, paginationParams.SortBy, paginationParams.SortOrder);

        var projectionQuery = query.Select(g => new GoalProjection
        {
            Id = g.Id,
            CategoryId = g.CategoryId,
            CategoryName = g.Category.Name,
            CategoryIcon = g.Category.Icon ?? string.Empty,
            CategoryColor = g.Category.Color ?? string.Empty,
            CategoryKind = g.Category.Kind,
            CategorySegment = g.Category.Segment ?? string.Empty,
            CategoryTags = g.Category.Tags,
            TargetAmount = g.TargetAmount,
            Month = g.Month,
            Year = g.Year,
            CreatedAt = g.CreatedAt
        });

        var projectionResult = await projectionQuery.ToPagedResultAsync(paginationParams);

        var mappedItems = projectionResult.Items
            .Select(MapGoalProjection)
            .ToList();

        return new PagedResult<GoalResponseDto>(
            mappedItems,
            projectionResult.Page,
            projectionResult.PageSize,
            projectionResult.TotalCount);
    }

    public async Task<IEnumerable<GoalResponseDto>> GetAllAsync(
        string userId,
        int? month = null,
        int? year = null,
        int? categoryId = null,
        CategoryKind? categoryKind = null)
    {
        var query = _context.Goals
            .Include(g => g.Category)
            .Where(g => g.UserId == userId);

        if (month.HasValue)
            query = query.Where(g => g.Month == month.Value);

        if (year.HasValue)
            query = query.Where(g => g.Year == year.Value);

        if (categoryId.HasValue)
            query = query.Where(g => g.CategoryId == categoryId.Value);

        if (categoryKind.HasValue)
            query = query.Where(g => g.Category.Kind == categoryKind.Value);

        var projections = await query
            .OrderByDescending(g => g.Year)
            .ThenByDescending(g => g.Month)
            .Select(g => new GoalProjection
            {
                Id = g.Id,
                CategoryId = g.CategoryId,
                CategoryName = g.Category.Name,
                CategoryIcon = g.Category.Icon ?? string.Empty,
                CategoryColor = g.Category.Color ?? string.Empty,
                CategoryKind = g.Category.Kind,
                CategorySegment = g.Category.Segment ?? string.Empty,
                CategoryTags = g.Category.Tags,
                TargetAmount = g.TargetAmount,
                Month = g.Month,
                Year = g.Year,
                CreatedAt = g.CreatedAt
            })
            .ToListAsync();

        return projections
            .Select(MapGoalProjection)
            .ToList();
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
            CategoryKind = goal.Category.Kind,
            CategorySegment = goal.Category.Segment ?? string.Empty,
            CategoryTags = CategoryTagHelper.Parse(goal.Category.Tags ?? "[]"),
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
        var targetMonth = month ?? DateTime.UtcNow.Month;
        var targetYear = year ?? DateTime.UtcNow.Year;

        var goals = await _context.Goals
            .Include(g => g.Category)
            .Where(g => g.UserId == userId && g.Month == targetMonth && g.Year == targetYear)
            .ToListAsync();

        var snapshots = await BuildGoalSnapshotsAsync(userId, goals);

        return snapshots
            .Select(MapSnapshotToStatus)
            .OrderByDescending(g => g.Percentage)
            .ToList();
    }

    public async Task<GoalInsightsSummaryDto> GetInsightsAsync(string userId, GoalInsightsFilterDto filter)
    {
        var query = _context.Goals
            .Include(g => g.Category)
            .Where(g => g.UserId == userId);

        if (filter.Month.HasValue)
            query = query.Where(g => g.Month == filter.Month.Value);

        if (filter.Year.HasValue)
            query = query.Where(g => g.Year == filter.Year.Value);

        if (filter.CategoryId.HasValue)
            query = query.Where(g => g.CategoryId == filter.CategoryId.Value);

        if (filter.CategoryKind.HasValue)
            query = query.Where(g => g.Category.Kind == filter.CategoryKind.Value);

        var goals = await query
            .OrderByDescending(g => g.Year)
            .ThenByDescending(g => g.Month)
            .ToListAsync();

        if (goals.Count == 0)
        {
            return new GoalInsightsSummaryDto();
        }

        var snapshots = await BuildGoalSnapshotsAsync(userId, goals);
        var overdue = new List<GoalInsightItemDto>();
        var atRisk = new List<GoalInsightItemDto>();
        var completed = new List<GoalInsightItemDto>();

        var riskThreshold = filter.RiskThresholdPercentage <= 0 ? 80m : filter.RiskThresholdPercentage;
        var now = DateTime.UtcNow.Date;

        foreach (var snapshot in snapshots)
        {
            var goal = snapshot.Goal;
            var endDate = new DateTime(goal.Year, goal.Month, DateTime.DaysInMonth(goal.Year, goal.Month));

            var insightItem = new GoalInsightItemDto
            {
                GoalId = goal.Id,
                CategoryId = goal.CategoryId,
                CategoryName = goal.Category.Name,
                CategoryIcon = goal.Category.Icon ?? string.Empty,
                CategoryColor = goal.Category.Color ?? string.Empty,
                CategoryKind = goal.Category.Kind,
                TargetAmount = goal.TargetAmount,
                CurrentAmount = snapshot.CurrentAmount,
                Percentage = snapshot.Percentage,
                RemainingAmount = Math.Max(goal.TargetAmount - snapshot.CurrentAmount, 0m),
                Month = goal.Month,
                Year = goal.Year,
                Status = snapshot.Status
            };

            var isCompleted = snapshot.Percentage >= 100m;
            var isOverdue = !isCompleted && endDate < now;
            var isAtRisk = !isCompleted && !isOverdue && snapshot.Percentage >= riskThreshold;

            if (isCompleted)
            {
                completed.Add(insightItem);
            }
            else if (isOverdue)
            {
                overdue.Add(insightItem);
            }
            else if (isAtRisk)
            {
                atRisk.Add(insightItem);
            }
        }

        return new GoalInsightsSummaryDto
        {
            Overdue = overdue,
            AtRisk = atRisk,
            Completed = completed,
            TotalTargetAmount = snapshots.Sum(s => s.Goal.TargetAmount),
            TotalCurrentAmount = snapshots.Sum(s => s.CurrentAmount),
            TotalGoals = snapshots.Count
        };
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

        private static GoalResponseDto MapGoalProjection(GoalProjection projection)
        {
            return new GoalResponseDto
            {
                Id = projection.Id,
                CategoryId = projection.CategoryId,
                CategoryName = projection.CategoryName,
                CategoryIcon = projection.CategoryIcon,
                CategoryColor = projection.CategoryColor,
                CategoryKind = projection.CategoryKind,
                CategorySegment = projection.CategorySegment,
                CategoryTags = CategoryTagHelper.Parse(projection.CategoryTags ?? "[]"),
                TargetAmount = projection.TargetAmount,
                Month = projection.Month,
                Year = projection.Year,
                CreatedAt = projection.CreatedAt
            };
        }

        private async Task<List<GoalSnapshot>> BuildGoalSnapshotsAsync(string userId, List<Goal> goals)
        {
            if (goals.Count == 0)
            {
                return new List<GoalSnapshot>();
            }

            var goalKeys = goals
                .Select(g => new GoalKey(g.CategoryId, g.Year, g.Month))
                .Distinct()
                .ToList();

            var categoryIds = goalKeys.Select(k => k.CategoryId).Distinct().ToArray();
            var minDate = goalKeys.Min(k => new DateTime(k.Year, k.Month, 1));
            var maxDate = goalKeys.Max(k => new DateTime(k.Year, k.Month, 1).AddMonths(1).AddDays(-1));

            var transactionSlices = await _context.Transactions
                .Where(t => t.UserId == userId &&
                            t.Type == TransactionType.Expense &&
                            categoryIds.Contains(t.CategoryId) &&
                            t.Date >= minDate &&
                            t.Date <= maxDate)
                .Select(t => new { t.CategoryId, t.Date.Year, t.Date.Month, t.Amount })
                .ToListAsync();

            var totals = transactionSlices
                .GroupBy(t => new GoalKey(t.CategoryId, t.Year, t.Month))
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));

            var snapshots = new List<GoalSnapshot>(goals.Count);

            foreach (var goal in goals)
            {
                var key = new GoalKey(goal.CategoryId, goal.Year, goal.Month);
                totals.TryGetValue(key, out var currentAmount);
                var percentage = goal.TargetAmount > 0
                    ? Math.Round((currentAmount / goal.TargetAmount) * 100m, 2)
                    : 0m;

                var status = percentage switch
                {
                    < 80m => "OnTrack",
                    >= 80m and < 100m => "Warning",
                    _ => "Exceeded"
                };

                snapshots.Add(new GoalSnapshot(goal, currentAmount, percentage, status));
            }

            return snapshots;
        }

        private static GoalStatusDto MapSnapshotToStatus(GoalSnapshot snapshot)
        {
            return new GoalStatusDto
            {
                Id = snapshot.Goal.Id,
                CategoryId = snapshot.Goal.CategoryId,
                CategoryName = snapshot.Goal.Category.Name,
                CategoryIcon = snapshot.Goal.Category.Icon ?? string.Empty,
                CategoryColor = snapshot.Goal.Category.Color ?? string.Empty,
                CategoryKind = snapshot.Goal.Category.Kind,
                TargetAmount = snapshot.Goal.TargetAmount,
                CurrentAmount = snapshot.CurrentAmount,
                Percentage = snapshot.Percentage,
                Month = snapshot.Goal.Month,
                Year = snapshot.Goal.Year,
                Status = snapshot.Status
            };
        }

        private readonly record struct GoalKey(int CategoryId, int Year, int Month);

        private sealed record GoalSnapshot(Goal Goal, decimal CurrentAmount, decimal Percentage, string Status);

        private sealed class GoalProjection
        {
            public int Id { get; set; }
            public int CategoryId { get; set; }
            public string CategoryName { get; set; } = string.Empty;
            public string CategoryIcon { get; set; } = string.Empty;
            public string CategoryColor { get; set; } = string.Empty;
            public CategoryKind CategoryKind { get; set; }
            public string CategorySegment { get; set; } = string.Empty;
            public string? CategoryTags { get; set; }
            public decimal TargetAmount { get; set; }
            public int Month { get; set; }
            public int Year { get; set; }
            public DateTime CreatedAt { get; set; }
        }
}