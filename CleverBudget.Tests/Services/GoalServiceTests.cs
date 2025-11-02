using CleverBudget.Core.Common;
using CleverBudget.Core.DTOs;
using CleverBudget.Core.Entities;
using CleverBudget.Core.Enums;
using CleverBudget.Infrastructure.Data;
using CleverBudget.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CleverBudget.Tests.Services;

public class GoalServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly GoalService _goalService;
    private readonly string _testUserId;
    private readonly Category _testCategory;

    public GoalServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);

        _goalService = new GoalService(_context);

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
    public async Task CreateAsync_ValidGoal_ReturnsGoalResponse()
    {

        var dto = new CreateGoalDto
        {
            CategoryId = _testCategory.Id,
            TargetAmount = 500m,
            Month = DateTime.Now.Month,
            Year = DateTime.Now.Year
        };


        var result = await _goalService.CreateAsync(dto, _testUserId);


        Assert.NotNull(result);
        Assert.Equal(dto.CategoryId, result.CategoryId);
        Assert.Equal(dto.TargetAmount, result.TargetAmount);
        Assert.Equal(dto.Month, result.Month);
        Assert.Equal(dto.Year, result.Year);
        Assert.Equal(_testCategory.Name, result.CategoryName);
    }

    [Fact]
    public async Task CreateAsync_InvalidCategoryId_ReturnsNull()
    {

        var dto = new CreateGoalDto
        {
            CategoryId = 9999, // Não existe
            TargetAmount = 500m,
            Month = DateTime.Now.Month,
            Year = DateTime.Now.Year
        };


        var result = await _goalService.CreateAsync(dto, _testUserId);


        Assert.Null(result);
    }

    [Fact]
    public async Task CreateAsync_DuplicateGoal_ReturnsNull()
    {

        var month = DateTime.Now.Month;
        var year = DateTime.Now.Year;

        var existingGoal = new Goal
        {
            UserId = _testUserId,
            CategoryId = _testCategory.Id,
            TargetAmount = 300m,
            Month = month,
            Year = year,
            CreatedAt = DateTime.UtcNow
        };
        _context.Goals.Add(existingGoal);
        await _context.SaveChangesAsync();

        var dto = new CreateGoalDto
        {
            CategoryId = _testCategory.Id,
            TargetAmount = 500m,
            Month = month,
            Year = year
        };


        var result = await _goalService.CreateAsync(dto, _testUserId);


        Assert.Null(result); // Não pode criar meta duplicada
    }

    [Fact]
    public async Task GetStatusAsync_CalculatesProgressCorrectly()
    {

        var month = DateTime.Now.Month;
        var year = DateTime.Now.Year;

        var goal = new Goal
        {
            UserId = _testUserId,
            CategoryId = _testCategory.Id,
            TargetAmount = 1000m,
            Month = month,
            Year = year,
            CreatedAt = DateTime.UtcNow
        };
        _context.Goals.Add(goal);


        var startDate = new DateTime(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        _context.Transactions.AddRange(
            new Transaction
            {
                UserId = _testUserId,
                CategoryId = _testCategory.Id,
                Amount = 300m,
                Type = TransactionType.Expense,
                Description = "Despesa 1",
                Date = startDate.AddDays(5),
                CreatedAt = DateTime.UtcNow
            },
            new Transaction
            {
                UserId = _testUserId,
                CategoryId = _testCategory.Id,
                Amount = 200m,
                Type = TransactionType.Expense,
                Description = "Despesa 2",
                Date = startDate.AddDays(10),
                CreatedAt = DateTime.UtcNow
            }
        );
        await _context.SaveChangesAsync();


        var result = await _goalService.GetStatusAsync(_testUserId, month, year);


        var goalStatus = result.FirstOrDefault();
        Assert.NotNull(goalStatus);
        Assert.Equal(500m, goalStatus.CurrentAmount); // 300 + 200
        Assert.Equal(50m, goalStatus.Percentage); // (500/1000) * 100
        Assert.Equal("OnTrack", goalStatus.Status); // < 80%
    }

    [Fact]
    public async Task GetStatusAsync_StatusWarning_At80Percent()
    {

        var month = DateTime.Now.Month;
        var year = DateTime.Now.Year;

        var goal = new Goal
        {
            UserId = _testUserId,
            CategoryId = _testCategory.Id,
            TargetAmount = 1000m,
            Month = month,
            Year = year,
            CreatedAt = DateTime.UtcNow
        };
        _context.Goals.Add(goal);


        var startDate = new DateTime(year, month, 1);
        _context.Transactions.Add(new Transaction
        {
            UserId = _testUserId,
            CategoryId = _testCategory.Id,
            Amount = 850m,
            Type = TransactionType.Expense,
            Description = "Despesa alta",
            Date = startDate.AddDays(5),
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();


        var result = await _goalService.GetStatusAsync(_testUserId, month, year);


        var goalStatus = result.FirstOrDefault();
        Assert.NotNull(goalStatus);
        Assert.Equal(850m, goalStatus.CurrentAmount);
        Assert.Equal(85m, goalStatus.Percentage);
        Assert.Equal("Warning", goalStatus.Status); // >= 80% and < 100%
    }

    [Fact]
    public async Task GetStatusAsync_StatusExceeded_AtOrAbove100Percent()
    {

        var month = DateTime.Now.Month;
        var year = DateTime.Now.Year;

        var goal = new Goal
        {
            UserId = _testUserId,
            CategoryId = _testCategory.Id,
            TargetAmount = 1000m,
            Month = month,
            Year = year,
            CreatedAt = DateTime.UtcNow
        };
        _context.Goals.Add(goal);


        var startDate = new DateTime(year, month, 1);
        _context.Transactions.Add(new Transaction
        {
            UserId = _testUserId,
            CategoryId = _testCategory.Id,
            Amount = 1200m,
            Type = TransactionType.Expense,
            Description = "Despesa acima da meta",
            Date = startDate.AddDays(5),
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();


        var result = await _goalService.GetStatusAsync(_testUserId, month, year);


        var goalStatus = result.FirstOrDefault();
        Assert.NotNull(goalStatus);
        Assert.Equal(1200m, goalStatus.CurrentAmount);
        Assert.Equal(120m, goalStatus.Percentage);
        Assert.Equal("Exceeded", goalStatus.Status); // >= 100%
    }

    [Fact]
    public async Task GetStatusAsync_IgnoresIncomeTransactions()
    {

        var month = DateTime.Now.Month;
        var year = DateTime.Now.Year;

        var goal = new Goal
        {
            UserId = _testUserId,
            CategoryId = _testCategory.Id,
            TargetAmount = 1000m,
            Month = month,
            Year = year,
            CreatedAt = DateTime.UtcNow
        };
        _context.Goals.Add(goal);

        var startDate = new DateTime(year, month, 1);
        _context.Transactions.AddRange(
            new Transaction
            {
                UserId = _testUserId,
                CategoryId = _testCategory.Id,
                Amount = 300m,
                Type = TransactionType.Expense,
                Description = "Despesa",
                Date = startDate.AddDays(5),
                CreatedAt = DateTime.UtcNow
            },
            new Transaction
            {
                UserId = _testUserId,
                CategoryId = _testCategory.Id,
                Amount = 500m,
                Type = TransactionType.Income, // Receita - deve ser ignorada
                Description = "Receita",
                Date = startDate.AddDays(10),
                CreatedAt = DateTime.UtcNow
            }
        );
        await _context.SaveChangesAsync();


        var result = await _goalService.GetStatusAsync(_testUserId, month, year);


        var goalStatus = result.FirstOrDefault();
        Assert.NotNull(goalStatus);
        Assert.Equal(300m, goalStatus.CurrentAmount); // Apenas a despesa
        Assert.Equal(30m, goalStatus.Percentage);
    }

    [Fact]
    public async Task UpdateAsync_ValidData_UpdatesGoal()
    {

        var goal = new Goal
        {
            UserId = _testUserId,
            CategoryId = _testCategory.Id,
            TargetAmount = 500m,
            Month = DateTime.Now.Month,
            Year = DateTime.Now.Year,
            CreatedAt = DateTime.UtcNow
        };
        _context.Goals.Add(goal);
        await _context.SaveChangesAsync();

        var updateDto = new UpdateGoalDto
        {
            TargetAmount = 800m
        };


        var result = await _goalService.UpdateAsync(goal.Id, updateDto, _testUserId);


        Assert.NotNull(result);
        Assert.Equal(updateDto.TargetAmount, result.TargetAmount);
    }

    [Fact]
    public async Task UpdateAsync_GoalNotFound_ReturnsNull()
    {

        var updateDto = new UpdateGoalDto
        {
            TargetAmount = 800m
        };


        var result = await _goalService.UpdateAsync(9999, updateDto, _testUserId);


        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteAsync_ExistingGoal_ReturnsTrue()
    {

        var goal = new Goal
        {
            UserId = _testUserId,
            CategoryId = _testCategory.Id,
            TargetAmount = 500m,
            Month = DateTime.Now.Month,
            Year = DateTime.Now.Year,
            CreatedAt = DateTime.UtcNow
        };
        _context.Goals.Add(goal);
        await _context.SaveChangesAsync();


        var result = await _goalService.DeleteAsync(goal.Id, _testUserId);


        Assert.True(result);
        var deletedGoal = await _context.Goals.FindAsync(goal.Id);
        Assert.Null(deletedGoal);
    }

    [Fact]
    public async Task DeleteAsync_GoalNotFound_ReturnsFalse()
    {

        var result = await _goalService.DeleteAsync(9999, _testUserId);


        Assert.False(result);
    }

    [Fact]
    public async Task GetPagedAsync_ReturnsPagedResults()
    {

        for (int i = 1; i <= 15; i++)
        {
            _context.Goals.Add(new Goal
            {
                UserId = _testUserId,
                CategoryId = _testCategory.Id,
                TargetAmount = i * 100,
                Month = i % 12 + 1,
                Year = 2025,
                CreatedAt = DateTime.UtcNow
            });
        }
        await _context.SaveChangesAsync();

        var paginationParams = new PaginationParams
        {
            Page = 1,
            PageSize = 10
        };


        var result = await _goalService.GetPagedAsync(_testUserId, paginationParams);


        Assert.NotNull(result);
        Assert.Equal(10, result.Items.Count);
        Assert.Equal(15, result.TotalCount);
        Assert.Equal(2, result.TotalPages);
        Assert.True(result.HasNextPage);
    }

    [Fact]
    public async Task GetPagedAsync_WithMonthFilter_ReturnsFilteredResults()
    {

        _context.Goals.AddRange(
            new Goal { UserId = _testUserId, CategoryId = _testCategory.Id, TargetAmount = 100m, Month = 1, Year = 2025, CreatedAt = DateTime.UtcNow },
            new Goal { UserId = _testUserId, CategoryId = _testCategory.Id, TargetAmount = 200m, Month = 2, Year = 2025, CreatedAt = DateTime.UtcNow },
            new Goal { UserId = _testUserId, CategoryId = _testCategory.Id, TargetAmount = 300m, Month = 1, Year = 2025, CreatedAt = DateTime.UtcNow }
        );
        await _context.SaveChangesAsync();

        var paginationParams = new PaginationParams { Page = 1, PageSize = 10 };


        var result = await _goalService.GetPagedAsync(_testUserId, paginationParams, month: 1);


        Assert.Equal(2, result.TotalCount);
        Assert.All(result.Items, g => Assert.Equal(1, g.Month));
    }

    [Fact]
    public async Task GetStatusAsync_MultipleGoals_ReturnsSortedByPercentage()
    {

        var month = DateTime.Now.Month;
        var year = DateTime.Now.Year;

        var category2 = new Category
        {
            Id = 2,
            UserId = _testUserId,
            Name = "Transporte",
            CreatedAt = DateTime.UtcNow
        };
        _context.Categories.Add(category2);

        _context.Goals.AddRange(
            new Goal { UserId = _testUserId, CategoryId = _testCategory.Id, TargetAmount = 1000m, Month = month, Year = year, CreatedAt = DateTime.UtcNow },
            new Goal { UserId = _testUserId, CategoryId = category2.Id, TargetAmount = 500m, Month = month, Year = year, CreatedAt = DateTime.UtcNow }
        );

        var startDate = new DateTime(year, month, 1);
        _context.Transactions.AddRange(
            new Transaction { UserId = _testUserId, CategoryId = _testCategory.Id, Amount = 300m, Type = TransactionType.Expense, Description = "A", Date = startDate, CreatedAt = DateTime.UtcNow },
            new Transaction { UserId = _testUserId, CategoryId = category2.Id, Amount = 400m, Type = TransactionType.Expense, Description = "B", Date = startDate, CreatedAt = DateTime.UtcNow }
        );
        await _context.SaveChangesAsync();


        var result = await _goalService.GetStatusAsync(_testUserId, month, year);


        Assert.Equal(2, result.Count());
        // Deve vir ordenado por percentual (decrescente)
        var goals = result.ToList();
        Assert.True(goals[0].Percentage >= goals[1].Percentage);
    }

    [Fact]
    public async Task GetStatusAsync_NoTransactions_ReturnsZeroProgress()
    {

        var month = DateTime.Now.Month;
        var year = DateTime.Now.Year;

        var goal = new Goal
        {
            UserId = _testUserId,
            CategoryId = _testCategory.Id,
            TargetAmount = 1000m,
            Month = month,
            Year = year,
            CreatedAt = DateTime.UtcNow
        };
        _context.Goals.Add(goal);
        await _context.SaveChangesAsync();


        var result = await _goalService.GetStatusAsync(_testUserId, month, year);


        var goalStatus = result.FirstOrDefault();
        Assert.NotNull(goalStatus);
        Assert.Equal(0m, goalStatus.CurrentAmount);
        Assert.Equal(0m, goalStatus.Percentage);
        Assert.Equal("OnTrack", goalStatus.Status);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
