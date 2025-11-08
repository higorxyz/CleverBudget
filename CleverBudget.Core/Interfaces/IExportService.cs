using CleverBudget.Core.DTOs;

namespace CleverBudget.Core.Interfaces;

public interface IExportService
{
    Task<byte[]> ExportTransactionsToCsvAsync(string userId, DateTime? startDate = null, DateTime? endDate = null, ExportRequestOptions? options = null);
    Task<byte[]> ExportCategoriesToCsvAsync(string userId, ExportRequestOptions? options = null);
    Task<byte[]> ExportGoalsToCsvAsync(string userId, int? month = null, int? year = null, ExportRequestOptions? options = null);
    Task<byte[]> ExportBudgetOverviewToCsvAsync(string userId, int? year = null, int? month = null, ExportRequestOptions? options = null);

    Task<byte[]> ExportTransactionsToPdfAsync(string userId, DateTime? startDate = null, DateTime? endDate = null, ExportRequestOptions? options = null);
    Task<byte[]> ExportFinancialReportToPdfAsync(string userId, DateTime? startDate = null, DateTime? endDate = null, ExportRequestOptions? options = null);
    Task<byte[]> ExportGoalsReportToPdfAsync(string userId, int? month = null, int? year = null, ExportRequestOptions? options = null);
    Task<byte[]> ExportBudgetOverviewToPdfAsync(string userId, int? year = null, int? month = null, ExportRequestOptions? options = null);
}