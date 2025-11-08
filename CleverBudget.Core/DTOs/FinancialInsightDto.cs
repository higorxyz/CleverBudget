using CleverBudget.Core.Enums;

namespace CleverBudget.Core.DTOs;

public class FinancialInsightDto
{
    public InsightCategory Category { get; set; }
    public InsightSeverity Severity { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Recommendation { get; set; } = string.Empty;
    public decimal? ImpactAmount { get; set; }
    public decimal? BenchmarkAmount { get; set; }
    public DateTime GeneratedAt { get; set; }
    public IReadOnlyCollection<InsightDataPointDto> DataPoints { get; set; } = Array.Empty<InsightDataPointDto>();
}

public class InsightDataPointDto
{
    public string Label { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public decimal? Benchmark { get; set; }
    public DateTime? Period { get; set; }
}

public class FinancialInsightFilter
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int? CategoryId { get; set; }
    public bool IncludeIncomeInsights { get; set; } = true;
    public bool IncludeExpenseInsights { get; set; } = true;
}
