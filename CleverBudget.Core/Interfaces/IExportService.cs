namespace CleverBudget.Core.Interfaces;

public interface IExportService
{
    Task<byte[]> ExportTransactionsToCsvAsync(string userId, DateTime? startDate = null, DateTime? endDate = null);
    Task<byte[]> ExportCategoriesToCsvAsync(string userId);
    Task<byte[]> ExportGoalsToCsvAsync(string userId, int? month = null, int? year = null);
    
    Task<byte[]> ExportTransactionsToPdfAsync(string userId, DateTime? startDate = null, DateTime? endDate = null);
    Task<byte[]> ExportFinancialReportToPdfAsync(string userId, DateTime? startDate = null, DateTime? endDate = null);
    Task<byte[]> ExportGoalsReportToPdfAsync(string userId, int? month = null, int? year = null);
}