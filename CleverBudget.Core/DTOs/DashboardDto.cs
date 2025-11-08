using System;
using System.Collections.Generic;

namespace CleverBudget.Core.DTOs;

public class MonthlyTrendPointDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string Label { get; set; } = string.Empty;
    public decimal Income { get; set; }
    public decimal Expenses { get; set; }
    public decimal Net { get; set; }
}

public class DashboardOverviewDto
{
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public BudgetOverviewDto? Budget { get; set; }
    public SummaryReportDto? Summary { get; set; }
    public IReadOnlyCollection<MonthlyTrendPointDto> CashflowTrend { get; set; } = Array.Empty<MonthlyTrendPointDto>();
    public IReadOnlyCollection<BudgetTrendPointDto> BudgetTrend { get; set; } = Array.Empty<BudgetTrendPointDto>();
    public IReadOnlyCollection<FinancialInsightDto> Insights { get; set; } = Array.Empty<FinancialInsightDto>();
}
