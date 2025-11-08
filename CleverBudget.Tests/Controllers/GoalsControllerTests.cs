using CleverBudget.Api.Controllers;
using CleverBudget.Api.Extensions;
using CleverBudget.Core.Common;
using CleverBudget.Core.DTOs;
using CleverBudget.Core.Interfaces;
using CleverBudget.Core.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using Xunit;

namespace CleverBudget.Tests.Controllers;

public class GoalsControllerTests
{
    private readonly Mock<IGoalService> _goalServiceMock;
    private readonly GoalsController _controller;
    private const string UserId = "test-user-id";

    public GoalsControllerTests()
    {
        _goalServiceMock = new Mock<IGoalService>();
        _controller = new GoalsController(_goalServiceMock.Object);

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
        var pagedResult = new PagedResult<GoalResponseDto>
        {
            Items = new List<GoalResponseDto>
            {
                new GoalResponseDto { Id = 1, TargetAmount = 1000 }
            },
            Page = 1,
            PageSize = 10,
            TotalCount = 1
        };

        _goalServiceMock
            .Setup(s => s.GetPagedAsync(
                UserId,
                It.IsAny<PaginationParams>(),
                It.Is<int?>(m => m == null),
                It.Is<int?>(y => y == null),
                It.Is<int?>(c => c == null),
                It.Is<CategoryKind?>(k => k == null)))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _controller.GetAll();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedResult = Assert.IsType<PagedResult<GoalResponseDto>>(okResult.Value);
        Assert.Single(returnedResult.Items);
    }

    [Fact]
    public async Task GetAll_WithMonthFilter_PassesCorrectParameter()
    {
        // Arrange
        var pagedResult = new PagedResult<GoalResponseDto>
        {
            Items = new List<GoalResponseDto>(),
            Page = 1,
            PageSize = 10,
            TotalCount = 0
        };

        _goalServiceMock
            .Setup(s => s.GetPagedAsync(
                UserId,
                It.IsAny<PaginationParams>(),
                It.Is<int?>(m => m == 11),
                It.Is<int?>(y => y == 2025),
                It.Is<int?>(c => c == null),
                It.Is<CategoryKind?>(k => k == null)))
            .ReturnsAsync(pagedResult);

        // Act
        await _controller.GetAll(month: 11, year: 2025);

        // Assert
        _goalServiceMock.Verify(s => s.GetPagedAsync(
            UserId,
            It.IsAny<PaginationParams>(),
            It.Is<int?>(m => m == 11),
            It.Is<int?>(y => y == 2025),
            It.Is<int?>(c => c == null),
            It.Is<CategoryKind?>(k => k == null)), Times.Once);
    }

    [Fact]
    public async Task GetAllWithoutPagination_ReturnsAllGoals()
    {
        // Arrange
        var goals = new List<GoalResponseDto>
        {
            new GoalResponseDto { Id = 1, TargetAmount = 1000 },
            new GoalResponseDto { Id = 2, TargetAmount = 2000 }
        };

        _goalServiceMock
            .Setup(s => s.GetAllAsync(
                UserId,
                It.Is<int?>(m => m == null),
                It.Is<int?>(y => y == null),
                It.Is<int?>(c => c == null),
                It.Is<CategoryKind?>(k => k == null)))
            .ReturnsAsync(goals);

        // Act
        var result = await _controller.GetAllWithoutPagination();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedGoals = Assert.IsAssignableFrom<IEnumerable<GoalResponseDto>>(okResult.Value);
        Assert.Equal(2, returnedGoals.Count());
    }

