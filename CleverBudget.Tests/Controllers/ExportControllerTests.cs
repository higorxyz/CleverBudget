using CleverBudget.Api.Controllers;
using CleverBudget.Core.Interfaces;
using CleverBudget.Core.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using Xunit;

namespace CleverBudget.Tests.Controllers;

public class ExportControllerTests
{
    private readonly Mock<IExportService> _exportServiceMock;
    private readonly Mock<IExportDeliveryService> _exportDeliveryServiceMock;
    private readonly ExportController _controller;
    private const string UserId = "test-user-id";

    public ExportControllerTests()
    {
    _exportServiceMock = new Mock<IExportService>();
    _exportDeliveryServiceMock = new Mock<IExportDeliveryService>();
    _controller = new ExportController(_exportServiceMock.Object, _exportDeliveryServiceMock.Object);

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
            .Setup(s => s.ExportTransactionsToCsvAsync(UserId, null, null, It.IsAny<ExportRequestOptions>()))
            .ReturnsAsync(csvData);

        // Act
        var result = await _controller.ExportTransactionsCsv(null, null, null);

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
            .Setup(s => s.ExportTransactionsToCsvAsync(UserId, startDate, endDate, It.IsAny<ExportRequestOptions>()))
            .ReturnsAsync(csvData);

        // Act
        await _controller.ExportTransactionsCsv(startDate, endDate, null);

        // Assert
        _exportServiceMock.Verify(s => s.ExportTransactionsToCsvAsync(UserId, startDate, endDate, It.IsAny<ExportRequestOptions>()), Times.Once);
    }

    [Fact]
    public async Task ExportCategoriesCsv_ReturnsFileContentResult()
    {
        // Arrange
        var csvData = System.Text.Encoding.UTF8.GetBytes("ID,Name\n1,Food");

        _exportServiceMock
            .Setup(s => s.ExportCategoriesToCsvAsync(UserId, It.IsAny<ExportRequestOptions>()))
            .ReturnsAsync(csvData);

        // Act
        var result = await _controller.ExportCategoriesCsv(null);

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
            .Setup(s => s.ExportGoalsToCsvAsync(UserId, null, null, It.IsAny<ExportRequestOptions>()))
            .ReturnsAsync(csvData);

        // Act
        var result = await _controller.ExportGoalsCsv(null, null, null);

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
            .Setup(s => s.ExportGoalsToCsvAsync(UserId, 11, 2025, It.IsAny<ExportRequestOptions>()))
            .ReturnsAsync(csvData);

        // Act
        await _controller.ExportGoalsCsv(11, 2025, null);

        // Assert
        _exportServiceMock.Verify(s => s.ExportGoalsToCsvAsync(UserId, 11, 2025, It.IsAny<ExportRequestOptions>()), Times.Once);
    }

    [Fact]
    public async Task ExportTransactionsPdf_ReturnsFileContentResult()
    {
        // Arrange
        var pdfData = new byte[] { 0x25, 0x50, 0x44, 0x46 }; // PDF magic bytes

        _exportServiceMock
            .Setup(s => s.ExportTransactionsToPdfAsync(UserId, null, null, It.IsAny<ExportRequestOptions>()))
            .ReturnsAsync(pdfData);

        // Act
        var result = await _controller.ExportTransactionsPdf(null, null, null);

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
            .Setup(s => s.ExportTransactionsToPdfAsync(UserId, startDate, endDate, It.IsAny<ExportRequestOptions>()))
            .ReturnsAsync(pdfData);

        // Act
        await _controller.ExportTransactionsPdf(startDate, endDate, null);

        // Assert
        _exportServiceMock.Verify(s => s.ExportTransactionsToPdfAsync(UserId, startDate, endDate, It.IsAny<ExportRequestOptions>()), Times.Once);
    }

    [Fact]
    public async Task ExportFinancialReportPdf_ReturnsFileContentResult()
    {
        // Arrange
        var pdfData = new byte[] { 0x25, 0x50, 0x44, 0x46 };

        _exportServiceMock
            .Setup(s => s.ExportFinancialReportToPdfAsync(UserId, null, null, It.IsAny<ExportRequestOptions>()))
            .ReturnsAsync(pdfData);

        // Act
        var result = await _controller.ExportFinancialReportPdf(null, null, null);

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
            .Setup(s => s.ExportFinancialReportToPdfAsync(UserId, startDate, endDate, It.IsAny<ExportRequestOptions>()))
            .ReturnsAsync(pdfData);

        // Act
        await _controller.ExportFinancialReportPdf(startDate, endDate, null);

        // Assert
        _exportServiceMock.Verify(s => s.ExportFinancialReportToPdfAsync(UserId, startDate, endDate, It.IsAny<ExportRequestOptions>()), Times.Once);
    }

    [Fact]
    public async Task ExportGoalsReportPdf_ReturnsFileContentResult()
    {
        // Arrange
        var pdfData = new byte[] { 0x25, 0x50, 0x44, 0x46 };

        _exportServiceMock
            .Setup(s => s.ExportGoalsReportToPdfAsync(UserId, null, null, It.IsAny<ExportRequestOptions>()))
            .ReturnsAsync(pdfData);

        // Act
        var result = await _controller.ExportGoalsReportPdf(null, null, null);

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
            .Setup(s => s.ExportGoalsReportToPdfAsync(UserId, 11, 2025, It.IsAny<ExportRequestOptions>()))
            .ReturnsAsync(pdfData);

        // Act
        await _controller.ExportGoalsReportPdf(11, 2025, null);

        // Assert
        _exportServiceMock.Verify(s => s.ExportGoalsReportToPdfAsync(UserId, 11, 2025, It.IsAny<ExportRequestOptions>()), Times.Once);
    }

