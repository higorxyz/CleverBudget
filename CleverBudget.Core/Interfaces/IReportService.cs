using CleverBudget.Core.DTOs;

namespace CleverBudget.Core.Interfaces;

public interface IReportService
{
    Task<SummaryReportDto> GetSummaryAsync(string userId, DateTime? startDate = null, DateTime? endDate = null);
    Task<IEnumerable<CategoryReportDto>> GetCategoryReportAsync(string userId, DateTime? startDate = null, DateTime? endDate = null, bool expensesOnly = true);
    Task<IEnumerable<MonthlyReportDto>> GetMonthlyReportAsync(string userId, int periods = 12, ReportGroupBy groupBy = ReportGroupBy.Month);
    Task<DetailedReportDto> GetDetailedReportAsync(string userId, DateTime? startDate = null, DateTime? endDate = null);
    Task<DashboardOverviewDto> GetDashboardOverviewAsync(string userId, int? year = null, int? month = null, int budgetTrendMonths = 6, int cashflowMonths = 6);
}