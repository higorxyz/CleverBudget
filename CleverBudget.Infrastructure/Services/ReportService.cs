using CleverBudget.Core.DTOs;
using CleverBudget.Core.Enums;
using CleverBudget.Core.Interfaces;
using CleverBudget.Infrastructure.Data;
using CleverBudget.Infrastructure.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using System.Globalization;
using System.Linq;

namespace CleverBudget.Infrastructure.Services;

public class ReportService : IReportService
{
    private readonly AppDbContext _context;
    private readonly IBudgetService _budgetService;
    private readonly IFinancialInsightService _financialInsightService;
    private readonly IRealtimeNotifier _realtimeNotifier;
    private static readonly CultureInfo PtCulture = CultureInfo.GetCultureInfo("pt-BR");

    public ReportService(AppDbContext context)
        : this(
            context,
            new BudgetService(context),
            new FinancialInsightService(context, NullLogger<FinancialInsightService>.Instance),
            NullRealtimeNotifier.Instance)
    {
    }

    public ReportService(
        AppDbContext context,
        IBudgetService budgetService,
        IFinancialInsightService financialInsightService,
        IRealtimeNotifier realtimeNotifier)
    {
        _context = context;
        _budgetService = budgetService;
        _financialInsightService = financialInsightService;
        _realtimeNotifier = realtimeNotifier ?? NullRealtimeNotifier.Instance;
    }

    public async Task<SummaryReportDto> GetSummaryAsync(string userId, DateTime? startDate = null, DateTime? endDate = null)
    {
        var end = endDate ?? DateTime.UtcNow;
        var start = startDate ?? end.AddMonths(-1);

        if (start > end)
        {
            (start, end) = (end, start);
        }

        var periodDuration = end - start;
        if (periodDuration.TotalDays <= 0)
        {
            end = start.AddDays(1);
            periodDuration = end - start;
        }

        var previousStart = start - periodDuration;
        var previousEnd = start;

        var transactions = await _context.Transactions
            .Where(t => t.UserId == userId && t.Date >= previousStart && t.Date <= end)
            .Select(t => new { t.Type, t.Amount, t.Date })
            .ToListAsync();

        var currentTransactions = transactions.Where(t => t.Date >= start).ToList();
        var previousTransactions = transactions.Where(t => t.Date < start).ToList();

        var totalIncome = currentTransactions
            .Where(t => t.Type == TransactionType.Income)
            .Sum(t => t.Amount);

        var totalExpenses = currentTransactions
            .Where(t => t.Type == TransactionType.Expense)
            .Sum(t => t.Amount);

        var previousIncome = previousTransactions
            .Where(t => t.Type == TransactionType.Income)
            .Sum(t => t.Amount);

        var previousExpenses = previousTransactions
            .Where(t => t.Type == TransactionType.Expense)
            .Sum(t => t.Amount);

        var balance = totalIncome - totalExpenses;
        var previousBalance = previousIncome - previousExpenses;

        var averageDailySpend = totalExpenses / (decimal)Math.Max(1, periodDuration.TotalDays);
        var savingsRate = totalIncome > 0 ? Math.Round((balance / totalIncome) * 100, 2) : 0;

        return new SummaryReportDto
        {
            TotalIncome = totalIncome,
            TotalExpenses = totalExpenses,
            Balance = balance,
            TransactionCount = currentTransactions.Count,
            StartDate = start,
            EndDate = end,
            IncomeChangePercentage = ComputePercentageChange(totalIncome, previousIncome),
            ExpenseChangePercentage = ComputePercentageChange(totalExpenses, previousExpenses),
            BalanceChangePercentage = ComputePercentageChange(balance, previousBalance),
            AverageDailySpend = Math.Round(averageDailySpend, 2),
            SavingsRatePercentage = savingsRate
        };
    }

