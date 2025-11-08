using CleverBudget.Core.DTOs;
using CleverBudget.Core.Entities;
using CleverBudget.Core.Enums;
using CleverBudget.Infrastructure.Data;
using CleverBudget.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CleverBudget.Tests.Services;

public class FinancialInsightServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly FinancialInsightService _service;
    private readonly string _userId = Guid.NewGuid().ToString();
    private readonly Category _foodCategory;
    private readonly Category _salaryCategory;

    public FinancialInsightServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _service = new FinancialInsightService(_context, NullLogger<FinancialInsightService>.Instance);

        _foodCategory = new Category
        {
            Id = 1,
            Name = "Restaurantes",
            Icon = "🍽",
            Color = "#FFAA00",
            UserId = _userId,
            IsDefault = false,
            CreatedAt = DateTime.UtcNow
        };

        _salaryCategory = new Category
        {
            Id = 2,
            Name = "Salário",
            Icon = "💼",
            Color = "#00AAFF",
            UserId = _userId,
            IsDefault = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.Categories.AddRange(_foodCategory, _salaryCategory);
        _context.SaveChanges();
    }

    [Fact]
    public async Task GenerateInsightsAsync_CategoryOverspend_ReturnsInsight()
    {
        SeedExpenseHistory();

        var filter = new FinancialInsightFilter();

        var insights = await _service.GenerateInsightsAsync(_userId, filter, CancellationToken.None);

        Assert.Contains(insights, i =>
            i.Category == InsightCategory.SpendingPattern &&
            i.Title.Contains("Gastos elevados", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GenerateInsightsAsync_BudgetRisk_ReturnsInsight()
    {
        SeedExpenseHistory();
        SeedBudget();

        var filter = new FinancialInsightFilter();

        var insights = await _service.GenerateInsightsAsync(_userId, filter, CancellationToken.None);

        Assert.Contains(insights, i =>
            i.Category == InsightCategory.BudgetRisk &&
            i.Title.Contains("Orçamento", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GenerateInsightsAsync_IncomeDrop_ReturnsInsight()
    {
        SeedIncomeHistory(dropCurrentMonth: true);

        var filter = new FinancialInsightFilter();

        var insights = await _service.GenerateInsightsAsync(_userId, filter, CancellationToken.None);

        Assert.Contains(insights, i =>
            i.Category == InsightCategory.IncomePattern &&
            i.Title.Contains("Queda", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GenerateInsightsAsync_ExcludeExpenseInsights_SkipsSpending()
    {
        SeedExpenseHistory();

        var filter = new FinancialInsightFilter
        {
            IncludeExpenseInsights = false
        };

        var insights = await _service.GenerateInsightsAsync(_userId, filter, CancellationToken.None);

        Assert.DoesNotContain(insights, i => i.Category == InsightCategory.SpendingPattern);
    }

    private void SeedExpenseHistory()
    {
        var today = DateTime.UtcNow;
        var currentMonthStart = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        // Histórico de 4 meses com gasto moderado (R$ 120)
        for (var monthOffset = 4; monthOffset >= 1; monthOffset--)
        {
            var date = currentMonthStart.AddMonths(-monthOffset).AddDays(5);
            _context.Transactions.Add(new Transaction
            {
                UserId = _userId,
                CategoryId = _foodCategory.Id,
                Category = _foodCategory,
                Amount = 120m,
                Type = TransactionType.Expense,
                Description = $"Jantar histórico {monthOffset}",
                Date = date,
                CreatedAt = date
            });
        }

        // Mês atual com gasto elevado (R$ 320)
        _context.Transactions.Add(new Transaction
        {
            UserId = _userId,
            CategoryId = _foodCategory.Id,
            Category = _foodCategory,
            Amount = 320m,
            Type = TransactionType.Expense,
            Description = "Jantar especial",
            Date = currentMonthStart.AddDays(3),
            CreatedAt = currentMonthStart.AddDays(3)
        });

        _context.SaveChanges();
    }

    private void SeedBudget()
    {
        var today = DateTime.UtcNow;
        _context.Budgets.Add(new Budget
        {
            UserId = _userId,
            CategoryId = _foodCategory.Id,
            Category = _foodCategory,
            Amount = 400m,
            Month = today.Month,
            Year = today.Year,
            CreatedAt = DateTime.UtcNow
        });

        _context.SaveChanges();
    }

    private void SeedIncomeHistory(bool dropCurrentMonth)
    {
        var today = DateTime.UtcNow;
        var currentMonthStart = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        // Receita histórica consistente de R$ 5.000
        for (var monthOffset = 6; monthOffset >= 1; monthOffset--)
        {
            var date = currentMonthStart.AddMonths(-monthOffset).AddDays(2);
            _context.Transactions.Add(new Transaction
            {
                UserId = _userId,
                CategoryId = _salaryCategory.Id,
                Category = _salaryCategory,
                Amount = 5000m,
                Type = TransactionType.Income,
                Description = $"Salário mês {monthOffset}",
                Date = date,
                CreatedAt = date
            });
        }

        var currentAmount = dropCurrentMonth ? 2500m : 6000m;
        _context.Transactions.Add(new Transaction
        {
            UserId = _userId,
            CategoryId = _salaryCategory.Id,
            Category = _salaryCategory,
            Amount = currentAmount,
            Type = TransactionType.Income,
            Description = "Salário mês atual",
            Date = currentMonthStart.AddDays(2),
            CreatedAt = currentMonthStart.AddDays(2)
        });

        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
