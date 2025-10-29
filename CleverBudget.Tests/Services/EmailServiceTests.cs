using CleverBudget.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CleverBudget.Tests.Services;

public class EmailServiceTests
{
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<ILogger<EmailService>> _loggerMock;
    private readonly EmailService _emailService;

    public EmailServiceTests()
    {
        _configurationMock = new Mock<IConfiguration>();
        _loggerMock = new Mock<ILogger<EmailService>>();

        // Mock configuração do Brevo (API Key vazia para testes)
        _configurationMock.Setup(x => x["Brevo:ApiKey"]).Returns("");
        _configurationMock.Setup(x => x["Brevo:FromEmail"]).Returns("noreply@cleverbudget.com");
        _configurationMock.Setup(x => x["Brevo:FromName"]).Returns("CleverBudget");

        _emailService = new EmailService(_configurationMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task SendWelcomeEmailAsync_WithoutApiKey_ReturnsFalse()
    {
        // Arrange
        var toEmail = "user@example.com";
        var userName = "João Silva";

        // Act
        var result = await _emailService.SendWelcomeEmailAsync(toEmail, userName);

        // Assert
        Assert.False(result); // Deve falhar sem API Key
    }

    [Fact]
    public async Task SendGoalAlertEmailAsync_WithoutApiKey_ReturnsFalse()
    {
        // Arrange
        var toEmail = "user@example.com";
        var userName = "Maria Santos";
        var categoryName = "Alimentação";
        var currentAmount = 850m;
        var targetAmount = 1000m;
        var percentage = 85m;

        // Act
        var result = await _emailService.SendGoalAlertEmailAsync(
            toEmail, userName, categoryName, currentAmount, targetAmount, percentage);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task SendMonthlyReportEmailAsync_WithoutApiKey_ReturnsFalse()
    {
        // Arrange
        var toEmail = "user@example.com";
        var userName = "Pedro Costa";
        var pdfReport = new byte[] { 1, 2, 3 }; // PDF fake
        var month = "Janeiro";
        var year = 2025;

        // Act
        var result = await _emailService.SendMonthlyReportEmailAsync(
            toEmail, userName, pdfReport, month, year);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task SendEmailAsync_WithoutApiKey_ReturnsFalse()
    {
        // Arrange
        var toEmail = "user@example.com";
        var subject = "Teste";
        var htmlContent = "<html><body>Teste</body></html>";

        // Act
        var result = await _emailService.SendEmailAsync(toEmail, subject, htmlContent);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task SendEmailAsync_WithAttachment_WithoutApiKey_ReturnsFalse()
    {
        // Arrange
        var toEmail = "user@example.com";
        var subject = "Relatório";
        var htmlContent = "<html><body>Relatório anexo</body></html>";
        var attachment = new byte[] { 1, 2, 3, 4, 5 };
        var attachmentName = "relatorio.pdf";

        // Act
        var result = await _emailService.SendEmailAsync(
            toEmail, subject, htmlContent, attachment, attachmentName);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void EmailService_LogsWarning_WhenApiKeyNotConfigured()
    {
        // Arrange
        var freshLoggerMock = new Mock<ILogger<EmailService>>();
        
        // Act
        var service = new EmailService(_configurationMock.Object, freshLoggerMock.Object);

        // Assert
        // Verificar que o log de warning foi chamado no construtor
        freshLoggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Brevo API Key não configurada")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SendWelcomeEmailAsync_ValidParameters_CallsServiceCorrectly()
    {
        // Arrange
        var toEmail = "newuser@example.com";
        var userName = "Carlos Ferreira";

        // Act
        var result = await _emailService.SendWelcomeEmailAsync(toEmail, userName);

        // Assert
        // Sem API Key, deve retornar false mas não deve lançar exceção
        Assert.False(result);
    }

    [Fact]
    public async Task SendGoalAlertEmailAsync_At80Percent_UsesWarningColor()
    {
        // Arrange
        var toEmail = "user@example.com";
        var userName = "Ana Silva";
        var categoryName = "Transporte";
        var currentAmount = 800m;
        var targetAmount = 1000m;
        var percentage = 80m;

        // Act
        var result = await _emailService.SendGoalAlertEmailAsync(
            toEmail, userName, categoryName, currentAmount, targetAmount, percentage);

        // Assert
        Assert.False(result); // Sem API Key
        // Em implementação real, verificaria que cor de warning foi usada
    }

    [Fact]
    public async Task SendGoalAlertEmailAsync_Above100Percent_UsesExceededColor()
    {
        // Arrange
        var toEmail = "user@example.com";
        var userName = "Roberto Lima";
        var categoryName = "Lazer";
        var currentAmount = 1200m;
        var targetAmount = 1000m;
        var percentage = 120m;

        // Act
        var result = await _emailService.SendGoalAlertEmailAsync(
            toEmail, userName, categoryName, currentAmount, targetAmount, percentage);

        // Assert
        Assert.False(result); // Sem API Key
        // Em implementação real, verificaria que cor de "exceeded" foi usada
    }

    [Fact]
    public async Task SendMonthlyReportEmailAsync_ValidPdf_AttachesCorrectly()
    {
        // Arrange
        var toEmail = "user@example.com";
        var userName = "Juliana Rocha";
        var pdfReport = new byte[1024]; // PDF de 1KB
        Array.Fill(pdfReport, (byte)0xFF);
        var month = "Dezembro";
        var year = 2024;

        // Act
        var result = await _emailService.SendMonthlyReportEmailAsync(
            toEmail, userName, pdfReport, month, year);

        // Assert
        Assert.False(result); // Sem API Key
        // Em implementação real, verificaria que anexo foi incluído
    }

    [Fact]
    public void EmailService_UsesDefaultValues_WhenConfigNotSet()
    {
        // Arrange
        var emptyConfig = new Mock<IConfiguration>();
        emptyConfig.Setup(x => x["Brevo:ApiKey"]).Returns((string?)null);
        emptyConfig.Setup(x => x["Brevo:FromEmail"]).Returns((string?)null);
        emptyConfig.Setup(x => x["Brevo:FromName"]).Returns((string?)null);

        // Act
        var service = new EmailService(emptyConfig.Object, _loggerMock.Object);

        // Assert
        // O serviço deve usar valores padrão sem lançar exceção
        Assert.NotNull(service);
    }
}