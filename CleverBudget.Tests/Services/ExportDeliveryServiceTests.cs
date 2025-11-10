using System;
using System.IO;
using CleverBudget.Core.DTOs;
using CleverBudget.Core.Interfaces;
using CleverBudget.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CleverBudget.Tests.Services;

public class ExportDeliveryServiceTests : IDisposable
{
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly Mock<ILogger<ExportDeliveryService>> _loggerMock;
    private readonly ExportDeliveryService _service;
    private readonly string _userId = "user-123";
    private readonly string _artifact = "relatorio.pdf";

    public ExportDeliveryServiceTests()
    {
        _emailServiceMock = new Mock<IEmailService>();
        _loggerMock = new Mock<ILogger<ExportDeliveryService>>();
        _service = new ExportDeliveryService(_emailServiceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task DeliverAsync_DownloadMode_ReturnsNoDelivery()
    {
        var options = new ExportRequestOptions
        {
            DeliveryMode = ExportDeliveryMode.Download
        };

        var result = await _service.DeliverAsync(_userId, _artifact, Array.Empty<byte>(), options);

        Assert.False(result.Delivered);
        Assert.Equal(ExportDeliveryMode.Download, result.Mode);
        Assert.Contains("Nenhuma entrega", result.Message);
        _emailServiceMock.Verify(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task DeliverAsync_EmailModeWithoutAddress_ReturnsFailure()
    {
        var options = new ExportRequestOptions
        {
            DeliveryMode = ExportDeliveryMode.Email,
            Email = null
        };

        var result = await _service.DeliverAsync(_userId, _artifact, Array.Empty<byte>(), options);

        Assert.False(result.Delivered);
        Assert.Equal(ExportDeliveryMode.Email, result.Mode);
        Assert.Equal("Email do destinatário não informado.", result.Message);
        _emailServiceMock.Verify(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task DeliverAsync_EmailMode_SuccessfullySendsAttachment()
    {
        _emailServiceMock
            .Setup(e => e.SendEmailAsync("user@example.com", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        var options = new ExportRequestOptions
        {
            DeliveryMode = ExportDeliveryMode.Email,
            Email = "user@example.com"
        };

        var payload = new byte[] { 1, 2, 3 };

        var result = await _service.DeliverAsync(_userId, _artifact, payload, options);

        Assert.True(result.Delivered);
        Assert.Equal(ExportDeliveryMode.Email, result.Mode);
        Assert.Equal("Arquivo enviado por email.", result.Message);
        _emailServiceMock.Verify(e => e.SendEmailAsync("user@example.com", It.IsAny<string>(), It.IsAny<string>(), payload, _artifact), Times.Once);
    }

    [Fact]
    public async Task DeliverAsync_EmailMode_WhenSendFails_ReturnsFailureMessage()
    {
        _emailServiceMock
            .Setup(e => e.SendEmailAsync("user@example.com", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<string>()))
            .ReturnsAsync(false);

        var options = new ExportRequestOptions
        {
            DeliveryMode = ExportDeliveryMode.Email,
            Email = "user@example.com"
        };

        var result = await _service.DeliverAsync(_userId, _artifact, Array.Empty<byte>(), options);

        Assert.False(result.Delivered);
        Assert.Equal(ExportDeliveryMode.Email, result.Mode);
        Assert.Equal("Falha ao enviar email com o arquivo.", result.Message);
    _emailServiceMock.Verify(e => e.SendEmailAsync("user@example.com", It.IsAny<string>(), It.IsAny<string>(), It.Is<byte[]>(p => p.Length == 0), _artifact), Times.Once);
    }

    [Fact]
    public async Task DeliverAsync_SignedLinkMode_PersistsFileAndReturnsLocation()
    {
        var options = new ExportRequestOptions
        {
            DeliveryMode = ExportDeliveryMode.SignedLink
        };

        var payload = new byte[] { 10, 20, 30 };

        var result = await _service.DeliverAsync(_userId, _artifact, payload, options);

        Assert.True(result.Delivered);
        Assert.Equal(ExportDeliveryMode.SignedLink, result.Mode);
        Assert.Equal("Arquivo disponível para download protegido.", result.Message);
        Assert.NotNull(result.Location);

        var fileName = Path.GetFileName(result.Location);
        var expectedPath = Path.Combine(AppContext.BaseDirectory, "Backups", "Exports", fileName);
        Assert.True(File.Exists(expectedPath));
        Assert.Equal(payload, await File.ReadAllBytesAsync(expectedPath));
    }

    public void Dispose()
    {
        var exportsPath = Path.Combine(AppContext.BaseDirectory, "Backups", "Exports");
        if (Directory.Exists(exportsPath))
        {
            Directory.Delete(exportsPath, recursive: true);
        }
    }
}
