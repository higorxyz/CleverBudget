using CleverBudget.Api.Controllers;
using CleverBudget.Core.Common;
using CleverBudget.Core.DTOs;
using CleverBudget.Core.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using Xunit;

namespace CleverBudget.Tests.Controllers;

public class BudgetsControllerTests
{
    private readonly Mock<IBudgetService> _budgetServiceMock;
    private readonly Mock<ILogger<BudgetsController>> _loggerMock;
    private readonly BudgetsController _controller;
    private const string UserId = "test-user-id";

    public BudgetsControllerTests()
    {
        _budgetServiceMock = new Mock<IBudgetService>();
        _loggerMock = new Mock<ILogger<BudgetsController>>();
        _controller = new BudgetsController(_budgetServiceMock.Object, _loggerMock.Object);

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
    public async Task GetAll_ReturnsOkWithBudgets()
    {
        // Arrange
        var budgets = new List<BudgetResponseDto>
        {
            new BudgetResponseDto { Id = 1, Amount = 1000, Month = 11, Year = 2025 },
            new BudgetResponseDto { Id = 2, Amount = 2000, Month = 11, Year = 2025 }
        };

        _budgetServiceMock
            .Setup(s => s.GetAllAsync(UserId, null, null))
            .ReturnsAsync(budgets);

        // Act
        var result = await _controller.GetAll(null, null);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedBudgets = Assert.IsAssignableFrom<IEnumerable<BudgetResponseDto>>(okResult.Value);
        Assert.Equal(2, returnedBudgets.Count());
    }

    [Fact]
    public async Task GetAll_WithYearFilter_ReturnsFilteredBudgets()
    {
        // Arrange
        var budgets = new List<BudgetResponseDto>
        {
            new BudgetResponseDto { Id = 1, Amount = 1000, Month = 11, Year = 2025 }
        };

        _budgetServiceMock
            .Setup(s => s.GetAllAsync(UserId, 2025, null))
            .ReturnsAsync(budgets);

        // Act
        var result = await _controller.GetAll(2025, null);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedBudgets = Assert.IsAssignableFrom<IEnumerable<BudgetResponseDto>>(okResult.Value);
        Assert.Single(returnedBudgets);
    }

    [Fact]
    public async Task GetPaged_ReturnsPagedResult()
    {
        // Arrange
        var pagedResult = new PagedResult<BudgetResponseDto>
        {
            Items = new List<BudgetResponseDto>
            {
                new BudgetResponseDto { Id = 1, Amount = 1000 }
            },
            Page = 1,
            PageSize = 10,
            TotalCount = 1
        };

        _budgetServiceMock
            .Setup(s => s.GetPagedAsync(UserId, It.IsAny<PaginationParams>(), null, null))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _controller.GetPaged();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedResult = Assert.IsType<PagedResult<BudgetResponseDto>>(okResult.Value);
        Assert.Single(returnedResult.Items);
    }

    [Fact]
    public async Task GetById_ExistingBudget_ReturnsOk()
    {
        // Arrange
        var budget = new BudgetResponseDto { Id = 1, Amount = 1000 };

        _budgetServiceMock
            .Setup(s => s.GetByIdAsync(1, UserId))
            .ReturnsAsync(budget);

        // Act
        var result = await _controller.GetById(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedBudget = Assert.IsType<BudgetResponseDto>(okResult.Value);
        Assert.Equal(1, returnedBudget.Id);
    }

    [Fact]
    public async Task GetById_NonExistingBudget_ReturnsNotFound()
    {
        // Arrange
        _budgetServiceMock
            .Setup(s => s.GetByIdAsync(999, UserId))
            .ReturnsAsync((BudgetResponseDto?)null);

        // Act
        var result = await _controller.GetById(999);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.NotNull(notFoundResult.Value);
    }

    [Fact]
    public async Task GetByCategoryAndPeriod_ExistingBudget_ReturnsOk()
    {
        // Arrange
        var budget = new BudgetResponseDto { Id = 1, CategoryId = 5, Month = 11, Year = 2025 };

        _budgetServiceMock
            .Setup(s => s.GetByCategoryAndPeriodAsync(5, 11, 2025, UserId))
            .ReturnsAsync(budget);

        // Act
        var result = await _controller.GetByCategoryAndPeriod(5, 11, 2025);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedBudget = Assert.IsType<BudgetResponseDto>(okResult.Value);
        Assert.Equal(5, returnedBudget.CategoryId);
    }

    [Fact]
    public async Task GetByCategoryAndPeriod_NonExisting_ReturnsNotFound()
    {
        // Arrange
        _budgetServiceMock
            .Setup(s => s.GetByCategoryAndPeriodAsync(999, 11, 2025, UserId))
            .ReturnsAsync((BudgetResponseDto?)null);

        // Act
        var result = await _controller.GetByCategoryAndPeriod(999, 11, 2025);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetCurrentMonth_ReturnsCurrentMonthBudgets()
    {
        // Arrange
        var budgets = new List<BudgetResponseDto>
        {
            new BudgetResponseDto { Id = 1, Month = DateTime.UtcNow.Month, Year = DateTime.UtcNow.Year }
        };

        _budgetServiceMock
            .Setup(s => s.GetCurrentMonthBudgetsAsync(UserId))
            .ReturnsAsync(budgets);

        // Act
        var result = await _controller.GetCurrentMonth();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedBudgets = Assert.IsAssignableFrom<IEnumerable<BudgetResponseDto>>(okResult.Value);
        Assert.Single(returnedBudgets);
    }

    [Fact]
    public async Task GetSummary_ReturnsCorrectCalculations()
    {
        // Arrange
        _budgetServiceMock
            .Setup(s => s.GetTotalBudgetForMonthAsync(UserId, It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(10000m);

        _budgetServiceMock
            .Setup(s => s.GetTotalSpentForMonthAsync(UserId, It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(8500m);

        // Act
        var result = await _controller.GetSummary(null, null);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        
        var summary = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(
            System.Text.Json.JsonSerializer.Serialize(okResult.Value));
        
        Assert.Equal(10000m, summary.GetProperty("totalBudget").GetDecimal());
        Assert.Equal(8500m, summary.GetProperty("totalSpent").GetDecimal());
        Assert.Equal(1500m, summary.GetProperty("remaining").GetDecimal());
        Assert.Equal(85m, summary.GetProperty("percentageUsed").GetDecimal());
        Assert.Equal("Crítico", summary.GetProperty("status").GetString());
    }

    [Fact]
    public async Task GetSummary_OverBudget_ReturnsExcedidoStatus()
    {
        // Arrange
        _budgetServiceMock
            .Setup(s => s.GetTotalBudgetForMonthAsync(UserId, It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(10000m);

        _budgetServiceMock
            .Setup(s => s.GetTotalSpentForMonthAsync(UserId, It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(12000m);

        // Act
        var result = await _controller.GetSummary(null, null);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        
        var summary = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(
            System.Text.Json.JsonSerializer.Serialize(okResult.Value));
        
        Assert.Equal("Excedido", summary.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Create_ValidBudget_ReturnsCreatedAtAction()
    {
        // Arrange
        var createDto = new CreateBudgetDto
        {
            CategoryId = 5,
            Amount = 1000,
            Month = 11,
            Year = 2025
        };

        var createdBudget = new BudgetResponseDto
        {
            Id = 1,
            CategoryId = 5,
            Amount = 1000,
            Month = 11,
            Year = 2025
        };

        _budgetServiceMock
            .Setup(s => s.CreateAsync(createDto, UserId))
            .ReturnsAsync(createdBudget);

        // Act
        var result = await _controller.Create(createDto);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(BudgetsController.GetById), createdResult.ActionName);
        var returnedBudget = Assert.IsType<BudgetResponseDto>(createdResult.Value);
        Assert.Equal(1, returnedBudget.Id);
    }

    [Fact]
    public async Task Create_DuplicateBudget_ReturnsBadRequest()
    {
        // Arrange
        var createDto = new CreateBudgetDto
        {
            CategoryId = 5,
            Amount = 1000,
            Month = 11,
            Year = 2025
        };

        _budgetServiceMock
            .Setup(s => s.CreateAsync(createDto, UserId))
            .ReturnsAsync((BudgetResponseDto?)null);

        // Act
        var result = await _controller.Create(createDto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public async Task Update_ExistingBudget_ReturnsOk()
    {
        // Arrange
        var updateDto = new UpdateBudgetDto
        {
            Amount = 2000
        };

        var updatedBudget = new BudgetResponseDto
        {
            Id = 1,
            Amount = 2000
        };

        _budgetServiceMock
            .Setup(s => s.UpdateAsync(1, updateDto, UserId))
            .ReturnsAsync(updatedBudget);

        // Act
        var result = await _controller.Update(1, updateDto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedBudget = Assert.IsType<BudgetResponseDto>(okResult.Value);
        Assert.Equal(2000, returnedBudget.Amount);
    }

    [Fact]
    public async Task Update_NonExistingBudget_ReturnsNotFound()
    {
        // Arrange
        var updateDto = new UpdateBudgetDto { Amount = 2000 };

        _budgetServiceMock
            .Setup(s => s.UpdateAsync(999, updateDto, UserId))
            .ReturnsAsync((BudgetResponseDto?)null);

        // Act
        var result = await _controller.Update(999, updateDto);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Delete_ExistingBudget_ReturnsNoContent()
    {
        // Arrange
        _budgetServiceMock
            .Setup(s => s.DeleteAsync(1, UserId))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.Delete(1);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Delete_NonExistingBudget_ReturnsNotFound()
    {
        // Arrange
        _budgetServiceMock
            .Setup(s => s.DeleteAsync(999, UserId))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.Delete(999);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }
}