    [Fact]
    public async Task ExportTransactionsCsv_EmailDelivery_ReturnsOkWithDeliveryInfo()
    {
        // Arrange
        var csvData = System.Text.Encoding.UTF8.GetBytes("data");
        ExportRequestOptions? capturedOptions = null;

        _exportServiceMock
            .Setup(s => s.ExportTransactionsToCsvAsync(
                UserId,
                null,
                null,
                It.IsAny<ExportRequestOptions>()))
            .Callback<string, DateTime?, DateTime?, ExportRequestOptions>((_, _, _, o) => capturedOptions = o)
            .ReturnsAsync(csvData);

        _exportDeliveryServiceMock
            .Setup(d => d.DeliverAsync(UserId, It.IsAny<string>(), csvData, It.IsAny<ExportRequestOptions>()))
            .ReturnsAsync(new ExportDeliveryResultDto
            {
                Delivered = true,
                Mode = ExportDeliveryMode.Email,
                Message = "Arquivo enviado por email."
            });

        var query = new ExportController.ExportOptionsQuery
        {
            Delivery = "Email",
            Email = "user@example.com"
        };

        // Act
        var result = await _controller.ExportTransactionsCsv(null, null, query);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var payload = okResult.Value!;
        Assert.Equal(ExportDeliveryMode.Email, GetProperty<ExportDeliveryMode>(payload, "Mode"));
        Assert.Equal("Arquivo enviado por email.", GetProperty<string>(payload, "Message"));
        Assert.Null(GetProperty<string?>(payload, "Location"));
        Assert.NotNull(capturedOptions);
        Assert.Equal(ExportDeliveryMode.Email, capturedOptions!.DeliveryMode);
        Assert.Equal("user@example.com", capturedOptions.Email);
        _exportDeliveryServiceMock.Verify(d => d.DeliverAsync(UserId, It.IsAny<string>(), csvData, It.IsAny<ExportRequestOptions>()), Times.Once);
    }

    [Fact]
    public async Task ExportTransactionsCsv_SignedLinkDelivery_ReturnsLocation()
    {
        // Arrange
        var csvData = System.Text.Encoding.UTF8.GetBytes("data");

        _exportServiceMock
            .Setup(s => s.ExportTransactionsToCsvAsync(UserId, null, null, It.IsAny<ExportRequestOptions>()))
            .ReturnsAsync(csvData);

        _exportDeliveryServiceMock
            .Setup(d => d.DeliverAsync(UserId, It.IsAny<string>(), csvData, It.IsAny<ExportRequestOptions>()))
            .ReturnsAsync(new ExportDeliveryResultDto
            {
                Delivered = true,
                Mode = ExportDeliveryMode.SignedLink,
                Location = "exports/file-token.csv",
                Message = "Disponível"
            });

        var query = new ExportController.ExportOptionsQuery
        {
            Delivery = "SignedLink"
        };

        // Act
        var result = await _controller.ExportTransactionsCsv(null, null, query);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var payload = okResult.Value!;
        Assert.Equal("exports/file-token.csv", GetProperty<string>(payload, "Location"));
        Assert.Equal(ExportDeliveryMode.SignedLink, GetProperty<ExportDeliveryMode>(payload, "Mode"));
        Assert.Equal("Disponível", GetProperty<string>(payload, "Message"));
        _exportDeliveryServiceMock.Verify(d => d.DeliverAsync(UserId, It.IsAny<string>(), csvData, It.IsAny<ExportRequestOptions>()), Times.Once);
    }

    [Fact]
    public async Task ExportTransactionsCsv_DeliveryFailure_ReturnsProblem()
    {
        // Arrange
        var csvData = System.Text.Encoding.UTF8.GetBytes("data");

        _exportServiceMock
            .Setup(s => s.ExportTransactionsToCsvAsync(UserId, null, null, It.IsAny<ExportRequestOptions>()))
            .ReturnsAsync(csvData);

        _exportDeliveryServiceMock
            .Setup(d => d.DeliverAsync(UserId, It.IsAny<string>(), csvData, It.IsAny<ExportRequestOptions>()))
            .ReturnsAsync(new ExportDeliveryResultDto
            {
                Delivered = false,
                Mode = ExportDeliveryMode.Email,
                Message = "Falha"
            });

        var query = new ExportController.ExportOptionsQuery
        {
            Delivery = "Email",
            Email = "user@example.com"
        };

        // Act
        var result = await _controller.ExportTransactionsCsv(null, null, query);

        // Assert
        var problem = Assert.IsType<ObjectResult>(result);
        var details = Assert.IsType<ProblemDetails>(problem.Value);
        Assert.Equal(StatusCodes.Status422UnprocessableEntity, problem.StatusCode);
        Assert.Equal("Falha na entrega da exportação", details.Title);
        _exportDeliveryServiceMock.Verify(d => d.DeliverAsync(UserId, It.IsAny<string>(), csvData, It.IsAny<ExportRequestOptions>()), Times.Once);
    }

    private static T GetProperty<T>(object target, string propertyName)
    {
        var property = target.GetType().GetProperty(propertyName);
        Assert.NotNull(property);
        var value = property!.GetValue(target);
        if (value != null)
        {
            Assert.IsType<T>(value);
        }
        return (T)value!;
    }
}
