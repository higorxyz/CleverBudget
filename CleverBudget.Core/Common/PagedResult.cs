namespace CleverBudget.Core.Common;

/// <summary>
/// Classe genérica para resultados paginados
/// </summary>
/// <typeparam name="T">Tipo de dados a ser retornado</typeparam>
public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;

    public PagedResult()
    {
    }

    public PagedResult(List<T> items, int page, int pageSize, int totalCount)
    {
        Items = items;
        Page = page;
        PageSize = pageSize;
        TotalCount = totalCount;
    }

    /// <summary>
    /// Cria um resultado paginado vazio
    /// </summary>
    public static PagedResult<T> Empty(int page, int pageSize)
    {
        return new PagedResult<T>(new List<T>(), page, pageSize, 0);
    }
}

/// <summary>
/// Parâmetros de paginação para requisições
/// </summary>
public class PaginationParams
{
    private const int MaxPageSize = 100;
    private int _pageSize = 10;

    public int Page { get; set; } = 1;

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value > MaxPageSize ? MaxPageSize : value;
    }

    public string? SortBy { get; set; }
    public string? SortOrder { get; set; } = "desc"; // asc ou desc
}