using CleverBudget.Core.Enums;

namespace CleverBudget.Core.Entities;

public class FinancialInsightRecord
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public InsightCategory Category { get; set; }
    public InsightSeverity Severity { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Recommendation { get; set; } = string.Empty;
    public decimal? ImpactAmount { get; set; }
    public decimal? BenchmarkAmount { get; set; }
    public DateTime GeneratedAt { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int? CategoryId { get; set; }
    public bool IncludeIncomeInsights { get; set; }
    public bool IncludeExpenseInsights { get; set; }
    public string DataPointsJson { get; set; } = "[]";

    public User User { get; set; } = null!;
}
