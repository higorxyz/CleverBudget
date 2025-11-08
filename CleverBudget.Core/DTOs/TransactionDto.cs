using System.Collections.Generic;
using CleverBudget.Core.Enums;

namespace CleverBudget.Core.DTOs;

public class CreateTransactionDto
{
    public decimal Amount { get; set; }
    public TransactionType Type { get; set; }
    public string Description { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public DateTime Date { get; set; }
}

public class UpdateTransactionDto
{
    public decimal? Amount { get; set; }
    public TransactionType? Type { get; set; }
    public string? Description { get; set; }
    public int? CategoryId { get; set; }
    public DateTime? Date { get; set; }
}

public class TransactionResponseDto
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public TransactionType Type { get; set; }
    public string Description { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string CategoryIcon { get; set; } = string.Empty;
    public string CategoryColor { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public DateTime CreatedAt { get; set; }
    public TransactionCategoryDto? Category { get; set; }
}

public class TransactionCategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public string? Color { get; set; }
    public CategoryKind Kind { get; set; }
    public string Segment { get; set; } = string.Empty;
    public IReadOnlyCollection<string> Tags { get; set; } = Array.Empty<string>();
}

public class TransactionImportOptions
{
    public bool HasHeader { get; set; } = true;
    public string Delimiter { get; set; } = ",";
    public bool UpsertExisting { get; set; }
    public string CategoryFallbackKind { get; set; } = CategoryKind.Essential.ToString();
}

public class TransactionImportResultDto
{
    public int Imported { get; set; }
    public int Skipped { get; set; }
    public List<string> Errors { get; set; } = new();
}