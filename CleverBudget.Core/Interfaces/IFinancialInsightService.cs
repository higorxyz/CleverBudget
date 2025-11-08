using CleverBudget.Core.DTOs;

namespace CleverBudget.Core.Interfaces;

public interface IFinancialInsightService
{
    Task<IReadOnlyList<FinancialInsightDto>> GenerateInsightsAsync(string userId, FinancialInsightFilter filter, CancellationToken cancellationToken = default);
}
