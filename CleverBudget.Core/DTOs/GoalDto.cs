using System;
using System.Collections.Generic;
using CleverBudget.Core.Enums;

namespace CleverBudget.Core.DTOs;

public class CreateGoalDto
{
    public int CategoryId { get; set; }
    public decimal TargetAmount { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
}

public class UpdateGoalDto
{
    public decimal? TargetAmount { get; set; }
}

public class GoalResponseDto
{
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string CategoryIcon { get; set; } = string.Empty;
    public string CategoryColor { get; set; } = string.Empty;
    public CategoryKind CategoryKind { get; set; }
    public string CategorySegment { get; set; } = string.Empty;
    public IReadOnlyCollection<string> CategoryTags { get; set; } = Array.Empty<string>();
    public decimal TargetAmount { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class GoalStatusDto
{
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string CategoryIcon { get; set; } = string.Empty;
    public string CategoryColor { get; set; } = string.Empty;
    public CategoryKind CategoryKind { get; set; }
    public decimal TargetAmount { get; set; }
    public decimal CurrentAmount { get; set; }
    public decimal Percentage { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class GoalInsightsFilterDto
{
    public int? Month { get; set; }
    public int? Year { get; set; }
    public int? CategoryId { get; set; }
    public CategoryKind? CategoryKind { get; set; }
    public decimal RiskThresholdPercentage { get; set; } = 80m;
}

public class GoalInsightItemDto
{
    public int GoalId { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string CategoryIcon { get; set; } = string.Empty;
    public string CategoryColor { get; set; } = string.Empty;
    public CategoryKind CategoryKind { get; set; }
    public decimal TargetAmount { get; set; }
    public decimal CurrentAmount { get; set; }
    public decimal Percentage { get; set; }
    public decimal RemainingAmount { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class GoalInsightsSummaryDto
{
    public IReadOnlyCollection<GoalInsightItemDto> Overdue { get; set; } = Array.Empty<GoalInsightItemDto>();
    public IReadOnlyCollection<GoalInsightItemDto> AtRisk { get; set; } = Array.Empty<GoalInsightItemDto>();
    public IReadOnlyCollection<GoalInsightItemDto> Completed { get; set; } = Array.Empty<GoalInsightItemDto>();
    public decimal TotalTargetAmount { get; set; }
    public decimal TotalCurrentAmount { get; set; }
    public int TotalGoals { get; set; }
}