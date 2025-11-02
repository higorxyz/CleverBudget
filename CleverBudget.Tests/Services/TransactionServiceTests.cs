using CleverBudget.Core.Common;
using CleverBudget.Core.DTOs;
using CleverBudget.Core.Entities;
using CleverBudget.Core.Enums;
using CleverBudget.Infrastructure.Data;
using CleverBudget.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CleverBudget.Tests.Services;

public class TransactionServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly TransactionService _transactionService;
    private readonly string _testUserId;
    private readonly Category _testCategory;

    public TransactionServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);

        _transactionService = new TransactionService(_context);


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
    public async Task CreateAsync_ValidTransaction_ReturnsTransactionResponse()
    {

        var dto = new CreateTransactionDto
        {
            Amount = 150.50m,
            Type = TransactionType.Expense,
            Description = "Almoço no restaurante",
            CategoryId = _testCategory.Id,
            Date = DateTime.Now.AddDays(-1)
        };


        var result = await _transactionService.CreateAsync(dto, _testUserId);


        Assert.NotNull(result);
        Assert.Equal(dto.Amount, result.Amount);
        Assert.Equal(dto.Type, result.Type);
        Assert.Equal(dto.Description, result.Description);
        Assert.Equal(_testCategory.Name, result.CategoryName);
        Assert.True(result.Id > 0);
    }

    [Fact]
    public async Task CreateAsync_InvalidCategoryId_ReturnsNull()
    {

        var dto = new CreateTransactionDto
        {
            Amount = 100m,
            Type = TransactionType.Expense,
            Description = "Teste",
            CategoryId = 9999, // Não existe
            Date = DateTime.Now
        };


        var result = await _transactionService.CreateAsync(dto, _testUserId);


        Assert.Null(result);
    }

    [Fact]
    public async Task CreateAsync_CategoryFromAnotherUser_ReturnsNull()
    {

        var otherUserCategory = new Category
        {
            Id = 2,
            UserId = Guid.NewGuid().ToString(), // Outro usuário
            Name = "Categoria Alheia",
            CreatedAt = DateTime.UtcNow
        };
        _context.Categories.Add(otherUserCategory);
        await _context.SaveChangesAsync();

        var dto = new CreateTransactionDto
        {
            Amount = 100m,
            Type = TransactionType.Expense,
            Description = "Tentativa de uso de categoria alheia",
            CategoryId = otherUserCategory.Id,
            Date = DateTime.Now
        };


        var result = await _transactionService.CreateAsync(dto, _testUserId);


        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingTransaction_ReturnsTransaction()
    {

        var transaction = new Transaction
        {
            UserId = _testUserId,
            Amount = 200m,
            Type = TransactionType.Income,
            Description = "Salário",
            CategoryId = _testCategory.Id,
            Date = DateTime.Now,
            CreatedAt = DateTime.UtcNow
        };
        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync();


        var result = await _transactionService.GetByIdAsync(transaction.Id, _testUserId);


        Assert.NotNull(result);
        Assert.Equal(transaction.Id, result.Id);
        Assert.Equal(transaction.Amount, result.Amount);
    }

    [Fact]
    public async Task GetByIdAsync_TransactionFromAnotherUser_ReturnsNull()
    {

        var otherUserId = Guid.NewGuid().ToString();
        var transaction = new Transaction
        {
            UserId = otherUserId,
            Amount = 100m,
            Type = TransactionType.Expense,
            Description = "Transação de outro usuário",
            CategoryId = _testCategory.Id,
            Date = DateTime.Now,
            CreatedAt = DateTime.UtcNow
        };
        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync();


        var result = await _transactionService.GetByIdAsync(transaction.Id, _testUserId);


        Assert.Null(result);
    }

    [Fact]
    public async Task GetPagedAsync_ReturnsPagedResult()
    {

        for (int i = 1; i <= 25; i++)
        {
            _context.Transactions.Add(new Transaction
            {
                UserId = _testUserId,
                Amount = i * 10,
                Type = i % 2 == 0 ? TransactionType.Income : TransactionType.Expense,
                Description = $"Transação {i}",
                CategoryId = _testCategory.Id,
                Date = DateTime.Now.AddDays(-i),
                CreatedAt = DateTime.UtcNow
            });
        }
        await _context.SaveChangesAsync();

        var paginationParams = new PaginationParams
        {
            Page = 1,
            PageSize = 10
        };


        var result = await _transactionService.GetPagedAsync(_testUserId, paginationParams);


        Assert.NotNull(result);
        Assert.Equal(10, result.Items.Count);
        Assert.Equal(25, result.TotalCount);
        Assert.Equal(3, result.TotalPages);
        Assert.True(result.HasNextPage);
        Assert.False(result.HasPreviousPage);
    }

    [Fact]
    public async Task GetPagedAsync_WithFilters_ReturnsFilteredResults()
    {

        _context.Transactions.AddRange(
            new Transaction
            {
                UserId = _testUserId,
                Amount = 100m,
                Type = TransactionType.Expense,
                Description = "Despesa 1",
                CategoryId = _testCategory.Id,
                Date = DateTime.Now.AddDays(-5),
                CreatedAt = DateTime.UtcNow
            },
            new Transaction
            {
                UserId = _testUserId,
                Amount = 500m,
                Type = TransactionType.Income,
                Description = "Receita 1",
                CategoryId = _testCategory.Id,
                Date = DateTime.Now.AddDays(-3),
                CreatedAt = DateTime.UtcNow
            },
            new Transaction
            {
                UserId = _testUserId,
                Amount = 200m,
                Type = TransactionType.Expense,
                Description = "Despesa 2",
                CategoryId = _testCategory.Id,
                Date = DateTime.Now.AddDays(-1),
                CreatedAt = DateTime.UtcNow
            }
        );
        await _context.SaveChangesAsync();

        var paginationParams = new PaginationParams { Page = 1, PageSize = 10 };


        var result = await _transactionService.GetPagedAsync(
            _testUserId, 
            paginationParams, 
            type: TransactionType.Expense
        );


        Assert.Equal(2, result.TotalCount);
        Assert.All(result.Items, t => Assert.Equal(TransactionType.Expense, t.Type));
    }

    [Fact]
    public async Task UpdateAsync_ValidData_UpdatesTransaction()
    {

        var transaction = new Transaction
        {
            UserId = _testUserId,
            Amount = 100m,
            Type = TransactionType.Expense,
            Description = "Descrição Original",
            CategoryId = _testCategory.Id,
            Date = DateTime.Now.AddDays(-1),
            CreatedAt = DateTime.UtcNow
        };
        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync();

        var updateDto = new UpdateTransactionDto
        {
            Amount = 150m,
            Description = "Descrição Atualizada"
        };


        var result = await _transactionService.UpdateAsync(transaction.Id, updateDto, _testUserId);


        Assert.NotNull(result);
        Assert.Equal(updateDto.Amount, result.Amount);
        Assert.Equal(updateDto.Description, result.Description);
    }

    [Fact]
    public async Task UpdateAsync_TransactionNotFound_ReturnsNull()
    {

        var updateDto = new UpdateTransactionDto
        {
            Amount = 150m
        };


        var result = await _transactionService.UpdateAsync(9999, updateDto, _testUserId);


        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteAsync_ExistingTransaction_ReturnsTrue()
    {

        var transaction = new Transaction
        {
            UserId = _testUserId,
            Amount = 100m,
            Type = TransactionType.Expense,
            Description = "Para deletar",
            CategoryId = _testCategory.Id,
            Date = DateTime.Now,
            CreatedAt = DateTime.UtcNow
        };
        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync();


        var result = await _transactionService.DeleteAsync(transaction.Id, _testUserId);


        Assert.True(result);
        var deletedTransaction = await _context.Transactions.FindAsync(transaction.Id);
        Assert.Null(deletedTransaction);
    }

    [Fact]
    public async Task DeleteAsync_TransactionNotFound_ReturnsFalse()
    {

        var result = await _transactionService.DeleteAsync(9999, _testUserId);


        Assert.False(result);
    }

    [Fact]
    public async Task GetPagedAsync_SortByAmount_ReturnsSortedResults()
    {

        _context.Transactions.AddRange(
            new Transaction { UserId = _testUserId, Amount = 100m, Type = TransactionType.Expense, Description = "A", CategoryId = _testCategory.Id, Date = DateTime.Now, CreatedAt = DateTime.UtcNow },
            new Transaction { UserId = _testUserId, Amount = 300m, Type = TransactionType.Expense, Description = "C", CategoryId = _testCategory.Id, Date = DateTime.Now, CreatedAt = DateTime.UtcNow },
            new Transaction { UserId = _testUserId, Amount = 200m, Type = TransactionType.Expense, Description = "B", CategoryId = _testCategory.Id, Date = DateTime.Now, CreatedAt = DateTime.UtcNow }
        );
        await _context.SaveChangesAsync();

        var paginationParams = new PaginationParams
        {
            Page = 1,
            PageSize = 10,
            SortBy = "amount",
            SortOrder = "asc"
        };


        var result = await _transactionService.GetPagedAsync(_testUserId, paginationParams);


        Assert.Equal(100m, result.Items[0].Amount);
        Assert.Equal(200m, result.Items[1].Amount);
        Assert.Equal(300m, result.Items[2].Amount);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