    public async Task<IEnumerable<CategoryReportDto>> GetCategoryReportAsync(
        string userId, 
        DateTime? startDate = null, 
        DateTime? endDate = null, 
        bool expensesOnly = true)
    {
        var start = startDate ?? DateTime.Now.AddMonths(-1);
        var end = endDate ?? DateTime.Now;

        var query = _context.Transactions
            .Include(t => t.Category)
            .Where(t => t.UserId == userId && t.Date >= start && t.Date <= end);

        if (expensesOnly)
            query = query.Where(t => t.Type == TransactionType.Expense);

        var transactions = await query.ToListAsync();

        var totalAmount = transactions.Sum(t => t.Amount);

        var categoryReport = transactions
            .GroupBy(t => new 
            { 
                t.CategoryId, 
                t.Category.Name, 
                t.Category.Icon, 
                t.Category.Color 
            })
            .Select(g => new CategoryReportDto
            {
                CategoryId = g.Key.CategoryId,
                CategoryName = g.Key.Name,
                CategoryIcon = g.Key.Icon ?? "",
                CategoryColor = g.Key.Color ?? "",
                TotalAmount = g.Sum(t => t.Amount),
                TransactionCount = g.Count(),
                Percentage = totalAmount > 0 ? Math.Round((g.Sum(t => t.Amount) / totalAmount) * 100, 2) : 0
            })
            .OrderByDescending(c => c.TotalAmount)
            .ToList();

        return categoryReport;
    }

