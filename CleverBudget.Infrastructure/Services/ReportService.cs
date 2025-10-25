using CleverBudget.Core.DTOs;
using CleverBudget.Core.Enums;
using CleverBudget.Core.Interfaces;
using CleverBudget.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace CleverBudget.Infrastructure.Services;

public class ReportService : IReportService
{
    private readonly AppDbContext _context;

    public ReportService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<SummaryReportDto> GetSummaryAsync(string userId, DateTime? startDate = null, DateTime? endDate = null)
    {
        var start = startDate ?? DateTime.Now.AddMonths(-1);
        var end = endDate ?? DateTime.Now;

        var transactions = await _context.Transactions
            .Where(t => t.UserId == userId && t.Date >= start && t.Date <= end)
            .ToListAsync();

        var totalIncome = transactions
            .Where(t => t.Type == TransactionType.Income)
            .Sum(t => t.Amount);

        var totalExpenses = transactions
            .Where(t => t.Type == TransactionType.Expense)
            .Sum(t => t.Amount);

        return new SummaryReportDto
        {
            TotalIncome = totalIncome,
            TotalExpenses = totalExpenses,
            Balance = totalIncome - totalExpenses,
            TransactionCount = transactions.Count,
            StartDate = start,
            EndDate = end
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

    public async Task<IEnumerable<MonthlyReportDto>> GetMonthlyReportAsync(string userId, int months = 12)
    {
        var endDate = DateTime.Now;
        var startDate = endDate.AddMonths(-months);

        var transactions = await _context.Transactions
            .Where(t => t.UserId == userId && t.Date >= startDate && t.Date <= endDate)
            .ToListAsync();

        var monthlyReport = new List<MonthlyReportDto>();

        for (int i = 0; i < months; i++)
        {
            var monthDate = endDate.AddMonths(-i);
            var monthStart = new DateTime(monthDate.Year, monthDate.Month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);

            var monthTransactions = transactions
                .Where(t => t.Date >= monthStart && t.Date <= monthEnd)
                .ToList();

            var totalIncome = monthTransactions
                .Where(t => t.Type == TransactionType.Income)
                .Sum(t => t.Amount);

            var totalExpenses = monthTransactions
                .Where(t => t.Type == TransactionType.Expense)
                .Sum(t => t.Amount);

            monthlyReport.Add(new MonthlyReportDto
            {
                Month = monthDate.Month,
                Year = monthDate.Year,
                MonthName = CultureInfo.GetCultureInfo("pt-BR").DateTimeFormat.GetMonthName(monthDate.Month),
                TotalIncome = totalIncome,
                TotalExpenses = totalExpenses,
                Balance = totalIncome - totalExpenses,
                TransactionCount = monthTransactions.Count
            });
        }

        return monthlyReport.OrderBy(m => m.Year).ThenBy(m => m.Month);
    }

    public async Task<DetailedReportDto> GetDetailedReportAsync(string userId, DateTime? startDate = null, DateTime? endDate = null)
    {
        var summary = await GetSummaryAsync(userId, startDate, endDate);
        var expenseCategories = await GetCategoryReportAsync(userId, startDate, endDate, expensesOnly: true);
        var incomeCategories = await GetCategoryReportAsync(userId, startDate, endDate, expensesOnly: false);
        var monthlyHistory = await GetMonthlyReportAsync(userId, 6);

        return new DetailedReportDto
        {
            Summary = summary,
            TopExpenseCategories = expenseCategories.Take(5).ToList(),
            TopIncomeCategories = incomeCategories.Where(c => c.TotalAmount > 0).Take(5).ToList(),
            MonthlyHistory = monthlyHistory.ToList()
        };
    }
}