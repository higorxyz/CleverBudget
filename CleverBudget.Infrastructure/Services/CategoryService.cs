using CleverBudget.Core.Common;
using CleverBudget.Core.DTOs;
using CleverBudget.Core.Entities;
using CleverBudget.Core.Interfaces;
using CleverBudget.Infrastructure.Data;
using CleverBudget.Infrastructure.Extensions;
using CleverBudget.Infrastructure.Helpers;
using Microsoft.EntityFrameworkCore;

namespace CleverBudget.Infrastructure.Services;

public class CategoryService : ICategoryService
{
    private readonly AppDbContext _context;

    public CategoryService(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Método NOVO com paginação
    /// </summary>
    public async Task<PagedResult<CategoryResponseDto>> GetPagedAsync(
        string userId,
        PaginationParams paginationParams,
        CategoryFilterOptions? filter = null,
        bool includeUsage = false)
    {
        var query = _context.Categories
            .Where(c => c.UserId == userId);

        if (filter != null)
        {
            query = ApplyFilters(query, filter, userId);
        }

        query = ApplySorting(query, paginationParams.SortBy, paginationParams.SortOrder);

        var pagedQuery = query.Select(c => new CategoryResponseDto
        {
            Id = c.Id,
            Name = c.Name,
            Icon = c.Icon,
            Color = c.Color,
            IsDefault = c.IsDefault,
            CreatedAt = c.CreatedAt,
            Kind = c.Kind,
            Segment = c.Segment ?? string.Empty,
            Tags = CategoryTagHelper.Parse(c.Tags)
        });

        var pagedResult = await pagedQuery.ToPagedResultAsync(paginationParams);

        if (includeUsage && pagedResult.Items.Count > 0)
        {
            var usageLookup = await BuildUsageLookupAsync(userId, pagedResult.Items.Select(c => c.Id).ToArray());

            foreach (var category in pagedResult.Items)
            {
                if (usageLookup.TryGetValue(category.Id, out var usage))
                {
                    category.Usage = usage;
                }
            }
        }

        return pagedResult;
    }

    public async Task<IEnumerable<CategoryResponseDto>> GetAllAsync(string userId)
    {
        var categories = await _context.Categories
            .Where(c => c.UserId == userId)
            .OrderBy(c => c.Name)
            .Select(c => new CategoryResponseDto
            {
                Id = c.Id,
                Name = c.Name,
                Icon = c.Icon,
                Color = c.Color,
                IsDefault = c.IsDefault,
                CreatedAt = c.CreatedAt,
                Kind = c.Kind,
                Segment = c.Segment ?? string.Empty,
                Tags = CategoryTagHelper.Parse(c.Tags)
            })
            .ToListAsync();

        return categories;
    }

    private IQueryable<Category> ApplyFilters(IQueryable<Category> query, CategoryFilterOptions filter, string userId)
    {
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim();
            var pattern = $"%{search}%";
            query = query.Where(c => EF.Functions.Like(c.Name, pattern) || (c.Segment != null && EF.Functions.Like(c.Segment, pattern)));
        }

        if (filter.Kinds != null && filter.Kinds.Count > 0)
        {
            query = query.Where(c => filter.Kinds.Contains(c.Kind));
        }

        if (filter.Segments != null && filter.Segments.Count > 0)
        {
            var normalizedSegments = filter.Segments
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim().ToLower())
                .ToArray();

            if (normalizedSegments.Length > 0)
            {
                query = query.Where(c => c.Segment != null && normalizedSegments.Contains(c.Segment.ToLower()));
            }
        }

        if (filter.OnlyWithGoals.HasValue)
        {
            if (filter.OnlyWithGoals.Value)
            {
                query = query.Where(c => _context.Goals.Any(g => g.CategoryId == c.Id && g.UserId == userId));
            }
            else
            {
                query = query.Where(c => !_context.Goals.Any(g => g.CategoryId == c.Id && g.UserId == userId));
            }
        }

        if (filter.OnlyWithTransactions.HasValue)
        {
            if (filter.OnlyWithTransactions.Value)
            {
                query = query.Where(c => _context.Transactions.Any(t => t.CategoryId == c.Id && t.UserId == userId));
            }
            else
            {
                query = query.Where(c => !_context.Transactions.Any(t => t.CategoryId == c.Id && t.UserId == userId));
            }
        }

