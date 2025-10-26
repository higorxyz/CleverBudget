using CleverBudget.Core.Common;
using Microsoft.EntityFrameworkCore;

namespace CleverBudget.Infrastructure.Extensions;

/// <summary>
/// Extension methods para facilitar paginação
/// </summary>
public static class QueryableExtensions
{
    /// <summary>
    /// Aplica paginação a uma query
    /// </summary>
    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query,
        int page,
        int pageSize)
    {
        var totalCount = await query.CountAsync();
        
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<T>(items, page, pageSize, totalCount);
    }

    /// <summary>
    /// Aplica paginação com parâmetros
    /// </summary>
    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query,
        PaginationParams paginationParams)
    {
        return await query.ToPagedResultAsync(
            paginationParams.Page,
            paginationParams.PageSize
        );
    }
}