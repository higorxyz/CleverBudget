using CleverBudget.Core.DTOs;

namespace CleverBudget.Core.Interfaces;

public interface IReportService
{
    Task<SummaryReportDto> GetSummaryAsync(string userId, DateTime? startDate = null, DateTime? endDate = null);
    Task<IEnumerable<CategoryReportDto>> GetCategoryReportAsync(string userId, DateTime? startDate = null, DateTime? endDate = null, bool expensesOnly = true);
    Task<IEnumerable<MonthlyReportDto>> GetMonthlyReportAsync(string userId, int months = 12);
    Task<DetailedReportDto> GetDetailedReportAsync(string userId, DateTime? startDate = null, DateTime? endDate = null);
}