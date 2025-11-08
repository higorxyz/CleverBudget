using CleverBudget.Core.Entities;
using CleverBudget.Core.Enums;
using CleverBudget.Infrastructure.Data;
using CleverBudget.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CleverBudget.Tests.Services;

public class ReportServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly ReportService _reportService;
    private readonly string _testUserId;
    private readonly Category _testCategory;

    public ReportServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);

        _reportService = new ReportService(_context);

        _testUserId = Guid.NewGuid().ToString();
        _testCategory = new Category
        {
            Id = 1,
            UserId = _testUserId,
            Name = "Alimentação",
            Icon = "🍔",
            Color = "#FF6B6B",
            IsDefault = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Categories.Add(_testCategory);
        _context.SaveChanges();
    }

    [Fact]
    public async Task GetSummaryAsync_CalculatesTotalsCorrectly()
    {

        var startDate = DateTime.Now.AddMonths(-1);
        var endDate = DateTime.Now;

        _context.Transactions.AddRange(
            new Transaction { UserId = _testUserId, CategoryId = _testCategory.Id, Amount = 1000m, Type = TransactionType.Income, Description = "Salário", Date = startDate.AddDays(1), CreatedAt = DateTime.UtcNow },
            new Transaction { UserId = _testUserId, CategoryId = _testCategory.Id, Amount = 500m, Type = TransactionType.Income, Description = "Freelance", Date = startDate.AddDays(2), CreatedAt = DateTime.UtcNow },
            new Transaction { UserId = _testUserId, CategoryId = _testCategory.Id, Amount = 300m, Type = TransactionType.Expense, Description = "Mercado", Date = startDate.AddDays(3), CreatedAt = DateTime.UtcNow },
            new Transaction { UserId = _testUserId, CategoryId = _testCategory.Id, Amount = 200m, Type = TransactionType.Expense, Description = "Gasolina", Date = startDate.AddDays(4), CreatedAt = DateTime.UtcNow }
        );
        await _context.SaveChangesAsync();


        var result = await _reportService.GetSummaryAsync(_testUserId, startDate, endDate);


        Assert.Equal(1500m, result.TotalIncome); // 1000 + 500
        Assert.Equal(500m, result.TotalExpenses); // 300 + 200
        Assert.Equal(1000m, result.Balance); // 1500 - 500
        Assert.Equal(4, result.TransactionCount);
    }

    [Fact]
    public async Task GetSummaryAsync_NoTransactions_ReturnsZero()
    {

        var startDate = DateTime.Now.AddMonths(-1);
        var endDate = DateTime.Now;


        var result = await _reportService.GetSummaryAsync(_testUserId, startDate, endDate);


        Assert.Equal(0m, result.TotalIncome);
        Assert.Equal(0m, result.TotalExpenses);
        Assert.Equal(0m, result.Balance);
        Assert.Equal(0, result.TransactionCount);
    }

    [Fact]
    public async Task GetCategoryReportAsync_GroupsByCategory()
    {

        var category2 = new Category { Id = 2, UserId = _testUserId, Name = "Transporte", Icon = "🚗", CreatedAt = DateTime.UtcNow };
        _context.Categories.Add(category2);

        _context.Transactions.AddRange(
            new Transaction { UserId = _testUserId, CategoryId = _testCategory.Id, Amount = 300m, Type = TransactionType.Expense, Description = "Mercado", Date = DateTime.Now, CreatedAt = DateTime.UtcNow },
            new Transaction { UserId = _testUserId, CategoryId = _testCategory.Id, Amount = 200m, Type = TransactionType.Expense, Description = "Restaurante", Date = DateTime.Now, CreatedAt = DateTime.UtcNow },
            new Transaction { UserId = _testUserId, CategoryId = category2.Id, Amount = 150m, Type = TransactionType.Expense, Description = "Uber", Date = DateTime.Now, CreatedAt = DateTime.UtcNow }
        );
        await _context.SaveChangesAsync();


        var result = await _reportService.GetCategoryReportAsync(_testUserId);


        Assert.Equal(2, result.Count());

        var alimentacao = result.First(r => r.CategoryName == "Alimentação");
        Assert.Equal(500m, alimentacao.TotalAmount); // 300 + 200
        Assert.Equal(2, alimentacao.TransactionCount);
        Assert.Equal(76.92m, Math.Round(alimentacao.Percentage, 2)); // 500/650 * 100

        var transporte = result.First(r => r.CategoryName == "Transporte");
        Assert.Equal(150m, transporte.TotalAmount);
        Assert.Equal(1, transporte.TransactionCount);
    }

    [Fact]
    public async Task GetCategoryReportAsync_ExpensesOnly_IgnoresIncome()
    {

        _context.Transactions.AddRange(
            new Transaction { UserId = _testUserId, CategoryId = _testCategory.Id, Amount = 300m, Type = TransactionType.Expense, Description = "Despesa", Date = DateTime.Now, CreatedAt = DateTime.UtcNow },
            new Transaction { UserId = _testUserId, CategoryId = _testCategory.Id, Amount = 1000m, Type = TransactionType.Income, Description = "Receita", Date = DateTime.Now, CreatedAt = DateTime.UtcNow }
        );
        await _context.SaveChangesAsync();


        var result = await _reportService.GetCategoryReportAsync(_testUserId, expensesOnly: true);


        Assert.Single(result);
        Assert.Equal(300m, result.First().TotalAmount); // Apenas despesa
    }

    [Fact]
    public async Task GetCategoryReportAsync_IncludesIncome_WhenExpensesOnlyIsFalse()
    {

        _context.Transactions.AddRange(
            new Transaction { UserId = _testUserId, CategoryId = _testCategory.Id, Amount = 300m, Type = TransactionType.Expense, Description = "Despesa", Date = DateTime.Now, CreatedAt = DateTime.UtcNow },
            new Transaction { UserId = _testUserId, CategoryId = _testCategory.Id, Amount = 1000m, Type = TransactionType.Income, Description = "Receita", Date = DateTime.Now, CreatedAt = DateTime.UtcNow }
        );
        await _context.SaveChangesAsync();


        var result = await _reportService.GetCategoryReportAsync(_testUserId, expensesOnly: false);


        Assert.Single(result);
        Assert.Equal(1300m, result.First().TotalAmount); // Despesa + Receita
        Assert.Equal(2, result.First().TransactionCount);
    }

    [Fact]
    public async Task GetMonthlyReportAsync_ReturnsMonthlyBreakdown()
    {

        var baseDate = DateTime.Now;
        _context.Transactions.AddRange(
            // Mês atual
            new Transaction { UserId = _testUserId, CategoryId = _testCategory.Id, Amount = 500m, Type = TransactionType.Income, Description = "Salário", Date = baseDate, CreatedAt = DateTime.UtcNow },
            new Transaction { UserId = _testUserId, CategoryId = _testCategory.Id, Amount = 200m, Type = TransactionType.Expense, Description = "Mercado", Date = baseDate, CreatedAt = DateTime.UtcNow },
            // Mês passado
            new Transaction { UserId = _testUserId, CategoryId = _testCategory.Id, Amount = 600m, Type = TransactionType.Income, Description = "Salário", Date = baseDate.AddMonths(-1), CreatedAt = DateTime.UtcNow },
            new Transaction { UserId = _testUserId, CategoryId = _testCategory.Id, Amount = 300m, Type = TransactionType.Expense, Description = "Conta", Date = baseDate.AddMonths(-1), CreatedAt = DateTime.UtcNow }
        );
        await _context.SaveChangesAsync();


    var result = await _reportService.GetMonthlyReportAsync(_testUserId, periods: 2);


        Assert.Equal(2, result.Count());

        var thisMonth = result.First(r => r.Month == baseDate.Month);
        Assert.Equal(500m, thisMonth.TotalIncome);
        Assert.Equal(200m, thisMonth.TotalExpenses);
        Assert.Equal(300m, thisMonth.Balance);

        var lastMonth = result.First(r => r.Month == baseDate.AddMonths(-1).Month);
        Assert.Equal(600m, lastMonth.TotalIncome);
        Assert.Equal(300m, lastMonth.TotalExpenses);
        Assert.Equal(300m, lastMonth.Balance);
    }

    [Fact]
    public async Task GetDetailedReportAsync_ReturnsCompleteReport()
    {

        _context.Transactions.AddRange(
            new Transaction { UserId = _testUserId, CategoryId = _testCategory.Id, Amount = 1000m, Type = TransactionType.Income, Description = "Salário", Date = DateTime.Now, CreatedAt = DateTime.UtcNow },
            new Transaction { UserId = _testUserId, CategoryId = _testCategory.Id, Amount = 300m, Type = TransactionType.Expense, Description = "Mercado", Date = DateTime.Now, CreatedAt = DateTime.UtcNow }
        );
        await _context.SaveChangesAsync();


        var result = await _reportService.GetDetailedReportAsync(_testUserId);


        Assert.NotNull(result.Summary);
        Assert.Equal(1000m, result.Summary.TotalIncome);
        Assert.Equal(300m, result.Summary.TotalExpenses);

        Assert.NotEmpty(result.TopExpenseCategories);
        Assert.NotEmpty(result.TopIncomeCategories);
        Assert.NotEmpty(result.MonthlyHistory);
    }

    [Fact]
    public async Task GetSummaryAsync_RespectsDateRange()
    {

        var startDate = new DateTime(2025, 1, 1);
        var endDate = new DateTime(2025, 1, 31);

        _context.Transactions.AddRange(

            new Transaction { UserId = _testUserId, CategoryId = _testCategory.Id, Amount = 500m, Type = TransactionType.Income, Description = "Janeiro", Date = new DateTime(2025, 1, 15), CreatedAt = DateTime.UtcNow },

            new Transaction { UserId = _testUserId, CategoryId = _testCategory.Id, Amount = 1000m, Type = TransactionType.Income, Description = "Fevereiro", Date = new DateTime(2025, 2, 1), CreatedAt = DateTime.UtcNow }
        );
        await _context.SaveChangesAsync();


        var result = await _reportService.GetSummaryAsync(_testUserId, startDate, endDate);


        Assert.Equal(500m, result.TotalIncome); // Apenas janeiro
        Assert.Equal(1, result.TransactionCount);
    }

    [Fact]
    public async Task GetCategoryReportAsync_OrdersByTotalAmountDescending()
    {

        var cat2 = new Category { Id = 2, UserId = _testUserId, Name = "Transporte", CreatedAt = DateTime.UtcNow };
        var cat3 = new Category { Id = 3, UserId = _testUserId, Name = "Lazer", CreatedAt = DateTime.UtcNow };
        _context.Categories.AddRange(cat2, cat3);

        _context.Transactions.AddRange(
            new Transaction { UserId = _testUserId, CategoryId = _testCategory.Id, Amount = 100m, Type = TransactionType.Expense, Description = "A", Date = DateTime.Now, CreatedAt = DateTime.UtcNow },
            new Transaction { UserId = _testUserId, CategoryId = cat2.Id, Amount = 300m, Type = TransactionType.Expense, Description = "B", Date = DateTime.Now, CreatedAt = DateTime.UtcNow },
            new Transaction { UserId = _testUserId, CategoryId = cat3.Id, Amount = 200m, Type = TransactionType.Expense, Description = "C", Date = DateTime.Now, CreatedAt = DateTime.UtcNow }
        );
        await _context.SaveChangesAsync();


        var result = await _reportService.GetCategoryReportAsync(_testUserId);


        var list = result.ToList();
        Assert.Equal(300m, list[0].TotalAmount); // Maior primeiro
        Assert.Equal(200m, list[1].TotalAmount);
        Assert.Equal(100m, list[2].TotalAmount);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
