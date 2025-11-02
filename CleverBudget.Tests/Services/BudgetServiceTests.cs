using CleverBudget.Core.Common;
using CleverBudget.Core.DTOs;
using CleverBudget.Core.Entities;
using CleverBudget.Core.Enums;
using CleverBudget.Infrastructure.Data;
using CleverBudget.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CleverBudget.Tests.Services;

public class BudgetServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly BudgetService _service;
    private readonly string _testUserId;
    private readonly Category _testCategory;

    public BudgetServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);

        _service = new BudgetService(_context);

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
    public async Task CreateAsync_ValidBudget_ReturnsDto()
    {
        var dto = new CreateBudgetDto
        {
            CategoryId = _testCategory.Id,
            Amount = 1000m,
            Month = DateTime.UtcNow.Month,
            Year = DateTime.UtcNow.Year
        };

        var result = await _service.CreateAsync(dto, _testUserId);

        Assert.NotNull(result);
        Assert.Equal(dto.Amount, result.Amount);
        Assert.Equal(dto.Month, result.Month);
        Assert.Equal(dto.Year, result.Year);
        Assert.Equal(_testCategory.Name, result.CategoryName);
        Assert.True(result.AlertAt50Percent);
        Assert.True(result.AlertAt80Percent);
        Assert.True(result.AlertAt100Percent);
    }

    [Fact]
    public async Task CreateAsync_InvalidCategoryId_ReturnsNull()
    {
        var dto = new CreateBudgetDto
        {
            CategoryId = 9999,
            Amount = 1000m,
            Month = DateTime.UtcNow.Month,
            Year = DateTime.UtcNow.Year
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

        var dto = new CreateBudgetDto
        {
            CategoryId = otherUserCategory.Id,
            Amount = 1000m,
            Month = DateTime.UtcNow.Month,
            Year = DateTime.UtcNow.Year
        };

        var result = await _service.CreateAsync(dto, _testUserId);

        Assert.Null(result);
    }

    [Fact]
    public async Task CreateAsync_DuplicateBudgetForCategoryAndPeriod_ReturnsNull()
    {
        var month = DateTime.UtcNow.Month;
        var year = DateTime.UtcNow.Year;

        var existingBudget = new Budget
        {
            UserId = _testUserId,
            CategoryId = _testCategory.Id,
            Amount = 500m,
            Month = month,
            Year = year,
            CreatedAt = DateTime.UtcNow
        };
        _context.Budgets.Add(existingBudget);
        await _context.SaveChangesAsync();

        var dto = new CreateBudgetDto
        {
            CategoryId = _testCategory.Id,
            Amount = 1000m,
            Month = month,
            Year = year
        };

        var result = await _service.CreateAsync(dto, _testUserId);

        Assert.Null(result);
    }

    [Fact]
    public async Task CreateAsync_WithCustomAlertSettings_SetsCorrectly()
    {
        var dto = new CreateBudgetDto
        {
            CategoryId = _testCategory.Id,
            Amount = 1000m,
            Month = DateTime.UtcNow.Month,
            Year = DateTime.UtcNow.Year,
            AlertAt50Percent = false,
            AlertAt80Percent = true,
            AlertAt100Percent = false
        };

        var result = await _service.CreateAsync(dto, _testUserId);

        Assert.NotNull(result);
        Assert.False(result.AlertAt50Percent);
        Assert.True(result.AlertAt80Percent);
        Assert.False(result.AlertAt100Percent);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDto()
    {
        var budget = new Budget
        {
            UserId = _testUserId,
            CategoryId = _testCategory.Id,
            Amount = 1500m,
            Month = DateTime.UtcNow.Month,
            Year = DateTime.UtcNow.Year,
            CreatedAt = DateTime.UtcNow
        };
        _context.Budgets.Add(budget);
        await _context.SaveChangesAsync();

        var result = await _service.GetByIdAsync(budget.Id, _testUserId);

        Assert.NotNull(result);
        Assert.Equal(budget.Id, result.Id);
        Assert.Equal(budget.Amount, result.Amount);
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
        var budget = new Budget
        {
            UserId = otherUserId,
            CategoryId = _testCategory.Id,
            Amount = 1000m,
            Month = DateTime.UtcNow.Month,
            Year = DateTime.UtcNow.Year,
            CreatedAt = DateTime.UtcNow
        };
        _context.Budgets.Add(budget);
        await _context.SaveChangesAsync();

        var result = await _service.GetByIdAsync(budget.Id, _testUserId);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByCategoryAndPeriodAsync_ExistingBudget_ReturnsDto()
    {
        var month = DateTime.UtcNow.Month;
        var year = DateTime.UtcNow.Year;

        var budget = new Budget
        {
            UserId = _testUserId,
            CategoryId = _testCategory.Id,
            Amount = 2000m,
            Month = month,
            Year = year,
            CreatedAt = DateTime.UtcNow
        };
        _context.Budgets.Add(budget);
        await _context.SaveChangesAsync();

        var result = await _service.GetByCategoryAndPeriodAsync(_testCategory.Id, month, year, _testUserId);

        Assert.NotNull(result);
        Assert.Equal(budget.Amount, result.Amount);
    }

    [Fact]
    public async Task GetByCategoryAndPeriodAsync_NonExisting_ReturnsNull()
    {
        var result = await _service.GetByCategoryAndPeriodAsync(_testCategory.Id, 12, 2025, _testUserId);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsOnlyUserBudgets()
    {
        var otherUserId = Guid.NewGuid().ToString();

        _context.Budgets.AddRange(
            new Budget
            {
                UserId = _testUserId,
                CategoryId = _testCategory.Id,
                Amount = 1000m,
                Month = 1,
                Year = 2025,
                CreatedAt = DateTime.UtcNow
            },
            new Budget
            {
                UserId = _testUserId,
                CategoryId = _testCategory.Id,
                Amount = 1500m,
                Month = 2,
                Year = 2025,
                CreatedAt = DateTime.UtcNow
            },
            new Budget
            {
                UserId = otherUserId,
                CategoryId = _testCategory.Id,
                Amount = 2000m,
                Month = 1,
                Year = 2025,
                CreatedAt = DateTime.UtcNow
            }
        );
        await _context.SaveChangesAsync();

        var result = await _service.GetAllAsync(_testUserId);

        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task GetAllAsync_WithYearFilter_ReturnsFilteredResults()
    {
        _context.Budgets.AddRange(
            new Budget
            {
                UserId = _testUserId,
                CategoryId = _testCategory.Id,
                Amount = 1000m,
                Month = 1,
                Year = 2025,
                CreatedAt = DateTime.UtcNow
            },
            new Budget
            {
                UserId = _testUserId,
                CategoryId = _testCategory.Id,
                Amount = 1500m,
                Month = 1,
                Year = 2024,
                CreatedAt = DateTime.UtcNow
            }
        );
        await _context.SaveChangesAsync();

        var result = await _service.GetAllAsync(_testUserId, year: 2025);

        Assert.Single(result);
        Assert.All(result, b => Assert.Equal(2025, b.Year));
    }

    [Fact]
    public async Task GetAllAsync_WithMonthFilter_ReturnsFilteredResults()
    {
        _context.Budgets.AddRange(
            new Budget
            {
                UserId = _testUserId,
                CategoryId = _testCategory.Id,
                Amount = 1000m,
                Month = 1,
                Year = 2025,
                CreatedAt = DateTime.UtcNow
            },
            new Budget
            {
                UserId = _testUserId,
                CategoryId = _testCategory.Id,
                Amount = 1500m,
                Month = 2,
                Year = 2025,
                CreatedAt = DateTime.UtcNow
            }
        );
        await _context.SaveChangesAsync();

        var result = await _service.GetAllAsync(_testUserId, month: 1);

        Assert.Single(result);
        Assert.All(result, b => Assert.Equal(1, b.Month));
    }

    [Fact]
    public async Task GetPagedAsync_ReturnsCorrectPage()
    {
        for (int i = 1; i <= 15; i++)
        {
            _context.Budgets.Add(new Budget
            {
                UserId = _testUserId,
                CategoryId = _testCategory.Id,
                Amount = i * 100m,
                Month = i % 12 + 1,
                Year = 2025,
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

        Assert.Equal(5, result.Items.Count);
        Assert.Equal(15, result.TotalCount);
        Assert.Equal(3, result.TotalPages);
        Assert.Equal(2, result.Page);
    }

    [Fact]
    public async Task GetPagedAsync_WithSortByAmount_ReturnsSorted()
    {
        _context.Budgets.AddRange(
            new Budget
            {
                UserId = _testUserId,
                CategoryId = _testCategory.Id,
                Amount = 500m,
                Month = 1,
                Year = 2025,
                CreatedAt = DateTime.UtcNow
            },
            new Budget
            {
                UserId = _testUserId,
                CategoryId = _testCategory.Id,
                Amount = 1500m,
                Month = 2,
                Year = 2025,
                CreatedAt = DateTime.UtcNow
            },
            new Budget
            {
                UserId = _testUserId,
                CategoryId = _testCategory.Id,
                Amount = 1000m,
                Month = 3,
                Year = 2025,
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

        var amounts = result.Items.Select(b => b.Amount).ToList();
        Assert.Equal(500m, amounts[0]);
        Assert.Equal(1000m, amounts[1]);
        Assert.Equal(1500m, amounts[2]);
    }

    [Fact]
    public async Task UpdateAsync_ValidUpdate_UpdatesAndReturnsDto()
    {
        var budget = new Budget
        {
            UserId = _testUserId,
            CategoryId = _testCategory.Id,
            Amount = 1000m,
            Month = DateTime.UtcNow.Month,
            Year = DateTime.UtcNow.Year,
            AlertAt50Percent = true,
            AlertAt80Percent = true,
            AlertAt100Percent = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.Budgets.Add(budget);
        await _context.SaveChangesAsync();

        var updateDto = new UpdateBudgetDto
        {
            Amount = 1500m,
            AlertAt50Percent = false
        };

        var result = await _service.UpdateAsync(budget.Id, updateDto, _testUserId);

        Assert.NotNull(result);
        Assert.Equal(1500m, result.Amount);
        Assert.False(result.AlertAt50Percent);
        Assert.True(result.AlertAt80Percent);
    }

    [Fact]
    public async Task UpdateAsync_PartialUpdate_UpdatesOnlyProvidedFields()
    {
        var budget = new Budget
        {
            UserId = _testUserId,
            CategoryId = _testCategory.Id,
            Amount = 1000m,
            Month = DateTime.UtcNow.Month,
            Year = DateTime.UtcNow.Year,
            AlertAt100Percent = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.Budgets.Add(budget);
        await _context.SaveChangesAsync();

        var updateDto = new UpdateBudgetDto
        {
            Amount = 2000m
        };

        var result = await _service.UpdateAsync(budget.Id, updateDto, _testUserId);

        Assert.NotNull(result);
        Assert.Equal(2000m, result.Amount);
        Assert.True(result.AlertAt100Percent);
    }

    [Fact]
    public async Task UpdateAsync_NonExistingId_ReturnsNull()
    {
        var updateDto = new UpdateBudgetDto
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
        var budget = new Budget
        {
            UserId = otherUserId,
            CategoryId = _testCategory.Id,
            Amount = 1000m,
            Month = DateTime.UtcNow.Month,
            Year = DateTime.UtcNow.Year,
            CreatedAt = DateTime.UtcNow
        };
        _context.Budgets.Add(budget);
        await _context.SaveChangesAsync();

        var updateDto = new UpdateBudgetDto
        {
            Amount = 2000m
        };

        var result = await _service.UpdateAsync(budget.Id, updateDto, _testUserId);

        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteAsync_ExistingId_DeletesAndReturnsTrue()
    {
        var budget = new Budget
        {
            UserId = _testUserId,
            CategoryId = _testCategory.Id,
            Amount = 1000m,
            Month = DateTime.UtcNow.Month,
            Year = DateTime.UtcNow.Year,
            CreatedAt = DateTime.UtcNow
        };
        _context.Budgets.Add(budget);
        await _context.SaveChangesAsync();

        var result = await _service.DeleteAsync(budget.Id, _testUserId);

        Assert.True(result);
        var deleted = await _context.Budgets.FindAsync(budget.Id);
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
        var budget = new Budget
        {
            UserId = otherUserId,
            CategoryId = _testCategory.Id,
            Amount = 1000m,
            Month = DateTime.UtcNow.Month,
            Year = DateTime.UtcNow.Year,
            CreatedAt = DateTime.UtcNow
        };
        _context.Budgets.Add(budget);
        await _context.SaveChangesAsync();

        var result = await _service.DeleteAsync(budget.Id, _testUserId);

        Assert.False(result);
    }

    [Fact]
    public async Task GetCurrentMonthBudgetsAsync_ReturnsOnlyCurrentMonth()
    {
        var now = DateTime.UtcNow;
        _context.Budgets.AddRange(
            new Budget
            {
                UserId = _testUserId,
                CategoryId = _testCategory.Id,
                Amount = 1000m,
                Month = now.Month,
                Year = now.Year,
                CreatedAt = DateTime.UtcNow
            },
            new Budget
            {
                UserId = _testUserId,
                CategoryId = _testCategory.Id,
                Amount = 1500m,
                Month = now.Month == 1 ? 12 : now.Month - 1,
                Year = now.Month == 1 ? now.Year - 1 : now.Year,
                CreatedAt = DateTime.UtcNow
            }
        );
        await _context.SaveChangesAsync();

        var result = await _service.GetCurrentMonthBudgetsAsync(_testUserId);

        Assert.Single(result);
        Assert.All(result, b =>
        {
            Assert.Equal(now.Month, b.Month);
            Assert.Equal(now.Year, b.Year);
        });
    }

    [Fact]
    public async Task GetTotalBudgetForMonthAsync_ReturnsSumOfBudgets()
    {
        _context.Budgets.AddRange(
            new Budget
            {
                UserId = _testUserId,
                CategoryId = _testCategory.Id,
                Amount = 1000m,
                Month = 5,
                Year = 2025,
                CreatedAt = DateTime.UtcNow
            },
            new Budget
            {
                UserId = _testUserId,
                CategoryId = _testCategory.Id,
                Amount = 1500m,
                Month = 5,
                Year = 2025,
                CreatedAt = DateTime.UtcNow
            },
            new Budget
            {
                UserId = _testUserId,
                CategoryId = _testCategory.Id,
                Amount = 500m,
                Month = 6,
                Year = 2025,
                CreatedAt = DateTime.UtcNow
            }
        );
        await _context.SaveChangesAsync();

        var result = await _service.GetTotalBudgetForMonthAsync(_testUserId, 5, 2025);

        Assert.Equal(2500m, result);
    }

    [Fact]
    public async Task GetTotalSpentForMonthAsync_ReturnsSumOfExpenses()
    {
        var targetDate = new DateTime(2025, 5, 15);

        _context.Transactions.AddRange(
            new Transaction
            {
                UserId = _testUserId,
                CategoryId = _testCategory.Id,
                Amount = 300m,
                Type = TransactionType.Expense,
                Description = "Despesa 1",
                Date = targetDate,
                CreatedAt = DateTime.UtcNow
            },
            new Transaction
            {
                UserId = _testUserId,
                CategoryId = _testCategory.Id,
                Amount = 200m,
                Type = TransactionType.Expense,
                Description = "Despesa 2",
                Date = targetDate.AddDays(5),
                CreatedAt = DateTime.UtcNow
            },
            new Transaction
            {
                UserId = _testUserId,
                CategoryId = _testCategory.Id,
                Amount = 1000m,
                Type = TransactionType.Income,
                Description = "Receita (não deve contar)",
                Date = targetDate,
                CreatedAt = DateTime.UtcNow
            }
        );
        await _context.SaveChangesAsync();

        var result = await _service.GetTotalSpentForMonthAsync(_testUserId, 5, 2025);

        Assert.Equal(500m, result);
    }

    [Fact]
    public async Task GetByIdAsync_CalculatesSpentAndRemainingCorrectly()
    {
        var budget = new Budget
        {
            UserId = _testUserId,
            CategoryId = _testCategory.Id,
            Amount = 1000m,
            Month = 5,
            Year = 2025,
            CreatedAt = DateTime.UtcNow
        };
        _context.Budgets.Add(budget);

        _context.Transactions.Add(new Transaction
        {
            UserId = _testUserId,
            CategoryId = _testCategory.Id,
            Amount = 300m,
            Type = TransactionType.Expense,
            Description = "Despesa",
            Date = new DateTime(2025, 5, 15),
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        var result = await _service.GetByIdAsync(budget.Id, _testUserId);

        Assert.NotNull(result);
        Assert.Equal(300m, result.Spent);
        Assert.Equal(700m, result.Remaining);
        Assert.Equal(30m, result.PercentageUsed);
    }

    [Fact]
    public async Task GetByIdAsync_SetsStatusCorrectly()
    {
        var budgets = new List<(decimal spent, string expectedStatus)>
        {
            (100m, "Normal"),
            (500m, "Alerta"),
            (900m, "Crítico"),
            (1000m, "Excedido"),
            (1200m, "Excedido")
        };

        foreach (var (spent, expectedStatus) in budgets)
        {
            var budget = new Budget
            {
                UserId = _testUserId,
                CategoryId = _testCategory.Id,
                Amount = 1000m,
                Month = DateTime.UtcNow.Month,
                Year = DateTime.UtcNow.Year,
                CreatedAt = DateTime.UtcNow
            };
            _context.Budgets.Add(budget);
            await _context.SaveChangesAsync();

            if (spent > 0)
            {
                _context.Transactions.Add(new Transaction
                {
                    UserId = _testUserId,
                    CategoryId = _testCategory.Id,
                    Amount = spent,
                    Type = TransactionType.Expense,
                    Description = "Test",
                    Date = new DateTime(budget.Year, budget.Month, 15),
                    CreatedAt = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();
            }

            var result = await _service.GetByIdAsync(budget.Id, _testUserId);

            Assert.NotNull(result);
            Assert.Equal(expectedStatus, result.Status);

            if (spent > 0)
            {
                var transaction = await _context.Transactions.FirstAsync(t => t.Description == "Test");
                _context.Transactions.Remove(transaction);
            }
            
            _context.Budgets.Remove(budget);
            await _context.SaveChangesAsync();
        }
    }

    [Fact]
    public async Task GetByIdAsync_IncludesCategoryDetails()
    {
        var budget = new Budget
        {
            UserId = _testUserId,
            CategoryId = _testCategory.Id,
            Amount = 1000m,
            Month = DateTime.UtcNow.Month,
            Year = DateTime.UtcNow.Year,
            CreatedAt = DateTime.UtcNow
        };
        _context.Budgets.Add(budget);
        await _context.SaveChangesAsync();

        var result = await _service.GetByIdAsync(budget.Id, _testUserId);

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
