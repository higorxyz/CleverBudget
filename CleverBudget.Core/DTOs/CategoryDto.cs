using System;
using System.Collections.Generic;
using CleverBudget.Core.Enums;

namespace CleverBudget.Core.DTOs;

public class CreateCategoryDto
{
    public string Name { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public string? Color { get; set; }
    public CategoryKind Kind { get; set; } = CategoryKind.Essential;
    public string? Segment { get; set; }
    public IEnumerable<string>? Tags { get; set; }
}

public class UpdateCategoryDto
{
    public string? Name { get; set; }
    public string? Icon { get; set; }
    public string? Color { get; set; }
    public CategoryKind? Kind { get; set; }
    public string? Segment { get; set; }
    public IEnumerable<string>? Tags { get; set; }
}

public class CategoryResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public string? Color { get; set; }
    public bool IsDefault { get; set; }
    public DateTime CreatedAt { get; set; }
    public CategoryKind Kind { get; set; }
    public string Segment { get; set; } = string.Empty;
    public IReadOnlyCollection<string> Tags { get; set; } = Array.Empty<string>();
    public CategoryUsageSummaryDto? Usage { get; set; }
}

public class CategoryUsageSummaryDto
{
    public int TransactionCount { get; set; }
    public decimal TransactionTotal { get; set; }
    public int ActiveGoals { get; set; }
    public DateTime? LastTransactionDate { get; set; }
}

public class CategoryFilterOptions
{
    public string? Search { get; set; }
    public IReadOnlyCollection<CategoryKind>? Kinds { get; set; }
    public IReadOnlyCollection<string>? Segments { get; set; }
    public bool? OnlyWithGoals { get; set; }
    public bool? OnlyWithTransactions { get; set; }
}