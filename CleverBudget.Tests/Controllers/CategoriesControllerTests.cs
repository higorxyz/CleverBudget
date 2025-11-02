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

public class CategoriesControllerTests
{
    private readonly Mock<ICategoryService> _categoryServiceMock;
    private readonly CategoriesController _controller;
    private const string UserId = "test-user-id";

    public CategoriesControllerTests()
    {
        _categoryServiceMock = new Mock<ICategoryService>();
        _controller = new CategoriesController(_categoryServiceMock.Object);

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
        var pagedResult = new PagedResult<CategoryResponseDto>
        {
            Items = new List<CategoryResponseDto>
            {
                new CategoryResponseDto { Id = 1, Name = "Test Category" }
            },
            Page = 1,
            PageSize = 20,
            TotalCount = 1
        };

        _categoryServiceMock
            .Setup(s => s.GetPagedAsync(UserId, It.IsAny<PaginationParams>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _controller.GetAll();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedResult = Assert.IsType<PagedResult<CategoryResponseDto>>(okResult.Value);
        Assert.Single(returnedResult.Items);
    }

    [Fact]
    public async Task GetAllWithoutPagination_ReturnsAllCategories()
    {
        // Arrange
        var categories = new List<CategoryResponseDto>
        {
            new CategoryResponseDto { Id = 1, Name = "Category 1" },
            new CategoryResponseDto { Id = 2, Name = "Category 2" }
        };

        _categoryServiceMock
            .Setup(s => s.GetAllAsync(UserId))
            .ReturnsAsync(categories);

        // Act
        var result = await _controller.GetAllWithoutPagination();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedCategories = Assert.IsAssignableFrom<IEnumerable<CategoryResponseDto>>(okResult.Value);
        Assert.Equal(2, returnedCategories.Count());
    }

    [Fact]
    public async Task GetById_ExistingCategory_ReturnsOk()
    {
        // Arrange
        var category = new CategoryResponseDto { Id = 1, Name = "Test Category" };

        _categoryServiceMock
            .Setup(s => s.GetByIdAsync(1, UserId))
            .ReturnsAsync(category);

        // Act
        var result = await _controller.GetById(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedCategory = Assert.IsType<CategoryResponseDto>(okResult.Value);
        Assert.Equal(1, returnedCategory.Id);
    }

    [Fact]
    public async Task GetById_NonExistingCategory_ReturnsNotFound()
    {
        // Arrange
        _categoryServiceMock
            .Setup(s => s.GetByIdAsync(999, UserId))
            .ReturnsAsync((CategoryResponseDto?)null);

        // Act
        var result = await _controller.GetById(999);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.NotNull(notFoundResult.Value);
    }

    [Fact]
    public async Task Create_ValidCategory_ReturnsCreatedAtAction()
    {
        // Arrange
        var createDto = new CreateCategoryDto
        {
            Name = "New Category",
            Icon = "icon-test"
        };

        var createdCategory = new CategoryResponseDto
        {
            Id = 1,
            Name = "New Category",
            Icon = "icon-test"
        };

        _categoryServiceMock
            .Setup(s => s.CreateAsync(createDto, UserId))
            .ReturnsAsync(createdCategory);

        // Act
        var result = await _controller.Create(createDto);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(CategoriesController.GetById), createdResult.ActionName);
        var returnedCategory = Assert.IsType<CategoryResponseDto>(createdResult.Value);
        Assert.Equal(1, returnedCategory.Id);
    }

    [Fact]
    public async Task Create_DuplicateName_ReturnsBadRequest()
    {
        // Arrange
        var createDto = new CreateCategoryDto
        {
            Name = "Existing Category"
        };

        _categoryServiceMock
            .Setup(s => s.CreateAsync(createDto, UserId))
            .ReturnsAsync((CategoryResponseDto?)null);

        // Act
        var result = await _controller.Create(createDto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public async Task Update_ExistingCategory_ReturnsOk()
    {
        // Arrange
        var updateDto = new UpdateCategoryDto
        {
            Name = "Updated Category"
        };

        var updatedCategory = new CategoryResponseDto
        {
            Id = 1,
            Name = "Updated Category"
        };

        _categoryServiceMock
            .Setup(s => s.UpdateAsync(1, updateDto, UserId))
            .ReturnsAsync(updatedCategory);

        // Act
        var result = await _controller.Update(1, updateDto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedCategory = Assert.IsType<CategoryResponseDto>(okResult.Value);
        Assert.Equal("Updated Category", returnedCategory.Name);
    }

    [Fact]
    public async Task Update_DefaultCategory_ReturnsBadRequest()
    {
        // Arrange
        var updateDto = new UpdateCategoryDto { Name = "New Name" };

        _categoryServiceMock
            .Setup(s => s.UpdateAsync(1, updateDto, UserId))
            .ReturnsAsync((CategoryResponseDto?)null);

        // Act
        var result = await _controller.Update(1, updateDto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Delete_CategoryWithoutTransactions_ReturnsNoContent()
    {
        // Arrange
        _categoryServiceMock
            .Setup(s => s.DeleteAsync(1, UserId))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.Delete(1);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Delete_DefaultCategory_ReturnsBadRequest()
    {
        // Arrange
        _categoryServiceMock
            .Setup(s => s.DeleteAsync(1, UserId))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.Delete(1);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task GetAll_WithCustomPaginationParams_UsesProvidedValues()
    {
        // Arrange
        var pagedResult = new PagedResult<CategoryResponseDto>
        {
            Items = new List<CategoryResponseDto>(),
            Page = 2,
            PageSize = 50,
            TotalCount = 0
        };

        _categoryServiceMock
            .Setup(s => s.GetPagedAsync(
                UserId,
                It.Is<PaginationParams>(p => p.Page == 2 && p.PageSize == 50)))
            .ReturnsAsync(pagedResult);

        // Act
        await _controller.GetAll(page: 2, pageSize: 50);

        // Assert
        _categoryServiceMock.Verify(s => s.GetPagedAsync(
            UserId,
            It.Is<PaginationParams>(p => p.Page == 2 && p.PageSize == 50 && p.SortBy == "name" && p.SortOrder == "asc")),
            Times.Once);
    }
}