    [Fact]
    public async Task GetById_ExistingGoal_ReturnsOk()
    {
        // Arrange
        var goal = new GoalResponseDto { Id = 1, TargetAmount = 1000 };

        _goalServiceMock
            .Setup(s => s.GetByIdAsync(1, UserId))
            .ReturnsAsync(goal);

        // Act
        var result = await _controller.GetById(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedGoal = Assert.IsType<GoalResponseDto>(okResult.Value);
        Assert.Equal(1, returnedGoal.Id);
    }

    [Fact]
    public async Task GetById_NonExistingGoal_ReturnsNotFound()
    {
        // Arrange
        _goalServiceMock
            .Setup(s => s.GetByIdAsync(999, UserId))
            .ReturnsAsync((GoalResponseDto?)null);

        // Act
        var result = await _controller.GetById(999);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Create_ValidGoal_ReturnsCreatedAtAction()
    {
        // Arrange
        var createDto = new CreateGoalDto
        {
            CategoryId = 1,
            TargetAmount = 1000,
            Month = 11,
            Year = 2025
        };

        var createdGoal = new GoalResponseDto
        {
            Id = 1,
            CategoryId = 1,
            TargetAmount = 1000
        };

        _goalServiceMock
            .Setup(s => s.CreateAsync(createDto, UserId))
            .ReturnsAsync(createdGoal);

        // Act
        var result = await _controller.Create(createDto);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(GoalsController.GetById), createdResult.ActionName);
    }

    [Fact]
    public async Task Create_DuplicateGoal_ReturnsBadRequest()
    {
        // Arrange
        var createDto = new CreateGoalDto
        {
            CategoryId = 1,
            TargetAmount = 1000,
            Month = 11,
            Year = 2025
        };

        _goalServiceMock
            .Setup(s => s.CreateAsync(createDto, UserId))
            .ReturnsAsync((GoalResponseDto?)null);

        // Act
        var result = await _controller.Create(createDto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Update_ExistingGoal_ReturnsOk()
    {
        // Arrange
        var updateDto = new UpdateGoalDto
        {
            TargetAmount = 2000
        };

        var updatedGoal = new GoalResponseDto
        {
            Id = 1,
            TargetAmount = 2000
        };

        _goalServiceMock
            .Setup(s => s.GetByIdAsync(1, UserId))
            .ReturnsAsync(updatedGoal);

        _goalServiceMock
            .Setup(s => s.UpdateAsync(1, updateDto, UserId))
            .ReturnsAsync(updatedGoal);

        _controller.ControllerContext.HttpContext.Request.Headers["If-Match"] = EtagGenerator.Create(updatedGoal);

        // Act
        var result = await _controller.Update(1, updateDto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedGoal = Assert.IsType<GoalResponseDto>(okResult.Value);
        Assert.Equal(2000, returnedGoal.TargetAmount);
    }

    [Fact]
    public async Task Update_NonExistingGoal_ReturnsNotFound()
    {
        // Arrange
        var updateDto = new UpdateGoalDto { TargetAmount = 2000 };

        _goalServiceMock
            .Setup(s => s.GetByIdAsync(999, UserId))
            .ReturnsAsync((GoalResponseDto?)null);

        // Act
        var result = await _controller.Update(999, updateDto);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Delete_ExistingGoal_ReturnsNoContent()
    {
        // Arrange
        _goalServiceMock
            .Setup(s => s.DeleteAsync(1, UserId))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.Delete(1);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Delete_NonExistingGoal_ReturnsNotFound()
    {
        // Arrange
        _goalServiceMock
            .Setup(s => s.DeleteAsync(999, UserId))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.Delete(999);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetStatus_ReturnsGoalStatusList()
    {
        // Arrange
        var statusList = new List<GoalStatusDto>
        {
            new GoalStatusDto
            {
                Id = 1,
                CategoryName = "Test",
                TargetAmount = 1000,
                CurrentAmount = 500,
                Percentage = 50,
                Status = "Normal"
            }
        };

        _goalServiceMock
            .Setup(s => s.GetStatusAsync(UserId, null, null))
            .ReturnsAsync(statusList);

        // Act
        var result = await _controller.GetStatus();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedStatus = Assert.IsAssignableFrom<IEnumerable<GoalStatusDto>>(okResult.Value);
        Assert.Single(returnedStatus);
    }

    [Fact]
    public async Task GetStatus_WithMonthFilter_PassesCorrectParameters()
    {
        // Arrange
        _goalServiceMock
            .Setup(s => s.GetStatusAsync(UserId, 11, 2025))
            .ReturnsAsync(new List<GoalStatusDto>());

        // Act
        await _controller.GetStatus(11, 2025);

        // Assert
        _goalServiceMock.Verify(s => s.GetStatusAsync(UserId, 11, 2025), Times.Once);
    }
}
