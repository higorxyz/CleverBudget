using CleverBudget.Core.DTOs;
using CleverBudget.Core.Interfaces;

namespace CleverBudget.Infrastructure.Notifications;

public sealed class NullRealtimeNotifier : IRealtimeNotifier
{
    public static readonly NullRealtimeNotifier Instance = new();

    private NullRealtimeNotifier()
    {
    }

    public Task NotifyInsightsUpdatedAsync(string userId, IReadOnlyCollection<FinancialInsightDto> insights)
    {
        return Task.CompletedTask;
    }

    public Task NotifyBudgetOverviewUpdatedAsync(string userId, BudgetOverviewDto overview)
    {
        return Task.CompletedTask;
    }

    public Task NotifyDashboardOverviewAsync(string userId, DashboardOverviewDto overview)
    {
        return Task.CompletedTask;
    }
}
