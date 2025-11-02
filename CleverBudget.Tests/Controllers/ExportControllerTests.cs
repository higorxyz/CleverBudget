using CleverBudget.Api.Controllers;
using CleverBudget.Core.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using Xunit;

namespace CleverBudget.Tests.Controllers;

public class ExportControllerTests
{
    private readonly Mock<IExportService> _exportServiceMock;
    private readonly ExportController _controller;
    private const string UserId = "test-user-id";

    public ExportControllerTests()
    {
        _exportServiceMock = new Mock<IExportService>();
        _controller = new ExportController(_exportServiceMock.Object);

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
    public async Task ExportTransactionsCsv_ReturnsFileContentResult()
    {
        // Arrange
        var csvData = System.Text.Encoding.UTF8.GetBytes("ID,Date,Amount\n1,2025-01-01,100");

        _exportServiceMock
            .Setup(s => s.ExportTransactionsToCsvAsync(UserId, null, null))
            .ReturnsAsync(csvData);

        // Act
        var result = await _controller.ExportTransactionsCsv();

        // Assert
        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal("text/csv", fileResult.ContentType);
        Assert.Contains("transacoes_", fileResult.FileDownloadName);
        Assert.Equal(csvData, fileResult.FileContents);
    }

    [Fact]
    public async Task ExportTransactionsCsv_WithDateRange_PassesCorrectParameters()
    {
        // Arrange
        var startDate = new DateTime(2025, 1, 1);
        var endDate = new DateTime(2025, 1, 31);
        var csvData = System.Text.Encoding.UTF8.GetBytes("test");

        _exportServiceMock
            .Setup(s => s.ExportTransactionsToCsvAsync(UserId, startDate, endDate))
            .ReturnsAsync(csvData);

        // Act
        await _controller.ExportTransactionsCsv(startDate, endDate);

        // Assert
        _exportServiceMock.Verify(s => s.ExportTransactionsToCsvAsync(UserId, startDate, endDate), Times.Once);
    }

    [Fact]
    public async Task ExportCategoriesCsv_ReturnsFileContentResult()
    {
        // Arrange
        var csvData = System.Text.Encoding.UTF8.GetBytes("ID,Name\n1,Food");

        _exportServiceMock
            .Setup(s => s.ExportCategoriesToCsvAsync(UserId))
            .ReturnsAsync(csvData);

        // Act
        var result = await _controller.ExportCategoriesCsv();

        // Assert
        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal("text/csv", fileResult.ContentType);
        Assert.Contains("categorias_", fileResult.FileDownloadName);
    }

    [Fact]
    public async Task ExportGoalsCsv_ReturnsFileContentResult()
    {
        // Arrange
        var csvData = System.Text.Encoding.UTF8.GetBytes("ID,Category,Target\n1,Food,1000");

        _exportServiceMock
            .Setup(s => s.ExportGoalsToCsvAsync(UserId, null, null))
            .ReturnsAsync(csvData);

        // Act
        var result = await _controller.ExportGoalsCsv();

        // Assert
        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal("text/csv", fileResult.ContentType);
        Assert.Contains("metas_", fileResult.FileDownloadName);
    }

    [Fact]
    public async Task ExportGoalsCsv_WithMonthYear_PassesCorrectParameters()
    {
        // Arrange
        var csvData = System.Text.Encoding.UTF8.GetBytes("test");

        _exportServiceMock
            .Setup(s => s.ExportGoalsToCsvAsync(UserId, 11, 2025))
            .ReturnsAsync(csvData);

        // Act
        await _controller.ExportGoalsCsv(11, 2025);

        // Assert
        _exportServiceMock.Verify(s => s.ExportGoalsToCsvAsync(UserId, 11, 2025), Times.Once);
    }

    [Fact]
    public async Task ExportTransactionsPdf_ReturnsFileContentResult()
    {
        // Arrange
        var pdfData = new byte[] { 0x25, 0x50, 0x44, 0x46 }; // PDF magic bytes

        _exportServiceMock
            .Setup(s => s.ExportTransactionsToPdfAsync(UserId, null, null))
            .ReturnsAsync(pdfData);

        // Act
        var result = await _controller.ExportTransactionsPdf();

        // Assert
        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal("application/pdf", fileResult.ContentType);
        Assert.Contains("transacoes_", fileResult.FileDownloadName);
        Assert.Contains(".pdf", fileResult.FileDownloadName);
    }

    [Fact]
    public async Task ExportTransactionsPdf_WithDateRange_PassesCorrectParameters()
    {
        // Arrange
        var startDate = new DateTime(2025, 1, 1);
        var endDate = new DateTime(2025, 1, 31);
        var pdfData = new byte[] { 0x25, 0x50, 0x44, 0x46 };

        _exportServiceMock
            .Setup(s => s.ExportTransactionsToPdfAsync(UserId, startDate, endDate))
            .ReturnsAsync(pdfData);

        // Act
        await _controller.ExportTransactionsPdf(startDate, endDate);

        // Assert
        _exportServiceMock.Verify(s => s.ExportTransactionsToPdfAsync(UserId, startDate, endDate), Times.Once);
    }

    [Fact]
    public async Task ExportFinancialReportPdf_ReturnsFileContentResult()
    {
        // Arrange
        var pdfData = new byte[] { 0x25, 0x50, 0x44, 0x46 };

        _exportServiceMock
            .Setup(s => s.ExportFinancialReportToPdfAsync(UserId, null, null))
            .ReturnsAsync(pdfData);

        // Act
        var result = await _controller.ExportFinancialReportPdf();

        // Assert
        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal("application/pdf", fileResult.ContentType);
        Assert.Contains("relatorio_financeiro_", fileResult.FileDownloadName);
    }

    [Fact]
    public async Task ExportFinancialReportPdf_WithDateRange_PassesCorrectParameters()
    {
        // Arrange
        var startDate = new DateTime(2025, 1, 1);
        var endDate = new DateTime(2025, 1, 31);
        var pdfData = new byte[] { 0x25, 0x50, 0x44, 0x46 };

        _exportServiceMock
            .Setup(s => s.ExportFinancialReportToPdfAsync(UserId, startDate, endDate))
            .ReturnsAsync(pdfData);

        // Act
        await _controller.ExportFinancialReportPdf(startDate, endDate);

        // Assert
        _exportServiceMock.Verify(s => s.ExportFinancialReportToPdfAsync(UserId, startDate, endDate), Times.Once);
    }

    [Fact]
    public async Task ExportGoalsReportPdf_ReturnsFileContentResult()
    {
        // Arrange
        var pdfData = new byte[] { 0x25, 0x50, 0x44, 0x46 };

        _exportServiceMock
            .Setup(s => s.ExportGoalsReportToPdfAsync(UserId, null, null))
            .ReturnsAsync(pdfData);

        // Act
        var result = await _controller.ExportGoalsReportPdf();

        // Assert
        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal("application/pdf", fileResult.ContentType);
        Assert.Contains("metas_", fileResult.FileDownloadName);
    }

    [Fact]
    public async Task ExportGoalsReportPdf_WithMonthYear_PassesCorrectParameters()
    {
        // Arrange
        var pdfData = new byte[] { 0x25, 0x50, 0x44, 0x46 };

        _exportServiceMock
            .Setup(s => s.ExportGoalsReportToPdfAsync(UserId, 11, 2025))
            .ReturnsAsync(pdfData);

        // Act
        await _controller.ExportGoalsReportPdf(11, 2025);

        // Assert
        _exportServiceMock.Verify(s => s.ExportGoalsReportToPdfAsync(UserId, 11, 2025), Times.Once);
    }
}
