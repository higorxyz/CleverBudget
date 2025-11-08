using System;
using System.Collections.Generic;
using CleverBudget.Core.Enums;

namespace CleverBudget.Core.DTOs;

/// <summary>
/// DTO para criação de orçamento
/// </summary>
public class CreateBudgetDto
{
    public int CategoryId { get; set; }
    public decimal Amount { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
    public bool AlertAt50Percent { get; set; } = true;
    public bool AlertAt80Percent { get; set; } = true;
    public bool AlertAt100Percent { get; set; } = true;
}

/// <summary>
/// DTO para atualização de orçamento
/// </summary>
public class UpdateBudgetDto
{
    public decimal? Amount { get; set; }
    public bool? AlertAt50Percent { get; set; }
    public bool? AlertAt80Percent { get; set; }
    public bool? AlertAt100Percent { get; set; }
}

/// <summary>
/// DTO de resposta para orçamento
/// </summary>
public class BudgetResponseDto
{
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string CategoryIcon { get; set; } = string.Empty;
    public string CategoryColor { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
    public string MonthName { get; set; } = string.Empty;
    public decimal Spent { get; set; }
    public decimal Remaining { get; set; }
    public decimal PercentageUsed { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool AlertAt50Percent { get; set; }
    public bool AlertAt80Percent { get; set; }
    public bool AlertAt100Percent { get; set; }
    public bool Alert50Sent { get; set; }
    public bool Alert80Sent { get; set; }
    public bool Alert100Sent { get; set; }
    public DateTime CreatedAt { get; set; }
    public decimal HistoricalAverage { get; set; }
    public decimal ProjectedSpend { get; set; }
    public decimal SuggestedBudget { get; set; }
    public decimal BudgetVariance { get; set; }
    public decimal ProjectedVariance { get; set; }
    public decimal DailyBudget { get; set; }
    public decimal BurnRate { get; set; }
    public decimal BurnRateVariance { get; set; }
    public int DaysElapsed { get; set; }
    public int DaysRemaining { get; set; }
    public int TransactionsCount { get; set; }
    public DateTime? LastTransactionDate { get; set; }
    public string Recommendation { get; set; } = string.Empty;
}

public class BudgetCategorySnapshotDto
{
    public int BudgetId { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string CategoryIcon { get; set; } = string.Empty;
    public string CategoryColor { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal Spent { get; set; }
    public decimal Remaining { get; set; }
    public decimal PercentageUsed { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal ProjectedSpend { get; set; }
    public decimal ProjectedVariance { get; set; }
    public decimal SuggestedBudget { get; set; }
    public decimal BudgetVariance { get; set; }
    public decimal PotentialReallocation { get; set; }
    public decimal DailyBudget { get; set; }
    public decimal BurnRate { get; set; }
    public decimal BurnRateVariance { get; set; }
    public int TransactionsCount { get; set; }
    public DateTime? LastTransactionDate { get; set; }
    public string Recommendation { get; set; } = string.Empty;
}

public class BudgetOverviewDto
{
    public int Month { get; set; }
    public int Year { get; set; }
    public decimal TotalBudget { get; set; }
    public decimal TotalSpent { get; set; }
    public decimal Remaining { get; set; }
    public decimal PercentageUsed { get; set; }
    public decimal SuggestedReallocation { get; set; }
    public string Recommendation { get; set; } = string.Empty;
    public IReadOnlyCollection<BudgetCategorySnapshotDto> Categories { get; set; } = Array.Empty<BudgetCategorySnapshotDto>();
    public IReadOnlyCollection<BudgetCategorySnapshotDto> AtRisk { get; set; } = Array.Empty<BudgetCategorySnapshotDto>();
    public IReadOnlyCollection<BudgetCategorySnapshotDto> Comfortable { get; set; } = Array.Empty<BudgetCategorySnapshotDto>();
}

public class BudgetTrendPointDto
{
    public int Month { get; set; }
    public int Year { get; set; }
    public string MonthName { get; set; } = string.Empty;
    public decimal Planned { get; set; }
    public decimal Spent { get; set; }
    public decimal Variance { get; set; }
    public decimal Remaining { get; set; }
    public decimal CoveragePercent { get; set; }
    public int CategoriesTracked { get; set; }
}