        return query;
    }

    private async Task<Dictionary<int, CategoryUsageSummaryDto>> BuildUsageLookupAsync(string userId, int[] categoryIds)
    {
        var now = DateTime.UtcNow;
        var currentMonth = now.Month;
        var currentYear = now.Year;

        var transactions = await _context.Transactions
            .Where(t => t.UserId == userId && categoryIds.Contains(t.CategoryId))
            .GroupBy(t => t.CategoryId)
            .Select(g => new
            {
                CategoryId = g.Key,
                Count = g.Count(),
                Total = g.Sum(t => t.Amount),
                LastDate = g.Max(t => (DateTime?)t.Date)
            })
            .ToListAsync();

        var activeGoals = await _context.Goals
            .Where(g => g.UserId == userId && categoryIds.Contains(g.CategoryId))
            .GroupBy(g => g.CategoryId)
            .Select(g => new
            {
                CategoryId = g.Key,
                ActiveCount = g.Count(goal => goal.Year > currentYear || (goal.Year == currentYear && goal.Month >= currentMonth))
            })
            .ToListAsync();

        var usageLookup = categoryIds.ToDictionary(id => id, id => new CategoryUsageSummaryDto());

        foreach (var item in transactions)
        {
            usageLookup[item.CategoryId].TransactionCount = item.Count;
            usageLookup[item.CategoryId].TransactionTotal = item.Total;
            usageLookup[item.CategoryId].LastTransactionDate = item.LastDate;
        }

        foreach (var goal in activeGoals)
        {
            usageLookup[goal.CategoryId].ActiveGoals = goal.ActiveCount;
        }

        foreach (var usage in usageLookup.Values)
        {
            if (usage.TransactionCount == 0 && usage.ActiveGoals == 0)
            {
                usage.LastTransactionDate = null;
            }
        }

        return usageLookup;
    }

    public async Task<CategoryResponseDto?> GetByIdAsync(int id, string userId)
    {
        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

        if (category == null)
            return null;

        return new CategoryResponseDto
        {
            Id = category.Id,
            Name = category.Name,
            Icon = category.Icon,
            Color = category.Color,
            IsDefault = category.IsDefault,
            CreatedAt = category.CreatedAt,
            Kind = category.Kind,
            Segment = category.Segment ?? string.Empty,
            Tags = CategoryTagHelper.Parse(category.Tags)
        };
    }

    public async Task<CategoryResponseDto?> CreateAsync(CreateCategoryDto dto, string userId)
    {
        var exists = await _context.Categories
            .AnyAsync(c => c.UserId == userId && c.Name.ToLower() == dto.Name.ToLower());

        if (exists)
            return null;

        var category = new Category
        {
            UserId = userId,
            Name = dto.Name,
            Icon = dto.Icon,
            Color = dto.Color,
            Kind = dto.Kind,
            Segment = string.IsNullOrWhiteSpace(dto.Segment) ? null : dto.Segment.Trim(),
            Tags = CategoryTagHelper.Serialize(dto.Tags),
            IsDefault = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        return await GetByIdAsync(category.Id, userId);
    }

    public async Task<CategoryResponseDto?> UpdateAsync(int id, UpdateCategoryDto dto, string userId)
    {
        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

        if (category == null)
            return null;

        if (category.IsDefault)
            return null;

        if (!string.IsNullOrEmpty(dto.Name))
        {
            var nameExists = await _context.Categories
                .AnyAsync(c => c.UserId == userId && c.Id != id && c.Name.ToLower() == dto.Name.ToLower());

            if (nameExists)
                return null;

            category.Name = dto.Name;
        }

        if (dto.Icon != null)
            category.Icon = dto.Icon;

        if (dto.Color != null)
            category.Color = dto.Color;

        if (dto.Kind.HasValue)
            category.Kind = dto.Kind.Value;

        if (dto.Segment != null)
            category.Segment = string.IsNullOrWhiteSpace(dto.Segment) ? null : dto.Segment.Trim();

        if (dto.Tags != null)
            category.Tags = CategoryTagHelper.Serialize(dto.Tags);

        await _context.SaveChangesAsync();

        return await GetByIdAsync(category.Id, userId);
    }

    public async Task<bool> DeleteAsync(int id, string userId)
    {
        var category = await _context.Categories
            .Include(c => c.Transactions)
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

        if (category == null)
            return false;

        if (category.IsDefault)
            return false;

        if (category.Transactions.Any())
            return false;

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();

        return true;
    }

    private IQueryable<Category> ApplySorting(
        IQueryable<Category> query,
        string? sortBy,
        string? sortOrder)
    {
        var isDescending = sortOrder?.ToLower() == "desc";

        return sortBy?.ToLower() switch
        {
            "name" => isDescending 
                ? query.OrderByDescending(c => c.Name) 
                : query.OrderBy(c => c.Name),
            
            "createdat" => isDescending 
                ? query.OrderByDescending(c => c.CreatedAt) 
                : query.OrderBy(c => c.CreatedAt),
            
            "isdefault" => isDescending 
                ? query.OrderByDescending(c => c.IsDefault) 
                : query.OrderBy(c => c.IsDefault),
            
            _ => query.OrderBy(c => c.Name) // Default: ordem alfabética
        };
    }

}