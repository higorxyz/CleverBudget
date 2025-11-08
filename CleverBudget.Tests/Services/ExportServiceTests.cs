using CleverBudget.Core.Entities;
using CleverBudget.Core.Enums;
using CleverBudget.Core.Interfaces;
using CleverBudget.Infrastructure.Data;
using CleverBudget.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;
using Moq;
using System.Text;
using Xunit;

namespace CleverBudget.Tests.Services;

public class ExportServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly ExportService _exportService;
    private readonly Mock<IBudgetService> _budgetServiceMock;
    private readonly string _testUserId;
    private readonly Category _testCategory;

    public ExportServiceTests()
    {
        // Configurar licença do QuestPDF para testes
        QuestPDF.Settings.License = LicenseType.Community;

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);

        _budgetServiceMock = new Mock<IBudgetService>();
        _exportService = new ExportService(_context, _budgetServiceMock.Object);

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

    #region CSV Tests

    [Fact]
    public async Task ExportTransactionsToCsvAsync_ReturnsValidCsv()
    {
        _context.Transactions.Add(new Transaction
        {
            UserId = _testUserId,
            CategoryId = _testCategory.Id,
            Amount = 150.50m,
            Type = TransactionType.Expense,
            Description = "Almoço",
            Date = DateTime.Now.AddDays(-1),
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var csv = await _exportService.ExportTransactionsToCsvAsync(_testUserId);

        Assert.NotNull(csv);
        Assert.True(csv.Length > 0);

        var csvContent = Encoding.UTF8.GetString(csv);
        Assert.Contains("Data", csvContent); // Header
        Assert.Contains("Alimentação", csvContent); // Categoria
        Assert.Contains("150,5", csvContent); // Valor (formato pt-BR)
    }

    [Fact]
    public async Task ExportTransactionsToCsvAsync_NoTransactions_ReturnsHeaderOnly()
    {
        var csv = await _exportService.ExportTransactionsToCsvAsync(_testUserId);

        Assert.NotNull(csv);
        var csvContent = Encoding.UTF8.GetString(csv);
        Assert.Contains("Data", csvContent); // Header existe
        Assert.DoesNotContain("Alimentação", csvContent); // Sem dados
    }

    [Fact]
    public async Task ExportTransactionsToCsvAsync_WithDateRange_RespectsFilter()
    {
        var startDate = new DateTime(2025, 1, 1);
        var endDate = new DateTime(2025, 1, 31);

        _context.Transactions.AddRange(
            new Transaction
            {
                UserId = _testUserId,
                CategoryId = _testCategory.Id,
                Amount = 100m,
                Type = TransactionType.Expense,
                Description = "Janeiro",
                Date = new DateTime(2025, 1, 15),
                CreatedAt = DateTime.UtcNow
            },
            new Transaction
            {
                UserId = _testUserId,
                CategoryId = _testCategory.Id,
                Amount = 200m,
                Type = TransactionType.Expense,
                Description = "Fevereiro",
                Date = new DateTime(2025, 2, 1),
                CreatedAt = DateTime.UtcNow
            }
        );
        await _context.SaveChangesAsync();

        var csv = await _exportService.ExportTransactionsToCsvAsync(_testUserId, startDate, endDate);

        var csvContent = Encoding.UTF8.GetString(csv);
        Assert.Contains("Janeiro", csvContent);
        Assert.DoesNotContain("Fevereiro", csvContent);
    }

    [Fact]
    public async Task ExportCategoriesToCsvAsync_ReturnsValidCsv()
    {
        _context.Categories.Add(new Category
        {
            UserId = _testUserId,
            Name = "Transporte",
            Icon = "🚗",
            Color = "#3498db",
            IsDefault = false,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var csv = await _exportService.ExportCategoriesToCsvAsync(_testUserId);

        Assert.NotNull(csv);
        var csvContent = Encoding.UTF8.GetString(csv);
        Assert.Contains("Nome", csvContent);
        Assert.Contains("Alimentação", csvContent);
        Assert.Contains("Transporte", csvContent);
        Assert.Contains("🚗", csvContent);
    }

    [Fact]
    public async Task ExportGoalsToCsvAsync_ReturnsValidCsv()
    {
        _context.Goals.Add(new Goal
        {
            UserId = _testUserId,
            CategoryId = _testCategory.Id,
            TargetAmount = 1000m,
            Month = 1,
            Year = 2025,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var csv = await _exportService.ExportGoalsToCsvAsync(_testUserId);

        Assert.NotNull(csv);
        var csvContent = Encoding.UTF8.GetString(csv);
    Assert.Contains("Mes", csvContent);
    Assert.Contains("Ano", csvContent);
    Assert.Contains("Alimentação", csvContent);
    Assert.Contains("R$ 1.000,00", csvContent);
    }

    [Fact]
    public async Task ExportGoalsToCsvAsync_WithFilters_ReturnsFilteredData()
    {
        _context.Goals.AddRange(
            new Goal { UserId = _testUserId, CategoryId = _testCategory.Id, TargetAmount = 500m, Month = 1, Year = 2025, CreatedAt = DateTime.UtcNow },
            new Goal { UserId = _testUserId, CategoryId = _testCategory.Id, TargetAmount = 600m, Month = 2, Year = 2025, CreatedAt = DateTime.UtcNow }
        );
        await _context.SaveChangesAsync();

        var csv = await _exportService.ExportGoalsToCsvAsync(_testUserId, month: 1, year: 2025);

        var csvContent = Encoding.UTF8.GetString(csv);
        Assert.Contains("500", csvContent);
        Assert.DoesNotContain("600", csvContent);
    }

    #endregion

    #region PDF Tests

    [Fact]
    public async Task ExportTransactionsToPdfAsync_ReturnsValidPdf()
    {
        _context.Transactions.Add(new Transaction
        {
            UserId = _testUserId,
            CategoryId = _testCategory.Id,
            Amount = 250m,
            Type = TransactionType.Income,
            Description = "Salário",
            Date = DateTime.Now,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var pdf = await _exportService.ExportTransactionsToPdfAsync(_testUserId);

        Assert.NotNull(pdf);
        Assert.True(pdf.Length > 0);
        
        // PDF deve começar com %PDF
        var header = Encoding.ASCII.GetString(pdf.Take(4).ToArray());
        Assert.Equal("%PDF", header);
    }

    [Fact]
    public async Task ExportTransactionsToPdfAsync_NoTransactions_ReturnsEmptyReportPdf()
    {
        var pdf = await _exportService.ExportTransactionsToPdfAsync(_testUserId);

        Assert.NotNull(pdf);
        Assert.True(pdf.Length > 0); // Deve gerar PDF mesmo sem dados
        
        var header = Encoding.ASCII.GetString(pdf.Take(4).ToArray());
        Assert.Equal("%PDF", header);
    }

    [Fact]
    public async Task ExportFinancialReportToPdfAsync_ReturnsValidPdf()
    {
        _context.Transactions.AddRange(
            new Transaction { UserId = _testUserId, CategoryId = _testCategory.Id, Amount = 1000m, Type = TransactionType.Income, Description = "Receita", Date = DateTime.Now, CreatedAt = DateTime.UtcNow },
            new Transaction { UserId = _testUserId, CategoryId = _testCategory.Id, Amount = 300m, Type = TransactionType.Expense, Description = "Despesa", Date = DateTime.Now, CreatedAt = DateTime.UtcNow }
        );
        await _context.SaveChangesAsync();

        var pdf = await _exportService.ExportFinancialReportToPdfAsync(_testUserId);

        Assert.NotNull(pdf);
        Assert.True(pdf.Length > 0);
        
        var header = Encoding.ASCII.GetString(pdf.Take(4).ToArray());
        Assert.Equal("%PDF", header);
    }

    [Fact]
    public async Task ExportGoalsReportToPdfAsync_ReturnsValidPdf()
    {
        var month = DateTime.Now.Month;
        var year = DateTime.Now.Year;

        _context.Goals.Add(new Goal
        {
            UserId = _testUserId,
            CategoryId = _testCategory.Id,
            TargetAmount = 1000m,
            Month = month,
            Year = year,
            CreatedAt = DateTime.UtcNow
        });

        var startDate = new DateTime(year, month, 1);
        _context.Transactions.Add(new Transaction
        {
            UserId = _testUserId,
            CategoryId = _testCategory.Id,
            Amount = 500m,
            Type = TransactionType.Expense,
            Description = "Gasto",
            Date = startDate.AddDays(5),
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var pdf = await _exportService.ExportGoalsReportToPdfAsync(_testUserId, month, year);

        Assert.NotNull(pdf);
        Assert.True(pdf.Length > 0);
        
        var header = Encoding.ASCII.GetString(pdf.Take(4).ToArray());
        Assert.Equal("%PDF", header);
    }

    [Fact]
    public async Task ExportFinancialReportToPdfAsync_WithDateRange_RespectsFilter()
    {
        var startDate = new DateTime(2025, 1, 1);
        var endDate = new DateTime(2025, 1, 31);

        _context.Transactions.AddRange(
            new Transaction { UserId = _testUserId, CategoryId = _testCategory.Id, Amount = 100m, Type = TransactionType.Expense, Description = "Jan", Date = new DateTime(2025, 1, 15), CreatedAt = DateTime.UtcNow },
            new Transaction { UserId = _testUserId, CategoryId = _testCategory.Id, Amount = 200m, Type = TransactionType.Expense, Description = "Fev", Date = new DateTime(2025, 2, 1), CreatedAt = DateTime.UtcNow }
        );
        await _context.SaveChangesAsync();

        var pdf = await _exportService.ExportFinancialReportToPdfAsync(_testUserId, startDate, endDate);

        Assert.NotNull(pdf);
        Assert.True(pdf.Length > 0);
    }

    [Fact]
    public async Task ExportGoalsReportToPdfAsync_NoGoals_ReturnsEmptyReportPdf()
    {
        var month = DateTime.Now.Month;
        var year = DateTime.Now.Year;

        // Act (sem metas cadastradas)
        var pdf = await _exportService.ExportGoalsReportToPdfAsync(_testUserId, month, year);

        Assert.NotNull(pdf);
        Assert.True(pdf.Length > 0); // Deve gerar PDF mesmo sem metas
        
        var header = Encoding.ASCII.GetString(pdf.Take(4).ToArray());
        Assert.Equal("%PDF", header);
    }

    [Fact]
    public async Task ExportTransactionsToCsvAsync_MultipleTransactions_OrdersByDateDescending()
    {
        _context.Transactions.AddRange(
            new Transaction { UserId = _testUserId, CategoryId = _testCategory.Id, Amount = 100m, Type = TransactionType.Expense, Description = "Antiga", Date = DateTime.Now.AddDays(-10), CreatedAt = DateTime.UtcNow },
            new Transaction { UserId = _testUserId, CategoryId = _testCategory.Id, Amount = 200m, Type = TransactionType.Expense, Description = "Recente", Date = DateTime.Now, CreatedAt = DateTime.UtcNow }
        );
        await _context.SaveChangesAsync();

        var csv = await _exportService.ExportTransactionsToCsvAsync(_testUserId);

        var csvContent = Encoding.UTF8.GetString(csv);
        var lines = csvContent.Split('\n');
        
        // A transação mais recente deve aparecer primeiro (após o header)
        var firstDataLine = lines[1];
        Assert.Contains("Recente", firstDataLine);
    }

    [Fact]
    public async Task ExportCategoriesToCsvAsync_OrdersByName()
    {
        _context.Categories.AddRange(
            new Category { UserId = _testUserId, Name = "Zebra", CreatedAt = DateTime.UtcNow },
            new Category { UserId = _testUserId, Name = "Alpha", CreatedAt = DateTime.UtcNow }
        );
        await _context.SaveChangesAsync();

        var csv = await _exportService.ExportCategoriesToCsvAsync(_testUserId);

        var csvContent = Encoding.UTF8.GetString(csv);
        var indexAlpha = csvContent.IndexOf("Alpha");
        var indexZebra = csvContent.IndexOf("Zebra");
        
        Assert.True(indexAlpha < indexZebra); // Alpha deve vir antes
    }

    #endregion

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
