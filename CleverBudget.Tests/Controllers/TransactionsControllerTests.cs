using CleverBudget.Api.Controllers;
using CleverBudget.Core.Common;
using CleverBudget.Core.DTOs;
using CleverBudget.Core.Enums;
using CleverBudget.Core.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using Xunit;

namespace CleverBudget.Tests.Controllers;

public class TransactionsControllerTests
{
    private readonly Mock<ITransactionService> _transactionServiceMock;
    private readonly TransactionsController _controller;
    private const string UserId = "test-user-id";

    public TransactionsControllerTests()
    {
        _transactionServiceMock = new Mock<ITransactionService>();
        _controller = new TransactionsController(_transactionServiceMock.Object);

        // Setup User Claims
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, UserId)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };
    }

    [Fact]
    public async Task GetAll_ReturnsPagedResult()
    {
        // Arrange
        var pagedResult = new PagedResult<TransactionResponseDto>
        {
            Items = new List<TransactionResponseDto>
            {
                new TransactionResponseDto { Id = 1, Amount = 100, Description = "Test" }
            },
            Page = 1,
            PageSize = 10,
            TotalCount = 1
        };

        _transactionServiceMock
            .Setup(s => s.GetPagedAsync(
                UserId,
                It.IsAny<PaginationParams>(),
                null,
                null,
                null,
                null))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _controller.GetAll();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedResult = Assert.IsType<PagedResult<TransactionResponseDto>>(okResult.Value);
        Assert.Single(returnedResult.Items);
    }

    [Fact]
    public async Task GetAll_WithTypeFilter_ReturnsFilteredResults()
    {
        // Arrange
        var pagedResult = new PagedResult<TransactionResponseDto>
        {
            Items = new List<TransactionResponseDto>
            {
                new TransactionResponseDto { Id = 1, Type = TransactionType.Expense }
            },
            Page = 1,
            PageSize = 10,
            TotalCount = 1
        };

        _transactionServiceMock
            .Setup(s => s.GetPagedAsync(
                UserId,
                It.IsAny<PaginationParams>(),
                TransactionType.Expense,
                null,
                null,
                null))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _controller.GetAll(type: TransactionType.Expense);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedResult = Assert.IsType<PagedResult<TransactionResponseDto>>(okResult.Value);
        Assert.Equal(TransactionType.Expense, returnedResult.Items.First().Type);
    }

    [Fact]
    public async Task GetAll_WithDateRange_PassesCorrectParameters()
    {
        // Arrange
        var startDate = new DateTime(2025, 1, 1);
        var endDate = new DateTime(2025, 1, 31);
        var pagedResult = new PagedResult<TransactionResponseDto>
        {
            Items = new List<TransactionResponseDto>(),
            Page = 1,
            PageSize = 10,
            TotalCount = 0
        };

        _transactionServiceMock
            .Setup(s => s.GetPagedAsync(
                UserId,
                It.IsAny<PaginationParams>(),
                null,
                null,
                startDate,
                endDate))
            .ReturnsAsync(pagedResult);

        // Act
        await _controller.GetAll(startDate: startDate, endDate: endDate);

        // Assert
        _transactionServiceMock.Verify(s => s.GetPagedAsync(
            UserId,
            It.IsAny<PaginationParams>(),
            null,
            null,
            startDate,
            endDate), Times.Once);
    }

    [Fact]
    public async Task GetById_ExistingTransaction_ReturnsOk()
    {
        // Arrange
        var transaction = new TransactionResponseDto
        {
            Id = 1,
            Amount = 100,
            Description = "Test Transaction"
        };

        _transactionServiceMock
            .Setup(s => s.GetByIdAsync(1, UserId))
            .ReturnsAsync(transaction);

        // Act
        var result = await _controller.GetById(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedTransaction = Assert.IsType<TransactionResponseDto>(okResult.Value);
        Assert.Equal(1, returnedTransaction.Id);
    }

    [Fact]
    public async Task GetById_NonExistingTransaction_ReturnsNotFound()
    {
        // Arrange
        _transactionServiceMock
            .Setup(s => s.GetByIdAsync(999, UserId))
            .ReturnsAsync((TransactionResponseDto?)null);

        // Act
        var result = await _controller.GetById(999);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.NotNull(notFoundResult.Value);
    }

    [Fact]
    public async Task Create_ValidTransaction_ReturnsCreatedAtAction()
    {
        // Arrange
        var createDto = new CreateTransactionDto
        {
            Amount = 100,
            Description = "Test",
            CategoryId = 1,
            Type = TransactionType.Expense,
            Date = DateTime.UtcNow
        };

        var createdTransaction = new TransactionResponseDto
        {
            Id = 1,
            Amount = 100,
            Description = "Test"
        };

        _transactionServiceMock
            .Setup(s => s.CreateAsync(createDto, UserId))
            .ReturnsAsync(createdTransaction);

        // Act
        var result = await _controller.Create(createDto);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(TransactionsController.GetById), createdResult.ActionName);
        var returnedTransaction = Assert.IsType<TransactionResponseDto>(createdResult.Value);
        Assert.Equal(1, returnedTransaction.Id);
    }

    [Fact]
    public async Task Create_InvalidCategory_ReturnsBadRequest()
    {
        // Arrange
        var createDto = new CreateTransactionDto
        {
            Amount = 100,
            Description = "Test",
            CategoryId = 999,
            Type = TransactionType.Expense,
            Date = DateTime.UtcNow
        };

        _transactionServiceMock
            .Setup(s => s.CreateAsync(createDto, UserId))
            .ReturnsAsync((TransactionResponseDto?)null);

        // Act
        var result = await _controller.Create(createDto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public async Task Update_ExistingTransaction_ReturnsOk()
    {
        // Arrange
        var updateDto = new UpdateTransactionDto
        {
            Amount = 200,
            Description = "Updated"
        };

        var updatedTransaction = new TransactionResponseDto
        {
            Id = 1,
            Amount = 200,
            Description = "Updated"
        };

        _transactionServiceMock
            .Setup(s => s.UpdateAsync(1, updateDto, UserId))
            .ReturnsAsync(updatedTransaction);

        // Act
        var result = await _controller.Update(1, updateDto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedTransaction = Assert.IsType<TransactionResponseDto>(okResult.Value);
        Assert.Equal(200, returnedTransaction.Amount);
    }

    [Fact]
    public async Task Update_NonExistingTransaction_ReturnsNotFound()
    {
        // Arrange
        var updateDto = new UpdateTransactionDto { Amount = 200 };

        _transactionServiceMock
            .Setup(s => s.UpdateAsync(999, updateDto, UserId))
            .ReturnsAsync((TransactionResponseDto?)null);

        // Act
        var result = await _controller.Update(999, updateDto);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Delete_ExistingTransaction_ReturnsNoContent()
    {
        // Arrange
        _transactionServiceMock
            .Setup(s => s.DeleteAsync(1, UserId))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.Delete(1);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Delete_NonExistingTransaction_ReturnsNotFound()
    {
        // Arrange
        _transactionServiceMock
            .Setup(s => s.DeleteAsync(999, UserId))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.Delete(999);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetAll_WithCategoryFilter_PassesCorrectParameter()
    {
        // Arrange
        var pagedResult = new PagedResult<TransactionResponseDto>
        {
            Items = new List<TransactionResponseDto>(),
            Page = 1,
            PageSize = 10,
            TotalCount = 0
        };

        _transactionServiceMock
            .Setup(s => s.GetPagedAsync(
                UserId,
                It.IsAny<PaginationParams>(),
                null,
                5,
                null,
                null))
            .ReturnsAsync(pagedResult);

        // Act
        await _controller.GetAll(categoryId: 5);

        // Assert
        _transactionServiceMock.Verify(s => s.GetPagedAsync(
            UserId,
            It.IsAny<PaginationParams>(),
            null,
            5,
            null,
            null), Times.Once);
    }

    [Fact]
    public async Task GetAll_WithCustomPaginationParams_UsesProvidedValues()
    {
        // Arrange
        var pagedResult = new PagedResult<TransactionResponseDto>
        {
            Items = new List<TransactionResponseDto>(),
            Page = 2,
            PageSize = 20,
            TotalCount = 0
        };

        _transactionServiceMock
            .Setup(s => s.GetPagedAsync(
                UserId,
                It.Is<PaginationParams>(p => p.Page == 2 && p.PageSize == 20),
                null,
                null,
                null,
                null))
            .ReturnsAsync(pagedResult);

        // Act
        await _controller.GetAll(page: 2, pageSize: 20);

        // Assert
        _transactionServiceMock.Verify(s => s.GetPagedAsync(
            UserId,
            It.Is<PaginationParams>(p => p.Page == 2 && p.PageSize == 20 && p.SortBy == "date" && p.SortOrder == "desc"),
            null,
            null,
            null,
            null), Times.Once);
    }
}
