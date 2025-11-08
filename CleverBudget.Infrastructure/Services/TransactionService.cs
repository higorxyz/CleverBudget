using CleverBudget.Core.Common;
using CleverBudget.Core.DTOs;
using CleverBudget.Core.Entities;
using CleverBudget.Core.Enums;
using CleverBudget.Core.Interfaces;
using CleverBudget.Infrastructure.Data;
using CleverBudget.Infrastructure.Extensions;
using CleverBudget.Infrastructure.Helpers;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

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
        DateTime? endDate = null,
        string? search = null,
        decimal? minAmount = null,
        decimal? maxAmount = null,
        bool includeCategory = false)
    {
        var query = _context.Transactions
            .Include(t => t.Category)
            .Where(t => t.UserId == userId);

        query = ApplyFilters(query, type, categoryId, startDate, endDate, search, minAmount, maxAmount);
        query = ApplySorting(query, paginationParams.SortBy, paginationParams.SortOrder);

        var projectionQuery = query.Select(t => new TransactionProjection
        {
            Id = t.Id,
            Amount = t.Amount,
            Type = t.Type,
            Description = t.Description,
            CategoryId = t.CategoryId,
            CategoryName = t.Category.Name,
            CategoryIcon = t.Category.Icon ?? string.Empty,
            CategoryColor = t.Category.Color ?? string.Empty,
            CategoryKind = t.Category.Kind,
            CategorySegment = t.Category.Segment ?? string.Empty,
            CategoryTags = t.Category.Tags,
            Date = t.Date,
            CreatedAt = t.CreatedAt
        });

        var projectionResult = await projectionQuery.ToPagedResultAsync(paginationParams);
        var mappedItems = projectionResult.Items
            .Select(item => MapProjection(item, includeCategory))
            .ToList();

        return new PagedResult<TransactionResponseDto>(
            mappedItems,
            projectionResult.Page,
            projectionResult.PageSize,
            projectionResult.TotalCount);
    }

    public async Task<IEnumerable<TransactionResponseDto>> GetAllAsync(
        string userId,
        TransactionType? type = null,
        int? categoryId = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? search = null,
        decimal? minAmount = null,
        decimal? maxAmount = null,
        bool includeCategory = false)
    {
        var query = _context.Transactions
            .Include(t => t.Category)
            .Where(t => t.UserId == userId);

        query = ApplyFilters(query, type, categoryId, startDate, endDate, search, minAmount, maxAmount);

        var projections = await query
            .OrderByDescending(t => t.Date)
            .Select(t => new TransactionProjection
            {
                Id = t.Id,
                Amount = t.Amount,
                Type = t.Type,
                Description = t.Description,
                CategoryId = t.CategoryId,
                CategoryName = t.Category.Name,
                CategoryIcon = t.Category.Icon ?? string.Empty,
                CategoryColor = t.Category.Color ?? string.Empty,
                CategoryKind = t.Category.Kind,
                CategorySegment = t.Category.Segment ?? string.Empty,
                CategoryTags = t.Category.Tags,
                Date = t.Date,
                CreatedAt = t.CreatedAt
            })
            .ToListAsync();

        return projections
            .Select(p => MapProjection(p, includeCategory))
            .ToList();
    }

    public async Task<TransactionImportResultDto> ImportFromCsvAsync(string userId, Stream csvStream, TransactionImportOptions options)
    {
        if (csvStream == null || !csvStream.CanRead)
        {
            throw new ArgumentException("O arquivo CSV fornecido é inválido ou não pode ser lido.", nameof(csvStream));
        }

        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        if (csvStream.CanSeek)
        {
            csvStream.Seek(0, SeekOrigin.Begin);
        }

        var result = new TransactionImportResultDto();
        var delimiter = string.IsNullOrWhiteSpace(options.Delimiter) ? "," : options.Delimiter;

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = delimiter,
            HasHeaderRecord = options.HasHeader,
            MissingFieldFound = null,
            BadDataFound = null,
            TrimOptions = TrimOptions.Trim,
            IgnoreBlankLines = true,
            PrepareHeaderForMatch = args => (args.Header ?? string.Empty).Trim().ToLowerInvariant()
        };

        using var reader = new StreamReader(csvStream, leaveOpen: true);
        using var csv = new CsvReader(reader, config);

        var fallbackKind = ParseCategoryKind(options.CategoryFallbackKind);
        var categories = await _context.Categories
            .Where(c => c.UserId == userId)
            .ToListAsync();

    var categoriesById = categories.ToDictionary(c => c.Id);
    var categoriesByName = categories.ToDictionary(c => c.Name, c => c, StringComparer.OrdinalIgnoreCase);

        while (await csv.ReadAsync())
        {
            var rowNumber = csv.Context.Parser?.Row ?? 0;

            TransactionImportCsvRow record;
            try
            {
                record = csv.GetRecord<TransactionImportCsvRow>();
            }
            catch (Exception ex)
            {
                result.Skipped++;
                result.Errors.Add($"Linha {rowNumber}: {ex.Message}");
                continue;
            }

            if (string.IsNullOrWhiteSpace(record.Description))
            {
                result.Skipped++;
                result.Errors.Add($"Linha {rowNumber}: descrição obrigatória não informada.");
                continue;
            }

            var categoryResolution = await ResolveCategoryAsync(
                userId,
                record.Category,
                fallbackKind,
                categoriesById,
                categoriesByName);

            if (!categoryResolution.IsSuccess)
            {
                result.Skipped++;
                result.Errors.Add($"Linha {rowNumber}: {categoryResolution.Error}");
                continue;
            }

            if (!TryParseTransactionType(record.Type, out var transactionType))
            {
                result.Skipped++;
                result.Errors.Add($"Linha {rowNumber}: tipo de transação inválido '{record.Type}'. Use Income/Expense ou 1/2.");
                continue;
            }

            if (record.Amount == 0)
            {
                result.Skipped++;
                result.Errors.Add($"Linha {rowNumber}: valor não pode ser zero.");
                continue;
            }

            var transaction = new Transaction
            {
                UserId = userId,
                Amount = record.Amount,
                Type = transactionType,
                Description = record.Description.Trim(),
                CategoryId = categoryResolution.CategoryId,
                Date = record.Date,
                CreatedAt = DateTime.UtcNow
            };

            if (options.UpsertExisting)
            {
                var existing = await _context.Transactions.FirstOrDefaultAsync(t =>
                    t.UserId == userId &&
                    t.Date == record.Date &&
                    t.Description == transaction.Description &&
                    t.Amount == record.Amount);

                if (existing != null)
                {
                    existing.Type = transactionType;
                    existing.CategoryId = categoryResolution.CategoryId;
                    existing.Date = record.Date;
                    existing.Description = transaction.Description;
                    existing.Amount = record.Amount;
                    result.Imported++;
                    continue;
                }
            }

            await _context.Transactions.AddAsync(transaction);
            result.Imported++;
        }

        await _context.SaveChangesAsync();
        return result;
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
            CreatedAt = transaction.CreatedAt,
            Category = new TransactionCategoryDto
            {
                Id = transaction.CategoryId,
                Name = transaction.Category.Name,
                Icon = transaction.Category.Icon,
                Color = transaction.Category.Color,
                Kind = transaction.Category.Kind,
                Segment = transaction.Category.Segment ?? string.Empty,
                Tags = CategoryTagHelper.Parse(transaction.Category.Tags ?? "[]")
            }
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

    private IQueryable<Transaction> ApplyFilters(
        IQueryable<Transaction> query,
        TransactionType? type,
        int? categoryId,
        DateTime? startDate,
        DateTime? endDate,
        string? search,
        decimal? minAmount,
        decimal? maxAmount)
    {
        if (type.HasValue)
        {
            query = query.Where(t => t.Type == type.Value);
        }

        if (categoryId.HasValue)
        {
            query = query.Where(t => t.CategoryId == categoryId.Value);
        }

        if (startDate.HasValue)
        {
            query = query.Where(t => t.Date >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(t => t.Date <= endDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(t =>
                EF.Functions.Like(t.Description, pattern) ||
                EF.Functions.Like(t.Category.Name, pattern));
        }

        if (minAmount.HasValue)
        {
            query = query.Where(t => t.Amount >= minAmount.Value);
        }

        if (maxAmount.HasValue)
        {
            query = query.Where(t => t.Amount <= maxAmount.Value);
        }

        return query;
    }

    private static TransactionResponseDto MapProjection(TransactionProjection projection, bool includeCategory)
    {
        var response = new TransactionResponseDto
        {
            Id = projection.Id,
            Amount = projection.Amount,
            Type = projection.Type,
            Description = projection.Description,
            CategoryId = projection.CategoryId,
            CategoryName = projection.CategoryName,
            CategoryIcon = projection.CategoryIcon,
            CategoryColor = projection.CategoryColor,
            Date = projection.Date,
            CreatedAt = projection.CreatedAt
        };

        if (includeCategory)
        {
            response.Category = new TransactionCategoryDto
            {
                Id = projection.CategoryId,
                Name = projection.CategoryName,
                Icon = projection.CategoryIcon,
                Color = projection.CategoryColor,
                Kind = projection.CategoryKind,
                Segment = projection.CategorySegment,
                Tags = CategoryTagHelper.Parse(projection.CategoryTags ?? "[]")
            };
        }

        return response;
    }

    private class TransactionProjection
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public TransactionType Type { get; set; }
        public string Description { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string CategoryIcon { get; set; } = string.Empty;
        public string CategoryColor { get; set; } = string.Empty;
        public CategoryKind CategoryKind { get; set; }
        public string CategorySegment { get; set; } = string.Empty;
        public string CategoryTags { get; set; } = "[]";
        public DateTime Date { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    private class TransactionImportCsvRow
    {
        [Name("date")]
        public DateTime Date { get; set; }

        [Name("description")]
        public string Description { get; set; } = string.Empty;

        [Name("amount")]
        public decimal Amount { get; set; }

        [Name("type")]
        public string Type { get; set; } = string.Empty;

        [Name("category")]
        public string Category { get; set; } = string.Empty;
    }

    private static bool TryParseTransactionType(string? raw, out TransactionType type)
    {
        if (!string.IsNullOrWhiteSpace(raw) && Enum.TryParse<TransactionType>(raw, true, out type))
        {
            return true;
        }

        if (int.TryParse(raw, out var numeric) && Enum.IsDefined(typeof(TransactionType), numeric))
        {
            type = (TransactionType)numeric;
            return true;
        }

        type = TransactionType.Expense;
        return false;
    }

    private static CategoryKind ParseCategoryKind(string? raw)
    {
        if (!string.IsNullOrWhiteSpace(raw) && Enum.TryParse<CategoryKind>(raw, true, out var parsed))
        {
            return parsed;
        }

        return CategoryKind.Essential;
    }

    private async Task<CategoryResolution> ResolveCategoryAsync(
        string userId,
        string? rawCategory,
        CategoryKind fallbackKind,
        IDictionary<int, Category> categoriesById,
        IDictionary<string, Category> categoriesByName)
    {
        if (string.IsNullOrWhiteSpace(rawCategory))
        {
            return CategoryResolution.Failure("categoria obrigatória não informada.");
        }

        var token = rawCategory.Trim();

        if (int.TryParse(token, out var parsedId) && categoriesById.TryGetValue(parsedId, out var categoryById))
        {
            return CategoryResolution.Successful(categoryById.Id);
        }

        if (categoriesByName.TryGetValue(token, out var categoryByName))
        {
            return CategoryResolution.Successful(categoryByName.Id);
        }

        var normalizedName = token;
        var newCategory = new Category
        {
            UserId = userId,
            Name = normalizedName,
            Icon = null,
            Color = null,
            Kind = fallbackKind,
            Segment = null,
            Tags = "[]",
            IsDefault = false,
            CreatedAt = DateTime.UtcNow
        };

        await _context.Categories.AddAsync(newCategory);
        await _context.SaveChangesAsync();

        categoriesById[newCategory.Id] = newCategory;
        categoriesByName[newCategory.Name] = newCategory;

        return CategoryResolution.Successful(newCategory.Id);
    }

    private readonly struct CategoryResolution
    {
        private CategoryResolution(bool success, int categoryId, string? error)
        {
            IsSuccess = success;
            CategoryId = categoryId;
            Error = error;
        }

        public bool IsSuccess { get; }
        public int CategoryId { get; }
        public string? Error { get; }

        public static CategoryResolution Successful(int categoryId) => new(true, categoryId, null);
        public static CategoryResolution Failure(string error) => new(false, 0, error);
    }
}