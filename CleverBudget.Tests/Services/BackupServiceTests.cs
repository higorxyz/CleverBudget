using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using CleverBudget.Core.Entities;
using CleverBudget.Core.Enums;
using CleverBudget.Core.Options;
using CleverBudget.Infrastructure.Data;
using CleverBudget.Infrastructure.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace CleverBudget.Tests.Services;

public sealed class BackupServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _context;
    private readonly BackupService _service;
    private readonly string _contentRootPath;
    private readonly string _userId = Guid.NewGuid().ToString();
    private readonly IOptions<BackupOptions> _options;

    public BackupServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new AppDbContext(options);
        _context.Database.EnsureCreated();

        _contentRootPath = Path.Combine(Path.GetTempPath(), $"cleverbudget-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_contentRootPath);

        _options = Options.Create(new BackupOptions
        {
            EnableAutomaticBackups = true,
            RootPath = "Backups",
            RetentionDays = 7,
            Interval = TimeSpan.FromHours(1),
            RunOnStartup = false
        });

        _service = new BackupService(
            _context,
            _options,
            NullLogger<BackupService>.Instance,
            new TestHostEnvironment(_contentRootPath));
    }

    [Fact]
    public async Task CreateBackupAsync_GeneratesCompressedPayload()
    {
        SeedSampleData();

        var result = await _service.CreateBackupAsync(persistToDisk: false);

        Assert.NotNull(result);
        Assert.EndsWith(".json.gz", result.FileName);
        Assert.NotNull(result.Content);
        Assert.True(result.Content.Length > 0);
        Assert.Null(result.StoredAt);

        using var memory = new MemoryStream(result.Content);
        using var gzip = new GZipStream(memory, CompressionMode.Decompress);
        using var document = JsonDocument.Parse(gzip);

    var root = document.RootElement;
    Assert.Equal(1, root.GetProperty("users").GetArrayLength());
        Assert.Equal(1, root.GetProperty("categories").GetArrayLength());
        Assert.Equal(1, root.GetProperty("transactions").GetArrayLength());
        Assert.Equal(1, root.GetProperty("budgets").GetArrayLength());
        Assert.Equal(1, root.GetProperty("recurringTransactions").GetArrayLength());
    }

    [Fact]
    public async Task CreateBackupAsync_WhenPersistToDisk_WritesFile()
    {
        SeedSampleData();

        var result = await _service.CreateBackupAsync(persistToDisk: true);

        Assert.NotNull(result.StoredAt);
        Assert.True(File.Exists(result.StoredAt));
    }

    [Fact]
    public async Task RestoreBackupAsync_ReplacesExistingData()
    {
        SeedSampleData();
        var backup = await _service.CreateBackupAsync(persistToDisk: false);

        using var newConnection = new SqliteConnection("DataSource=:memory:");
        newConnection.Open();

        var newOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(newConnection)
            .Options;

        using var newContext = new AppDbContext(newOptions);
        newContext.Database.EnsureCreated();

        var newService = new BackupService(
            newContext,
            _options,
            NullLogger<BackupService>.Instance,
            new TestHostEnvironment(_contentRootPath));

        using var stream = new MemoryStream(backup.Content);
        await newService.RestoreBackupAsync(stream);

        Assert.Equal(1, newContext.Users.Count());
        Assert.Equal("testuser@example.com", newContext.Users.Single().Email);
        Assert.Equal(1, newContext.Categories.Count());
        Assert.Equal("Alimentação", newContext.Categories.Single().Name);
        Assert.Equal(1, newContext.Transactions.Count());
        Assert.Equal("Compra mercado", newContext.Transactions.Single().Description);
        Assert.Equal(1, newContext.Budgets.Count());
        Assert.Equal(1, newContext.Goals.Count());
        Assert.Equal(1, newContext.RecurringTransactions.Count());
    }

    private void SeedSampleData()
    {
        if (_context.Categories.Any())
        {
            return;
        }

        // Create user first to satisfy foreign key constraints
        var user = new User
        {
            Id = _userId,
            UserName = "testuser@example.com",
            Email = "testuser@example.com",
            FirstName = "Test",
            LastName = "User",
            EmailConfirmed = true,
            NormalizedUserName = "TESTUSER@EXAMPLE.COM",
            NormalizedEmail = "TESTUSER@EXAMPLE.COM",
            SecurityStamp = Guid.NewGuid().ToString(),
            PasswordHash = "hashed-password",
            ConcurrencyStamp = Guid.NewGuid().ToString(),
            PhoneNumberConfirmed = false,
            TwoFactorEnabled = false,
            LockoutEnabled = false,
            AccessFailedCount = 0,
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(user);
        _context.SaveChanges();

        var category = new Category
        {
            Id = 1,
            UserId = _userId,
            Name = "Alimentação",
            Icon = "food",
            Color = "#FFFFFF",
            IsDefault = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Categories.Add(category);

        _context.Transactions.Add(new Transaction
        {
            Id = 1,
            UserId = _userId,
            Amount = 100m,
            Type = TransactionType.Expense,
            Description = "Compra mercado",
            CategoryId = category.Id,
            Date = DateTime.UtcNow.Date,
            CreatedAt = DateTime.UtcNow
        });

        _context.Budgets.Add(new Budget
        {
            Id = 1,
            UserId = _userId,
            CategoryId = category.Id,
            Amount = 500m,
            Month = DateTime.UtcNow.Month,
            Year = DateTime.UtcNow.Year,
            CreatedAt = DateTime.UtcNow
        });

        _context.Goals.Add(new Goal
        {
            Id = 1,
            UserId = _userId,
            CategoryId = category.Id,
            TargetAmount = 1000m,
            Month = DateTime.UtcNow.Month,
            Year = DateTime.UtcNow.Year,
            CreatedAt = DateTime.UtcNow
        });

        _context.RecurringTransactions.Add(new RecurringTransaction
        {
            Id = 1,
            UserId = _userId,
            CategoryId = category.Id,
            Amount = 50m,
            Type = TransactionType.Expense,
            Description = "Assinatura streaming",
            Frequency = RecurrenceFrequency.Monthly,
            StartDate = DateTime.UtcNow.Date,
            DayOfMonth = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });

        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
        if (Directory.Exists(_contentRootPath))
        {
            Directory.Delete(_contentRootPath, true);
        }
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public TestHostEnvironment(string contentRootPath)
        {
            EnvironmentName = Environments.Development;
            ApplicationName = "CleverBudget.Tests";
            ContentRootPath = contentRootPath;
            ContentRootFileProvider = new PhysicalFileProvider(contentRootPath);
        }

        public string EnvironmentName { get; set; }
        public string ApplicationName { get; set; }
        public string ContentRootPath { get; set; }
        public IFileProvider ContentRootFileProvider { get; set; }
    }
}
