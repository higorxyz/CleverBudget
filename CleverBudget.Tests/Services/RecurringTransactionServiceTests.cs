using CleverBudget.Core.Common;
using CleverBudget.Core.DTOs;
using CleverBudget.Core.Entities;
using CleverBudget.Core.Enums;
using CleverBudget.Infrastructure.Data;
using CleverBudget.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CleverBudget.Tests.Services;

public class RecurringTransactionServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly RecurringTransactionService _service;
    private readonly string _testUserId;
    private readonly Category _testCategory;

    public RecurringTransactionServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);

        _service = new RecurringTransactionService(_context);

        _testUserId = Guid.NewGuid().ToString();
        _testCategory = new Category
        {
            Id = 1,
            UserId = _testUserId,
            Name = "Salário",
            Icon = "💰",
            Color = "#4CAF50",
            IsDefault = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Categories.Add(_testCategory);
        _context.SaveChanges();
    }

    [Fact]
    public async Task CreateAsync_ValidMonthlyRecurringTransaction_ReturnsDto()
    {
        var dto = new CreateRecurringTransactionDto
        {
            Amount = 5000m,
            Type = TransactionType.Income,
            Description = "Salário Mensal",
            CategoryId = _testCategory.Id,
            Frequency = RecurrenceFrequency.Monthly,
            StartDate = DateTime.UtcNow.Date,
            DayOfMonth = 5
        };

        var result = await _service.CreateAsync(dto, _testUserId);

        Assert.NotNull(result);
        Assert.Equal(dto.Amount, result.Amount);
        Assert.Equal(dto.Type, result.Type);
        Assert.Equal(dto.Description, result.Description);
        Assert.Equal(_testCategory.Name, result.CategoryName);
        Assert.Equal(RecurrenceFrequency.Monthly, result.Frequency);
        Assert.Equal(5, result.DayOfMonth);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task CreateAsync_ValidWeeklyRecurringTransaction_ReturnsDto()
    {
        var dto = new CreateRecurringTransactionDto
        {
            Amount = 100m,
            Type = TransactionType.Expense,
            Description = "Academia Semanal",
            CategoryId = _testCategory.Id,
            Frequency = RecurrenceFrequency.Weekly,
            StartDate = DateTime.UtcNow.Date,
            DayOfWeek = DayOfWeek.Monday
        };

        var result = await _service.CreateAsync(dto, _testUserId);

        Assert.NotNull(result);
        Assert.Equal(RecurrenceFrequency.Weekly, result.Frequency);
        Assert.Equal(DayOfWeek.Monday, result.DayOfWeek);
        Assert.NotNull(result.DayOfWeekDescription);
    }

    [Fact]
    public async Task CreateAsync_ValidDailyRecurringTransaction_ReturnsDto()
    {
        var dto = new CreateRecurringTransactionDto
        {
            Amount = 50m,
            Type = TransactionType.Expense,
            Description = "Almoço Diário",
            CategoryId = _testCategory.Id,
            Frequency = RecurrenceFrequency.Daily,
            StartDate = DateTime.UtcNow.Date
        };

        var result = await _service.CreateAsync(dto, _testUserId);

        Assert.NotNull(result);
        Assert.Equal(RecurrenceFrequency.Daily, result.Frequency);
    }

    [Fact]
    public async Task CreateAsync_ValidYearlyRecurringTransaction_ReturnsDto()
    {
        var dto = new CreateRecurringTransactionDto
        {
            Amount = 1200m,
            Type = TransactionType.Expense,
            Description = "IPVA Anual",
            CategoryId = _testCategory.Id,
            Frequency = RecurrenceFrequency.Yearly,
            StartDate = DateTime.UtcNow.Date
        };

        var result = await _service.CreateAsync(dto, _testUserId);

        Assert.NotNull(result);
        Assert.Equal(RecurrenceFrequency.Yearly, result.Frequency);
    }

    [Fact]
    public async Task CreateAsync_MonthlyWithoutDayOfMonth_ReturnsNull()
    {
        var dto = new CreateRecurringTransactionDto
        {
            Amount = 1000m,
            Type = TransactionType.Income,
            Description = "Teste",
            CategoryId = _testCategory.Id,
            Frequency = RecurrenceFrequency.Monthly,
            StartDate = DateTime.UtcNow.Date
        };

        var result = await _service.CreateAsync(dto, _testUserId);

        Assert.Null(result);
    }

    [Fact]
    public async Task CreateAsync_WeeklyWithoutDayOfWeek_ReturnsNull()
    {
        var dto = new CreateRecurringTransactionDto
        {
            Amount = 100m,
            Type = TransactionType.Expense,
            Description = "Teste",
            CategoryId = _testCategory.Id,
            Frequency = RecurrenceFrequency.Weekly,
            StartDate = DateTime.UtcNow.Date
        };

        var result = await _service.CreateAsync(dto, _testUserId);

        Assert.Null(result);
    }

    [Fact]
    public async Task CreateAsync_InvalidCategoryId_ReturnsNull()
    {
        var dto = new CreateRecurringTransactionDto
        {
            Amount = 1000m,
            Type = TransactionType.Income,
            Description = "Teste",
            CategoryId = 9999,
            Frequency = RecurrenceFrequency.Monthly,
            StartDate = DateTime.UtcNow.Date,
            DayOfMonth = 10
        };

        var result = await _service.CreateAsync(dto, _testUserId);

        Assert.Null(result);
    }

    [Fact]
    public async Task CreateAsync_CategoryFromAnotherUser_ReturnsNull()
    {
        var otherUserCategory = new Category
        {
            Id = 2,
            UserId = Guid.NewGuid().ToString(),
            Name = "Categoria Alheia",
            CreatedAt = DateTime.UtcNow
        };
        _context.Categories.Add(otherUserCategory);
        await _context.SaveChangesAsync();

        var dto = new CreateRecurringTransactionDto
        {
            Amount = 1000m,
            Type = TransactionType.Income,
            Description = "Teste",
            CategoryId = otherUserCategory.Id,
            Frequency = RecurrenceFrequency.Monthly,
            StartDate = DateTime.UtcNow.Date,
            DayOfMonth = 10
        };

        var result = await _service.CreateAsync(dto, _testUserId);

        Assert.Null(result);
    }

    [Fact]
    public async Task CreateAsync_WithEndDate_SetsEndDateCorrectly()
    {
        var startDate = DateTime.UtcNow.Date;
        var endDate = startDate.AddMonths(6);

        var dto = new CreateRecurringTransactionDto
        {
            Amount = 500m,
            Type = TransactionType.Expense,
            Description = "Aluguel Temporário",
            CategoryId = _testCategory.Id,
            Frequency = RecurrenceFrequency.Monthly,
            StartDate = startDate,
            EndDate = endDate,
            DayOfMonth = 1
        };

        var result = await _service.CreateAsync(dto, _testUserId);

        Assert.NotNull(result);
        Assert.Equal(endDate, result.EndDate);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDto()
    {
        var recurring = new RecurringTransaction
        {
            UserId = _testUserId,
            Amount = 3000m,
            Type = TransactionType.Income,
            Description = "Freelance",
            CategoryId = _testCategory.Id,
            Frequency = RecurrenceFrequency.Monthly,
            StartDate = DateTime.UtcNow.Date,
            DayOfMonth = 15,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.RecurringTransactions.Add(recurring);
        await _context.SaveChangesAsync();

        var result = await _service.GetByIdAsync(recurring.Id, _testUserId);

        Assert.NotNull(result);
        Assert.Equal(recurring.Id, result.Id);
        Assert.Equal(recurring.Amount, result.Amount);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNull()
    {
        var result = await _service.GetByIdAsync(9999, _testUserId);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_IdFromAnotherUser_ReturnsNull()
    {
        var otherUserId = Guid.NewGuid().ToString();
        var recurring = new RecurringTransaction
        {
            UserId = otherUserId,
            Amount = 1000m,
            Type = TransactionType.Income,
            Description = "Outro Usuário",
            CategoryId = _testCategory.Id,
            Frequency = RecurrenceFrequency.Monthly,
            StartDate = DateTime.UtcNow.Date,
            DayOfMonth = 10,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.RecurringTransactions.Add(recurring);
        await _context.SaveChangesAsync();

        var result = await _service.GetByIdAsync(recurring.Id, _testUserId);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsOnlyUserRecurringTransactions()
    {
        var otherUserId = Guid.NewGuid().ToString();

        _context.RecurringTransactions.AddRange(
            new RecurringTransaction
            {
                UserId = _testUserId,
                Amount = 1000m,
                Type = TransactionType.Income,
                Description = "Minha Transação 1",
                CategoryId = _testCategory.Id,
                Frequency = RecurrenceFrequency.Monthly,
                StartDate = DateTime.UtcNow.Date,
                DayOfMonth = 1,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new RecurringTransaction
            {
                UserId = _testUserId,
                Amount = 500m,
                Type = TransactionType.Expense,
                Description = "Minha Transação 2",
                CategoryId = _testCategory.Id,
                Frequency = RecurrenceFrequency.Weekly,
                StartDate = DateTime.UtcNow.Date,
                DayOfWeek = DayOfWeek.Friday,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new RecurringTransaction
            {
                UserId = otherUserId,
                Amount = 2000m,
                Type = TransactionType.Income,
                Description = "Outro Usuário",
                CategoryId = _testCategory.Id,
                Frequency = RecurrenceFrequency.Monthly,
                StartDate = DateTime.UtcNow.Date,
                DayOfMonth = 5,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            }
        );
        await _context.SaveChangesAsync();

        var result = await _service.GetAllAsync(_testUserId);

        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        Assert.All(result, r => Assert.Contains("Minha Transação", r.Description));
    }

    [Fact]
    public async Task GetAllAsync_WithIsActiveFilter_ReturnsOnlyActive()
    {
        _context.RecurringTransactions.AddRange(
            new RecurringTransaction
            {
                UserId = _testUserId,
                Amount = 1000m,
                Type = TransactionType.Income,
                Description = "Ativo",
                CategoryId = _testCategory.Id,
                Frequency = RecurrenceFrequency.Monthly,
                StartDate = DateTime.UtcNow.Date,
                DayOfMonth = 1,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new RecurringTransaction
            {
                UserId = _testUserId,
                Amount = 500m,
                Type = TransactionType.Expense,
                Description = "Inativo",
                CategoryId = _testCategory.Id,
                Frequency = RecurrenceFrequency.Monthly,
                StartDate = DateTime.UtcNow.Date,
                DayOfMonth = 5,
                IsActive = false,
                CreatedAt = DateTime.UtcNow
            }
        );
        await _context.SaveChangesAsync();

        var result = await _service.GetAllAsync(_testUserId, isActive: true);

        Assert.Single(result);
        Assert.All(result, r => Assert.True(r.IsActive));
    }

    [Fact]
    public async Task GetAllAsync_WithIsActiveFilter_ReturnsOnlyInactive()
    {
        _context.RecurringTransactions.AddRange(
            new RecurringTransaction
            {
                UserId = _testUserId,
                Amount = 1000m,
                Type = TransactionType.Income,
                Description = "Ativo",
                CategoryId = _testCategory.Id,
                Frequency = RecurrenceFrequency.Monthly,
                StartDate = DateTime.UtcNow.Date,
                DayOfMonth = 1,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new RecurringTransaction
            {
                UserId = _testUserId,
                Amount = 500m,
                Type = TransactionType.Expense,
                Description = "Inativo",
                CategoryId = _testCategory.Id,
                Frequency = RecurrenceFrequency.Monthly,
                StartDate = DateTime.UtcNow.Date,
                DayOfMonth = 5,
                IsActive = false,
                CreatedAt = DateTime.UtcNow
            }
        );
        await _context.SaveChangesAsync();

        var result = await _service.GetAllAsync(_testUserId, isActive: false);

        Assert.Single(result);
        Assert.All(result, r => Assert.False(r.IsActive));
    }

    [Fact]
    public async Task GetPagedAsync_ReturnsCorrectPage()
    {
        for (int i = 1; i <= 15; i++)
        {
            _context.RecurringTransactions.Add(new RecurringTransaction
            {
                UserId = _testUserId,
                Amount = i * 100m,
                Type = TransactionType.Income,
                Description = $"Recorrente {i}",
                CategoryId = _testCategory.Id,
                Frequency = RecurrenceFrequency.Monthly,
                StartDate = DateTime.UtcNow.Date.AddDays(-i),
                DayOfMonth = i,
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-i)
            });
        }
        await _context.SaveChangesAsync();

        var paginationParams = new PaginationParams
        {
            Page = 2,
            PageSize = 5
        };

        var result = await _service.GetPagedAsync(_testUserId, paginationParams);

        Assert.Equal(5, result.Items.Count());
        Assert.Equal(15, result.TotalCount);
        Assert.Equal(3, result.TotalPages);
        Assert.Equal(2, result.Page);
    }

    [Fact]
    public async Task GetPagedAsync_WithSortByAmount_ReturnsSorted()
    {
        _context.RecurringTransactions.AddRange(
            new RecurringTransaction
            {
                UserId = _testUserId,
                Amount = 500m,
                Type = TransactionType.Income,
                Description = "Médio",
                CategoryId = _testCategory.Id,
                Frequency = RecurrenceFrequency.Monthly,
                StartDate = DateTime.UtcNow.Date,
                DayOfMonth = 1,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new RecurringTransaction
            {
                UserId = _testUserId,
                Amount = 1000m,
                Type = TransactionType.Income,
                Description = "Alto",
                CategoryId = _testCategory.Id,
                Frequency = RecurrenceFrequency.Monthly,
                StartDate = DateTime.UtcNow.Date,
                DayOfMonth = 5,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new RecurringTransaction
            {
                UserId = _testUserId,
                Amount = 100m,
                Type = TransactionType.Expense,
                Description = "Baixo",
                CategoryId = _testCategory.Id,
                Frequency = RecurrenceFrequency.Monthly,
                StartDate = DateTime.UtcNow.Date,
                DayOfMonth = 10,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            }
        );
        await _context.SaveChangesAsync();

        var paginationParams = new PaginationParams
        {
            Page = 1,
            PageSize = 10,
            SortBy = "amount",
            SortOrder = "asc"
        };

        var result = await _service.GetPagedAsync(_testUserId, paginationParams);

        var amounts = result.Items.Select(r => r.Amount).ToList();
        Assert.Equal(100m, amounts[0]);
        Assert.Equal(500m, amounts[1]);
        Assert.Equal(1000m, amounts[2]);
    }

    [Fact]
    public async Task UpdateAsync_ValidUpdate_UpdatesAndReturnsDto()
    {
        var recurring = new RecurringTransaction
        {
            UserId = _testUserId,
            Amount = 1000m,
            Type = TransactionType.Income,
            Description = "Descrição Original",
            CategoryId = _testCategory.Id,
            Frequency = RecurrenceFrequency.Monthly,
            StartDate = DateTime.UtcNow.Date,
            DayOfMonth = 10,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.RecurringTransactions.Add(recurring);
        await _context.SaveChangesAsync();

        var updateDto = new UpdateRecurringTransactionDto
        {
            Amount = 1500m,
            Description = "Descrição Atualizada"
        };

        var result = await _service.UpdateAsync(recurring.Id, updateDto, _testUserId);

        Assert.NotNull(result);
        Assert.Equal(1500m, result.Amount);
        Assert.Equal("Descrição Atualizada", result.Description);
    }

    [Fact]
    public async Task UpdateAsync_PartialUpdate_UpdatesOnlyProvidedFields()
    {
        var recurring = new RecurringTransaction
        {
            UserId = _testUserId,
            Amount = 1000m,
            Type = TransactionType.Income,
            Description = "Descrição Original",
            CategoryId = _testCategory.Id,
            Frequency = RecurrenceFrequency.Monthly,
            StartDate = DateTime.UtcNow.Date,
            DayOfMonth = 10,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.RecurringTransactions.Add(recurring);
        await _context.SaveChangesAsync();

        var updateDto = new UpdateRecurringTransactionDto
        {
            Amount = 2000m
        };

        var result = await _service.UpdateAsync(recurring.Id, updateDto, _testUserId);

        Assert.NotNull(result);
        Assert.Equal(2000m, result.Amount);
        Assert.Equal("Descrição Original", result.Description);
    }

    [Fact]
    public async Task UpdateAsync_NonExistingId_ReturnsNull()
    {
        var updateDto = new UpdateRecurringTransactionDto
        {
            Amount = 1000m
        };

        var result = await _service.UpdateAsync(9999, updateDto, _testUserId);

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_IdFromAnotherUser_ReturnsNull()
    {
        var otherUserId = Guid.NewGuid().ToString();
        var recurring = new RecurringTransaction
        {
            UserId = otherUserId,
            Amount = 1000m,
            Type = TransactionType.Income,
            Description = "Outro Usuário",
            CategoryId = _testCategory.Id,
            Frequency = RecurrenceFrequency.Monthly,
            StartDate = DateTime.UtcNow.Date,
            DayOfMonth = 10,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.RecurringTransactions.Add(recurring);
        await _context.SaveChangesAsync();

        var updateDto = new UpdateRecurringTransactionDto
        {
            Amount = 2000m
        };

        var result = await _service.UpdateAsync(recurring.Id, updateDto, _testUserId);

        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteAsync_ExistingId_DeletesAndReturnsTrue()
    {
        var recurring = new RecurringTransaction
        {
            UserId = _testUserId,
            Amount = 1000m,
            Type = TransactionType.Income,
            Description = "Para Deletar",
            CategoryId = _testCategory.Id,
            Frequency = RecurrenceFrequency.Monthly,
            StartDate = DateTime.UtcNow.Date,
            DayOfMonth = 10,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.RecurringTransactions.Add(recurring);
        await _context.SaveChangesAsync();

        var result = await _service.DeleteAsync(recurring.Id, _testUserId);

        Assert.True(result);
        var deleted = await _context.RecurringTransactions.FindAsync(recurring.Id);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingId_ReturnsFalse()
    {
        var result = await _service.DeleteAsync(9999, _testUserId);

        Assert.False(result);
    }

    [Fact]
    public async Task DeleteAsync_IdFromAnotherUser_ReturnsFalse()
    {
        var otherUserId = Guid.NewGuid().ToString();
        var recurring = new RecurringTransaction
        {
            UserId = otherUserId,
            Amount = 1000m,
            Type = TransactionType.Income,
            Description = "Outro Usuário",
            CategoryId = _testCategory.Id,
            Frequency = RecurrenceFrequency.Monthly,
            StartDate = DateTime.UtcNow.Date,
            DayOfMonth = 10,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.RecurringTransactions.Add(recurring);
        await _context.SaveChangesAsync();

        var result = await _service.DeleteAsync(recurring.Id, _testUserId);

        Assert.False(result);
    }

    [Fact]
    public async Task ToggleActiveAsync_ActiveToInactive_TogglesCorrectly()
    {
        var recurring = new RecurringTransaction
        {
            UserId = _testUserId,
            Amount = 1000m,
            Type = TransactionType.Income,
            Description = "Ativo",
            CategoryId = _testCategory.Id,
            Frequency = RecurrenceFrequency.Monthly,
            StartDate = DateTime.UtcNow.Date,
            DayOfMonth = 10,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.RecurringTransactions.Add(recurring);
        await _context.SaveChangesAsync();

        var result = await _service.ToggleActiveAsync(recurring.Id, _testUserId);

        Assert.True(result);
        var updated = await _context.RecurringTransactions.FindAsync(recurring.Id);
        Assert.NotNull(updated);
        Assert.False(updated.IsActive);
    }

    [Fact]
    public async Task ToggleActiveAsync_InactiveToActive_TogglesCorrectly()
    {
        var recurring = new RecurringTransaction
        {
            UserId = _testUserId,
            Amount = 1000m,
            Type = TransactionType.Income,
            Description = "Inativo",
            CategoryId = _testCategory.Id,
            Frequency = RecurrenceFrequency.Monthly,
            StartDate = DateTime.UtcNow.Date,
            DayOfMonth = 10,
            IsActive = false,
            CreatedAt = DateTime.UtcNow
        };
        _context.RecurringTransactions.Add(recurring);
        await _context.SaveChangesAsync();

        var result = await _service.ToggleActiveAsync(recurring.Id, _testUserId);

        Assert.True(result);
        var updated = await _context.RecurringTransactions.FindAsync(recurring.Id);
        Assert.NotNull(updated);
        Assert.True(updated.IsActive);
    }

    [Fact]
    public async Task ToggleActiveAsync_NonExistingId_ReturnsFalse()
    {
        var result = await _service.ToggleActiveAsync(9999, _testUserId);

        Assert.False(result);
    }

    [Fact]
    public async Task ToggleActiveAsync_IdFromAnotherUser_ReturnsFalse()
    {
        var otherUserId = Guid.NewGuid().ToString();
        var recurring = new RecurringTransaction
        {
            UserId = otherUserId,
            Amount = 1000m,
            Type = TransactionType.Income,
            Description = "Outro Usuário",
            CategoryId = _testCategory.Id,
            Frequency = RecurrenceFrequency.Monthly,
            StartDate = DateTime.UtcNow.Date,
            DayOfMonth = 10,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.RecurringTransactions.Add(recurring);
        await _context.SaveChangesAsync();

        var result = await _service.ToggleActiveAsync(recurring.Id, _testUserId);

        Assert.False(result);
    }

    [Fact]
    public async Task CreateAsync_IncludesFrequencyDescription()
    {
        var dto = new CreateRecurringTransactionDto
        {
            Amount = 1000m,
            Type = TransactionType.Income,
            Description = "Teste",
            CategoryId = _testCategory.Id,
            Frequency = RecurrenceFrequency.Monthly,
            StartDate = DateTime.UtcNow.Date,
            DayOfMonth = 10
        };

        var result = await _service.CreateAsync(dto, _testUserId);

        Assert.NotNull(result);
        Assert.Equal("Mensal", result.FrequencyDescription);
    }

    [Fact]
    public async Task CreateAsync_IncludesTypeDescription()
    {
        var incomeDto = new CreateRecurringTransactionDto
        {
            Amount = 1000m,
            Type = TransactionType.Income,
            Description = "Receita",
            CategoryId = _testCategory.Id,
            Frequency = RecurrenceFrequency.Monthly,
            StartDate = DateTime.UtcNow.Date,
            DayOfMonth = 10
        };

        var incomeResult = await _service.CreateAsync(incomeDto, _testUserId);
        Assert.NotNull(incomeResult);
        Assert.Equal("Receita", incomeResult.TypeDescription);

        var expenseDto = new CreateRecurringTransactionDto
        {
            Amount = 500m,
            Type = TransactionType.Expense,
            Description = "Despesa",
            CategoryId = _testCategory.Id,
            Frequency = RecurrenceFrequency.Monthly,
            StartDate = DateTime.UtcNow.Date,
            DayOfMonth = 15
        };

        var expenseResult = await _service.CreateAsync(expenseDto, _testUserId);
        Assert.NotNull(expenseResult);
        Assert.Equal("Despesa", expenseResult.TypeDescription);
    }

    [Fact]
    public async Task CreateAsync_IncludesCategoryDetails()
    {
        var dto = new CreateRecurringTransactionDto
        {
            Amount = 1000m,
            Type = TransactionType.Income,
            Description = "Teste",
            CategoryId = _testCategory.Id,
            Frequency = RecurrenceFrequency.Monthly,
            StartDate = DateTime.UtcNow.Date,
            DayOfMonth = 10
        };

        var result = await _service.CreateAsync(dto, _testUserId);

        Assert.NotNull(result);
        Assert.Equal(_testCategory.Name, result.CategoryName);
        Assert.Equal(_testCategory.Icon, result.CategoryIcon);
        Assert.Equal(_testCategory.Color, result.CategoryColor);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