    public async Task<IEnumerable<MonthlyReportDto>> GetMonthlyReportAsync(string userId, int periods = 12, ReportGroupBy groupBy = ReportGroupBy.Month)
    {
        periods = Math.Clamp(periods, 1, groupBy == ReportGroupBy.Week ? 52 : 36);
        var now = DateTime.UtcNow;
        var periodStartBoundary = AlignToPeriodStart(now, groupBy).AddDays(groupBy == ReportGroupBy.Week ? -7 * (periods - 1) : 0);

        if (groupBy == ReportGroupBy.Month)
        {
            periodStartBoundary = new DateTime(now.Year, now.Month, 1).AddMonths(-(periods - 1));
        }
        else if (groupBy == ReportGroupBy.Year)
        {
            periodStartBoundary = new DateTime(now.Year - (periods - 1), 1, 1);
        }

        var transactions = await _context.Transactions
            .Where(t => t.UserId == userId && t.Date >= periodStartBoundary && t.Date <= now)
            .Select(t => new { t.Date, t.Amount, t.Type })
            .ToListAsync();

        var grouped = transactions
            .Select(t => new
            {
                Key = CreateGroupKey(t.Date, groupBy),
                t.Type,
                t.Amount
            })
            .GroupBy(t => t.Key)
            .Select(g => new
            {
                g.Key,
                Income = g.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount),
                Expenses = g.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount),
                Count = g.Count()
            })
            .OrderByDescending(x => x.Key.Start)
            .Take(periods)
            .OrderBy(x => x.Key.Start)
            .ToList();

        return grouped.Select(g => new MonthlyReportDto
        {
            Month = g.Key.Start.Month,
            Year = g.Key.Year,
            MonthName = PtCulture.DateTimeFormat.GetMonthName(g.Key.Start.Month),
            TotalIncome = Math.Round(g.Income, 2),
            TotalExpenses = Math.Round(g.Expenses, 2),
            Balance = Math.Round(g.Income - g.Expenses, 2),
            TransactionCount = g.Count,
            GroupLabel = g.Key.Label,
            GroupBy = g.Key.GroupBy,
            PeriodStart = g.Key.Start,
            PeriodEnd = g.Key.End
        }).ToList();
    }

    public async Task<DetailedReportDto> GetDetailedReportAsync(string userId, DateTime? startDate = null, DateTime? endDate = null)
    {
        var summary = await GetSummaryAsync(userId, startDate, endDate);
        var expenseCategories = await GetCategoryReportAsync(userId, startDate, endDate, expensesOnly: true);
        var incomeCategories = await GetCategoryReportAsync(userId, startDate, endDate, expensesOnly: false);
    var monthlyHistory = await GetMonthlyReportAsync(userId, 6, ReportGroupBy.Month);

        return new DetailedReportDto
        {
            Summary = summary,
            TopExpenseCategories = expenseCategories.Take(5).ToList(),
            TopIncomeCategories = incomeCategories.Where(c => c.TotalAmount > 0).Take(5).ToList(),
            MonthlyHistory = monthlyHistory.ToList()
        };
    }

    public async Task<DashboardOverviewDto> GetDashboardOverviewAsync(
        string userId,
        int? year = null,
        int? month = null,
        int budgetTrendMonths = 6,
        int cashflowMonths = 6)
    {
        var now = DateTime.UtcNow;
        var targetYear = year ?? now.Year;
        var targetMonth = month ?? now.Month;

        var periodStart = new DateTime(targetYear, targetMonth, 1, 0, 0, 0, DateTimeKind.Utc);
        var periodEnd = periodStart.AddMonths(1).AddTicks(-1);

        var summary = await GetSummaryAsync(userId, periodStart, periodEnd);
        var budgetOverview = await _budgetService.GetOverviewAsync(userId, targetYear, targetMonth);

        var insights = await _financialInsightService.GenerateInsightsAsync(
            userId,
            new FinancialInsightFilter
            {
                StartDate = periodStart,
                EndDate = periodEnd,
                IncludeExpenseInsights = true,
                IncludeIncomeInsights = true
            });

    var cashflowHistory = await GetMonthlyReportAsync(userId, Math.Clamp(cashflowMonths, 1, 24), ReportGroupBy.Month);
        var cashflowTrend = cashflowHistory
            .OrderBy(m => new DateTime(m.Year, m.Month, 1))
            .Select(m => new MonthlyTrendPointDto
            {
                Year = m.Year,
                Month = m.Month,
                Label = $"{PtCulture.DateTimeFormat.GetAbbreviatedMonthName(m.Month)}/{m.Year}",
                Income = Math.Round(m.TotalIncome, 2),
                Expenses = Math.Round(m.TotalExpenses, 2),
                Net = Math.Round(m.Balance, 2)
            })
            .ToList();

        var budgetTrend = await _budgetService.GetTrendAsync(userId, Math.Clamp(budgetTrendMonths, 1, 24));

        var dashboard = new DashboardOverviewDto
        {
            GeneratedAt = DateTime.UtcNow,
            Summary = summary,
            Budget = budgetOverview,
            Insights = insights.Take(6).ToList(),
            CashflowTrend = cashflowTrend,
            BudgetTrend = budgetTrend
        };

        await _realtimeNotifier.NotifyDashboardOverviewAsync(userId, dashboard);

        return dashboard;
    }

    private static decimal ComputePercentageChange(decimal current, decimal previous)
    {
        if (previous == 0)
        {
            return current == 0 ? 0 : 100;
        }

        return Math.Round(((current - previous) / Math.Abs(previous)) * 100, 2);
    }

    private static DateTime AlignToPeriodStart(DateTime date, ReportGroupBy groupBy)
    {
        var normalized = date.Date;

        return groupBy switch
        {
            ReportGroupBy.Week => StartOfWeek(normalized, DayOfWeek.Monday),
            ReportGroupBy.Year => new DateTime(normalized.Year, 1, 1),
            _ => new DateTime(normalized.Year, normalized.Month, 1)
        };
    }

    private static TimeGroupKey CreateGroupKey(DateTime date, ReportGroupBy groupBy)
    {
        var start = AlignToPeriodStart(date, groupBy);

        return groupBy switch
        {
            ReportGroupBy.Week =>
                CreateWeekGroupKey(date, start),
            ReportGroupBy.Year =>
                CreateYearGroupKey(start),
            _ =>
                CreateMonthGroupKey(start)
        };
    }

    private static TimeGroupKey CreateWeekGroupKey(DateTime originalDate, DateTime start)
    {
        var calendar = PtCulture.Calendar;
        var week = calendar.GetWeekOfYear(originalDate, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
        var year = calendar.GetYear(originalDate);
        var end = start.AddDays(7).AddTicks(-1);
        var label = $"Semana {week}/{year}";

        return new TimeGroupKey(start, end, year, week, label, ReportGroupBy.Week);
    }

    private static TimeGroupKey CreateYearGroupKey(DateTime start)
    {
        var end = new DateTime(start.Year, 12, 31, 23, 59, 59, DateTimeKind.Unspecified);
        var label = start.Year.ToString();
        return new TimeGroupKey(start, end, start.Year, start.Year, label, ReportGroupBy.Year);
    }

    private static TimeGroupKey CreateMonthGroupKey(DateTime start)
    {
        var end = start.AddMonths(1).AddTicks(-1);
        var label = $"{PtCulture.DateTimeFormat.GetAbbreviatedMonthName(start.Month)}/{start.Year}";
        return new TimeGroupKey(start, end, start.Year, start.Month, label, ReportGroupBy.Month);
    }

    private static DateTime StartOfWeek(DateTime date, DayOfWeek startOfWeek)
    {
        var diff = (7 + (date.DayOfWeek - startOfWeek)) % 7;
        return date.AddDays(-diff);
    }

    private readonly record struct TimeGroupKey(DateTime Start, DateTime End, int Year, int Number, string Label, ReportGroupBy GroupBy);
}