using CleverBudget.Core.DTOs;

namespace CleverBudget.Core.Interfaces;

public interface IRealtimeNotifier
{
    Task NotifyInsightsUpdatedAsync(string userId, IReadOnlyCollection<FinancialInsightDto> insights);
    Task NotifyBudgetOverviewUpdatedAsync(string userId, BudgetOverviewDto overview);
    Task NotifyDashboardOverviewAsync(string userId, DashboardOverviewDto overview);
}
