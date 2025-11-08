using CleverBudget.Api.Controllers;
using CleverBudget.Core.DTOs;
using CleverBudget.Core.Enums;
using CleverBudget.Core.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using Xunit;

namespace CleverBudget.Tests.Controllers;

public class ReportsControllerTests
{
    private readonly Mock<IReportService> _reportServiceMock;
    private readonly ReportsController _controller;
    private const string UserId = "test-user-id";

    public ReportsControllerTests()
    {
        _reportServiceMock = new Mock<IReportService>();
        _controller = new ReportsController(_reportServiceMock.Object);

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
    public async Task GetSummary_WithoutDateRange_ReturnsOk()
    {
        // Arrange
        var summary = new SummaryReportDto
        {
            TotalIncome = 5000,
            TotalExpenses = 3000,
            Balance = 2000
        };

        _reportServiceMock
            .Setup(s => s.GetSummaryAsync(UserId, null, null))
            .ReturnsAsync(summary);

        // Act
        var result = await _controller.GetSummary();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedSummary = Assert.IsType<SummaryReportDto>(okResult.Value);
        Assert.Equal(5000, returnedSummary.TotalIncome);
        Assert.Equal(3000, returnedSummary.TotalExpenses);
        Assert.Equal(2000, returnedSummary.Balance);
    }

    [Fact]
    public async Task GetSummary_WithDateRange_PassesCorrectParameters()
    {
        // Arrange
        var startDate = new DateTime(2025, 1, 1);
        var endDate = new DateTime(2025, 1, 31);
        var summary = new SummaryReportDto();

        _reportServiceMock
            .Setup(s => s.GetSummaryAsync(UserId, startDate, endDate))
            .ReturnsAsync(summary);

        // Act
        await _controller.GetSummary(startDate, endDate);

        // Assert
        _reportServiceMock.Verify(s => s.GetSummaryAsync(UserId, startDate, endDate), Times.Once);
    }

    [Fact]
    public async Task GetCategoryReport_ExpensesOnly_ReturnsOk()
    {
        // Arrange
        var categoryReport = new List<CategoryReportDto>
        {
            new CategoryReportDto
            {
                CategoryName = "Food",
                TotalAmount = 500,
                TransactionCount = 10
            }
        };

        _reportServiceMock
            .Setup(s => s.GetCategoryReportAsync(UserId, null, null, true))
            .ReturnsAsync(categoryReport);

        // Act
        var result = await _controller.GetCategoryReport();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedReport = Assert.IsAssignableFrom<IEnumerable<CategoryReportDto>>(okResult.Value);
        Assert.Single(returnedReport);
    }

    [Fact]
    public async Task GetCategoryReport_IncludingIncome_PassesCorrectParameter()
    {
        // Arrange
        var categoryReport = new List<CategoryReportDto>();

        _reportServiceMock
            .Setup(s => s.GetCategoryReportAsync(UserId, null, null, false))
            .ReturnsAsync(categoryReport);

        // Act
        await _controller.GetCategoryReport(expensesOnly: false);

        // Assert
        _reportServiceMock.Verify(s => s.GetCategoryReportAsync(UserId, null, null, false), Times.Once);
    }

    [Fact]
    public async Task GetCategoryReport_WithDateRange_PassesCorrectParameters()
    {
        // Arrange
        var startDate = new DateTime(2025, 1, 1);
        var endDate = new DateTime(2025, 1, 31);
        var categoryReport = new List<CategoryReportDto>();

        _reportServiceMock
            .Setup(s => s.GetCategoryReportAsync(UserId, startDate, endDate, true))
            .ReturnsAsync(categoryReport);

        // Act
        await _controller.GetCategoryReport(startDate, endDate);

        // Assert
        _reportServiceMock.Verify(s => s.GetCategoryReportAsync(UserId, startDate, endDate, true), Times.Once);
    }

    [Fact]
    public async Task GetMonthlyReport_WithDefaultMonths_ReturnsOk()
    {
        // Arrange
        var monthlyReport = new List<MonthlyReportDto>
        {
            new MonthlyReportDto
            {
                Month = 11,
                Year = 2025,
                TotalIncome = 5000,
                TotalExpenses = 3000
            }
        };

        _reportServiceMock
            .Setup(s => s.GetMonthlyReportAsync(UserId, 12, It.IsAny<ReportGroupBy>()))
            .ReturnsAsync(monthlyReport);

        // Act
        var result = await _controller.GetMonthlyReport();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedReport = Assert.IsAssignableFrom<IEnumerable<MonthlyReportDto>>(okResult.Value);
        Assert.Single(returnedReport);
    }

    [Fact]
    public async Task GetMonthlyReport_WithCustomMonths_PassesCorrectParameter()
    {
        // Arrange
        var monthlyReport = new List<MonthlyReportDto>();

        _reportServiceMock
            .Setup(s => s.GetMonthlyReportAsync(UserId, 6, It.IsAny<ReportGroupBy>()))
            .ReturnsAsync(monthlyReport);

        // Act
        await _controller.GetMonthlyReport(6);

        // Assert
    _reportServiceMock.Verify(s => s.GetMonthlyReportAsync(UserId, 6, It.IsAny<ReportGroupBy>()), Times.Once);
    }

    [Fact]
    public async Task GetDetailedReport_WithoutDateRange_ReturnsOk()
    {
        // Arrange
        var detailedReport = new DetailedReportDto
        {
            Summary = new SummaryReportDto(),
            TopExpenseCategories = new List<CategoryReportDto>(),
            MonthlyHistory = new List<MonthlyReportDto>()
        };

        _reportServiceMock
            .Setup(s => s.GetDetailedReportAsync(UserId, null, null))
            .ReturnsAsync(detailedReport);

        // Act
        var result = await _controller.GetDetailedReport();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedReport = Assert.IsType<DetailedReportDto>(okResult.Value);
        Assert.NotNull(returnedReport.Summary);
    }

    [Fact]
    public async Task GetDetailedReport_WithDateRange_PassesCorrectParameters()
    {
        // Arrange
        var startDate = new DateTime(2025, 1, 1);
        var endDate = new DateTime(2025, 1, 31);
        var detailedReport = new DetailedReportDto
        {
            Summary = new SummaryReportDto(),
            TopExpenseCategories = new List<CategoryReportDto>(),
            MonthlyHistory = new List<MonthlyReportDto>()
        };

        _reportServiceMock
            .Setup(s => s.GetDetailedReportAsync(UserId, startDate, endDate))
            .ReturnsAsync(detailedReport);

        // Act
        await _controller.GetDetailedReport(startDate, endDate);

        // Assert
        _reportServiceMock.Verify(s => s.GetDetailedReportAsync(UserId, startDate, endDate), Times.Once);
    }
}
