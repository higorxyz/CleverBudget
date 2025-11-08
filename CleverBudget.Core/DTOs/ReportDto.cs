using System;

namespace CleverBudget.Core.DTOs;

public class SummaryReportDto
{
    public decimal TotalIncome { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal Balance { get; set; }
    public int TransactionCount { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal IncomeChangePercentage { get; set; }
    public decimal ExpenseChangePercentage { get; set; }
    public decimal BalanceChangePercentage { get; set; }
    public decimal AverageDailySpend { get; set; }
    public decimal SavingsRatePercentage { get; set; }
}

public class CategoryReportDto
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string CategoryIcon { get; set; } = string.Empty;
    public string CategoryColor { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public int TransactionCount { get; set; }
    public decimal Percentage { get; set; }
}

public class MonthlyReportDto
{
    public int Month { get; set; }
    public int Year { get; set; }
    public string MonthName { get; set; } = string.Empty;
    public decimal TotalIncome { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal Balance { get; set; }
    public int TransactionCount { get; set; }
    public string GroupLabel { get; set; } = string.Empty;
    public ReportGroupBy GroupBy { get; set; } = ReportGroupBy.Month;
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
}

public class DetailedReportDto
{
    public SummaryReportDto Summary { get; set; } = new();
    public List<CategoryReportDto> TopExpenseCategories { get; set; } = new();
    public List<CategoryReportDto> TopIncomeCategories { get; set; } = new();
    public List<MonthlyReportDto> MonthlyHistory { get; set; } = new();
}

public enum ReportGroupBy
{
    Month,
    Week,
    Year
}