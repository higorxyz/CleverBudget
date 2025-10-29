using CleverBudget.Core.Common;
using CleverBudget.Core.DTOs;
using CleverBudget.Core.Entities;
using CleverBudget.Core.Enums;
using CleverBudget.Infrastructure.Data;
using CleverBudget.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CleverBudget.Tests.Services;

public class CategoryServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly CategoryService _categoryService;
    private readonly string _testUserId;

    public CategoryServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);

        _categoryService = new CategoryService(_context);
        _testUserId = Guid.NewGuid().ToString();
    }

    [Fact]
    public async Task CreateAsync_ValidCategory_ReturnsCategoryResponse()
    {
        // Arrange
        var dto = new CreateCategoryDto
        {
            Name = "Eletrônicos",
            Icon = "💻",
            Color = "#3498db"
        };

        // Act
        var result = await _categoryService.CreateAsync(dto, _testUserId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(dto.Name, result.Name);
        Assert.Equal(dto.Icon, result.Icon);
        Assert.Equal(dto.Color, result.Color);
        Assert.False(result.IsDefault);
        Assert.True(result.Id > 0);
    }

    [Fact]
    public async Task CreateAsync_DuplicateName_ReturnsNull()
    {
        // Arrange
        var existingCategory = new Category
        {
            UserId = _testUserId,
            Name = "Alimentação",
            IsDefault = false,
            CreatedAt = DateTime.UtcNow
        };
        _context.Categories.Add(existingCategory);
        await _context.SaveChangesAsync();

        var dto = new CreateCategoryDto
        {
            Name = "alimentação", // Case insensitive
            Icon = "🍔"
        };

        // Act
        var result = await _categoryService.CreateAsync(dto, _testUserId);

        // Assert
        Assert.Null(result); // Não permite duplicata
    }

    [Fact]
    public async Task GetByIdAsync_ExistingCategory_ReturnsCategory()
    {
        // Arrange
        var category = new Category
        {
            UserId = _testUserId,
            Name = "Transporte",
            Icon = "🚗",
            Color = "#e74c3c",
            IsDefault = false,
            CreatedAt = DateTime.UtcNow
        };
        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        // Act
        var result = await _categoryService.GetByIdAsync(category.Id, _testUserId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(category.Id, result.Id);
        Assert.Equal(category.Name, result.Name);
    }

    [Fact]
    public async Task GetByIdAsync_CategoryFromAnotherUser_ReturnsNull()
    {
        // Arrange
        var otherUserId = Guid.NewGuid().ToString();
        var category = new Category
        {
            UserId = otherUserId,
            Name = "Categoria Alheia",
            CreatedAt = DateTime.UtcNow
        };
        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        // Act
        var result = await _categoryService.GetByIdAsync(category.Id, _testUserId);

        // Assert
        Assert.Null(result); // Isolamento entre usuários
    }

    [Fact]
    public async Task GetAllAsync_ReturnsUserCategories()
    {
        // Arrange
        var otherUserId = Guid.NewGuid().ToString();
        _context.Categories.AddRange(
            new Category { UserId = _testUserId, Name = "Cat 1", CreatedAt = DateTime.UtcNow },
            new Category { UserId = _testUserId, Name = "Cat 2", CreatedAt = DateTime.UtcNow },
            new Category { UserId = otherUserId, Name = "Cat Outro", CreatedAt = DateTime.UtcNow }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _categoryService.GetAllAsync(_testUserId);

        // Assert
        Assert.Equal(2, result.Count());
        Assert.All(result, c => Assert.NotEqual("Cat Outro", c.Name));
    }

    [Fact]
    public async Task GetPagedAsync_ReturnsPaginatedResults()
    {
        // Arrange
        for (int i = 1; i <= 15; i++)
        {
            _context.Categories.Add(new Category
            {
                UserId = _testUserId,
                Name = $"Categoria {i}",
                CreatedAt = DateTime.UtcNow
            });
        }
        await _context.SaveChangesAsync();

        var paginationParams = new PaginationParams
        {
            Page = 1,
            PageSize = 10
        };

        // Act
        var result = await _categoryService.GetPagedAsync(_testUserId, paginationParams);

        // Assert
        Assert.Equal(10, result.Items.Count);
        Assert.Equal(15, result.TotalCount);
        Assert.Equal(2, result.TotalPages);
        Assert.True(result.HasNextPage);
    }

    [Fact]
    public async Task UpdateAsync_ValidData_UpdatesCategory()
    {
        // Arrange
        var category = new Category
        {
            UserId = _testUserId,
            Name = "Original",
            Icon = "📦",
            IsDefault = false,
            CreatedAt = DateTime.UtcNow
        };
        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        var updateDto = new UpdateCategoryDto
        {
            Name = "Atualizada",
            Icon = "🎁",
            Color = "#ff0000"
        };

        // Act
        var result = await _categoryService.UpdateAsync(category.Id, updateDto, _testUserId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(updateDto.Name, result.Name);
        Assert.Equal(updateDto.Icon, result.Icon);
        Assert.Equal(updateDto.Color, result.Color);
    }

    [Fact]
    public async Task UpdateAsync_DefaultCategory_ReturnsNull()
    {
        // Arrange
        var defaultCategory = new Category
        {
            UserId = _testUserId,
            Name = "Alimentação",
            IsDefault = true, // Categoria padrão
            CreatedAt = DateTime.UtcNow
        };
        _context.Categories.Add(defaultCategory);
        await _context.SaveChangesAsync();

        var updateDto = new UpdateCategoryDto
        {
            Name = "Tentativa de Mudança"
        };

        // Act
        var result = await _categoryService.UpdateAsync(defaultCategory.Id, updateDto, _testUserId);

        // Assert
        Assert.Null(result); // Não permite editar padrão
    }

    [Fact]
    public async Task UpdateAsync_DuplicateName_ReturnsNull()
    {
        // Arrange
        _context.Categories.AddRange(
            new Category { UserId = _testUserId, Name = "Cat A", IsDefault = false, CreatedAt = DateTime.UtcNow },
            new Category { UserId = _testUserId, Name = "Cat B", IsDefault = false, CreatedAt = DateTime.UtcNow }
        );
        await _context.SaveChangesAsync();

        var catA = await _context.Categories.FirstAsync(c => c.Name == "Cat A");
        var updateDto = new UpdateCategoryDto { Name = "Cat B" }; // Nome já existe

        // Act
        var result = await _categoryService.UpdateAsync(catA.Id, updateDto, _testUserId);

        // Assert
        Assert.Null(result); // Não permite duplicata
    }

    [Fact]
    public async Task DeleteAsync_CategoryWithoutTransactions_ReturnsTrue()
    {
        // Arrange
        var category = new Category
        {
            UserId = _testUserId,
            Name = "Para Deletar",
            IsDefault = false,
            CreatedAt = DateTime.UtcNow
        };
        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        // Act
        var result = await _categoryService.DeleteAsync(category.Id, _testUserId);

        // Assert
        Assert.True(result);
        var deleted = await _context.Categories.FindAsync(category.Id);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task DeleteAsync_DefaultCategory_ReturnsFalse()
    {
        // Arrange
        var defaultCategory = new Category
        {
            UserId = _testUserId,
            Name = "Alimentação",
            IsDefault = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.Categories.Add(defaultCategory);
        await _context.SaveChangesAsync();

        // Act
        var result = await _categoryService.DeleteAsync(defaultCategory.Id, _testUserId);

        // Assert
        Assert.False(result); // Não permite deletar padrão
    }

    [Fact]
    public async Task DeleteAsync_CategoryWithTransactions_ReturnsFalse()
    {
        // Arrange
        var category = new Category
        {
            UserId = _testUserId,
            Name = "Com Transações",
            IsDefault = false,
            CreatedAt = DateTime.UtcNow
        };
        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        // Adicionar transação associada
        _context.Transactions.Add(new Transaction
        {
            UserId = _testUserId,
            CategoryId = category.Id,
            Amount = 100m,
            Type = TransactionType.Expense,
            Description = "Teste",
            Date = DateTime.Now,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _categoryService.DeleteAsync(category.Id, _testUserId);

        // Assert
        Assert.False(result); // Não permite deletar com transações
    }

    [Fact]
    public async Task GetPagedAsync_SortByName_ReturnsSorted()
    {
        // Arrange
        _context.Categories.AddRange(
            new Category { UserId = _testUserId, Name = "Zebra", CreatedAt = DateTime.UtcNow },
            new Category { UserId = _testUserId, Name = "Alpha", CreatedAt = DateTime.UtcNow },
            new Category { UserId = _testUserId, Name = "Beta", CreatedAt = DateTime.UtcNow }
        );
        await _context.SaveChangesAsync();

        var paginationParams = new PaginationParams
        {
            Page = 1,
            PageSize = 10,
            SortBy = "name",
            SortOrder = "asc"
        };

        // Act
        var result = await _categoryService.GetPagedAsync(_testUserId, paginationParams);

        // Assert
        Assert.Equal("Alpha", result.Items[0].Name);
        Assert.Equal("Beta", result.Items[1].Name);
        Assert.Equal("Zebra", result.Items[2].Name);
    }

    [Fact]
    public async Task GetPagedAsync_SortByIsDefault_ReturnsSorted()
    {
        // Arrange
        _context.Categories.AddRange(
            new Category { UserId = _testUserId, Name = "Custom", IsDefault = false, CreatedAt = DateTime.UtcNow },
            new Category { UserId = _testUserId, Name = "Default", IsDefault = true, CreatedAt = DateTime.UtcNow }
        );
        await _context.SaveChangesAsync();

        var paginationParams = new PaginationParams
        {
            Page = 1,
            PageSize = 10,
            SortBy = "isdefault",
            SortOrder = "desc"
        };

        // Act
        var result = await _categoryService.GetPagedAsync(_testUserId, paginationParams);

        // Assert
        Assert.True(result.Items[0].IsDefault); // Padrão vem primeiro
        Assert.False(result.Items[1].IsDefault);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}