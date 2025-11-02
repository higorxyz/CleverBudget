using CleverBudget.Api.Controllers;
using CleverBudget.Core.Common;
using CleverBudget.Core.DTOs;
using CleverBudget.Core.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using Xunit;

namespace CleverBudget.Tests.Controllers;

public class RecurringTransactionsControllerTests
{
    private readonly Mock<IRecurringTransactionService> _serviceMock;
    private readonly RecurringTransactionsController _controller;
    private const string UserId = "test-user-id";

    public RecurringTransactionsControllerTests()
    {
        _serviceMock = new Mock<IRecurringTransactionService>();
        _controller = new RecurringTransactionsController(_serviceMock.Object);

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
        var pagedResult = new PagedResult<RecurringTransactionResponseDto>
        {
            Items = new List<RecurringTransactionResponseDto>
            {
                new RecurringTransactionResponseDto { Id = 1, Amount = 100 }
            },
            Page = 1,
            PageSize = 10,
            TotalCount = 1
        };

        _serviceMock
            .Setup(s => s.GetPagedAsync(UserId, It.IsAny<PaginationParams>(), null))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _controller.GetAll();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedResult = Assert.IsType<PagedResult<RecurringTransactionResponseDto>>(okResult.Value);
        Assert.Single(returnedResult.Items);
    }

    [Fact]
    public async Task GetAll_WithIsActiveFilter_ReturnsFilteredResults()
    {
        // Arrange
        var pagedResult = new PagedResult<RecurringTransactionResponseDto>
        {
            Items = new List<RecurringTransactionResponseDto>
            {
                new RecurringTransactionResponseDto { Id = 1, IsActive = true }
            },
            Page = 1,
            PageSize = 10,
            TotalCount = 1
        };

        _serviceMock
            .Setup(s => s.GetPagedAsync(UserId, It.IsAny<PaginationParams>(), true))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _controller.GetAll(isActive: true);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedResult = Assert.IsType<PagedResult<RecurringTransactionResponseDto>>(okResult.Value);
        Assert.True(returnedResult.Items.First().IsActive);
    }

    [Fact]
    public async Task GetAllWithoutPagination_ReturnsAllRecurringTransactions()
    {
        // Arrange
        var transactions = new List<RecurringTransactionResponseDto>
        {
            new RecurringTransactionResponseDto { Id = 1, Amount = 100 },
            new RecurringTransactionResponseDto { Id = 2, Amount = 200 }
        };

        _serviceMock
            .Setup(s => s.GetAllAsync(UserId, null))
            .ReturnsAsync(transactions);

        // Act
        var result = await _controller.GetAllWithoutPagination();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedTransactions = Assert.IsAssignableFrom<IEnumerable<RecurringTransactionResponseDto>>(okResult.Value);
        Assert.Equal(2, returnedTransactions.Count());
    }

    [Fact]
    public async Task GetById_ExistingTransaction_ReturnsOk()
    {
        // Arrange
        var transaction = new RecurringTransactionResponseDto { Id = 1, Amount = 100 };

        _serviceMock
            .Setup(s => s.GetByIdAsync(1, UserId))
            .ReturnsAsync(transaction);

        // Act
        var result = await _controller.GetById(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedTransaction = Assert.IsType<RecurringTransactionResponseDto>(okResult.Value);
        Assert.Equal(1, returnedTransaction.Id);
    }

    [Fact]
    public async Task GetById_NonExistingTransaction_ReturnsNotFound()
    {
        // Arrange
        _serviceMock
            .Setup(s => s.GetByIdAsync(999, UserId))
            .ReturnsAsync((RecurringTransactionResponseDto?)null);

        // Act
        var result = await _controller.GetById(999);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Create_ValidTransaction_ReturnsCreatedAtAction()
    {
        // Arrange
        var createDto = new CreateRecurringTransactionDto
        {
            Amount = 100,
            Description = "Test",
            CategoryId = 1,
            Frequency = Core.Enums.RecurrenceFrequency.Monthly
        };

        var createdTransaction = new RecurringTransactionResponseDto
        {
            Id = 1,
            Amount = 100
        };

        _serviceMock
            .Setup(s => s.CreateAsync(createDto, UserId))
            .ReturnsAsync(createdTransaction);

        // Act
        var result = await _controller.Create(createDto);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(RecurringTransactionsController.GetById), createdResult.ActionName);
    }

    [Fact]
    public async Task Create_InvalidData_ReturnsBadRequest()
    {
        // Arrange
        var createDto = new CreateRecurringTransactionDto
        {
            Amount = 100,
            CategoryId = 999
        };

        _serviceMock
            .Setup(s => s.CreateAsync(createDto, UserId))
            .ReturnsAsync((RecurringTransactionResponseDto?)null);

        // Act
        var result = await _controller.Create(createDto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Update_ExistingTransaction_ReturnsOk()
    {
        // Arrange
        var updateDto = new UpdateRecurringTransactionDto
        {
            Amount = 200
        };

        var updatedTransaction = new RecurringTransactionResponseDto
        {
            Id = 1,
            Amount = 200
        };

        _serviceMock
            .Setup(s => s.UpdateAsync(1, updateDto, UserId))
            .ReturnsAsync(updatedTransaction);

        // Act
        var result = await _controller.Update(1, updateDto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedTransaction = Assert.IsType<RecurringTransactionResponseDto>(okResult.Value);
        Assert.Equal(200, returnedTransaction.Amount);
    }

    [Fact]
    public async Task Update_NonExistingTransaction_ReturnsNotFound()
    {
        // Arrange
        var updateDto = new UpdateRecurringTransactionDto { Amount = 200 };

        _serviceMock
            .Setup(s => s.UpdateAsync(999, updateDto, UserId))
            .ReturnsAsync((RecurringTransactionResponseDto?)null);

        // Act
        var result = await _controller.Update(999, updateDto);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Delete_ExistingTransaction_ReturnsNoContent()
    {
        // Arrange
        _serviceMock
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
        _serviceMock
            .Setup(s => s.DeleteAsync(999, UserId))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.Delete(999);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task ToggleActive_ExistingTransaction_ReturnsOk()
    {
        // Arrange
        _serviceMock
            .Setup(s => s.ToggleActiveAsync(1, UserId))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.ToggleActive(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task ToggleActive_NonExistingTransaction_ReturnsNotFound()
    {
        // Arrange
        _serviceMock
            .Setup(s => s.ToggleActiveAsync(999, UserId))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.ToggleActive(999);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetAll_WithCustomPaginationParams_UsesProvidedValues()
    {
        // Arrange
        var pagedResult = new PagedResult<RecurringTransactionResponseDto>
        {
            Items = new List<RecurringTransactionResponseDto>(),
            Page = 2,
            PageSize = 20,
            TotalCount = 0
        };

        _serviceMock
            .Setup(s => s.GetPagedAsync(
                UserId,
                It.Is<PaginationParams>(p => p.Page == 2 && p.PageSize == 20),
                null))
            .ReturnsAsync(pagedResult);

        // Act
        await _controller.GetAll(page: 2, pageSize: 20);

        // Assert
        _serviceMock.Verify(s => s.GetPagedAsync(
            UserId,
            It.Is<PaginationParams>(p => p.Page == 2 && p.PageSize == 20),
            null), Times.Once);
    }
}
