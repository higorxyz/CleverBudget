using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CleverBudget.Api.Controllers;
using CleverBudget.Core.DTOs;
using CleverBudget.Core.Interfaces;
using CleverBudget.Core.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace CleverBudget.Tests.Controllers;

public class BackupsControllerTests
{
    [Fact]
    public async Task CreateBackup_WhenPersistedOnDisk_HidesPhysicalPath()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"cleverbudget-controller-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempRoot);

        var backupServiceMock = new Mock<IBackupService>();
        var optionsMock = new Mock<IOptionsSnapshot<BackupOptions>>();
        var environmentMock = new Mock<IHostEnvironment>();
        var loggerMock = new Mock<ILogger<BackupsController>>();

        optionsMock.Setup(o => o.Value).Returns(new BackupOptions
        {
            RootPath = "Backups"
        });

        environmentMock.SetupGet(e => e.ContentRootPath).Returns(tempRoot);

        var storedPath = Path.Combine(tempRoot, "Backups", "cleverbudget-backup-20250101.json.gz");
        backupServiceMock
            .Setup(s => s.CreateBackupAsync(true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BackupResult("cleverbudget-backup-20250101.json.gz", new byte[] { 1 }, storedPath));

        var controller = new BackupsController(
            backupServiceMock.Object,
            loggerMock.Object,
            optionsMock.Object,
            environmentMock.Object);

        var result = await controller.CreateBackup(download: false, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<BackupsController.BackupCreatedResponse>(ok.Value);
        Assert.True(response.StoredOnDisk);

        try
        {
            Directory.Delete(tempRoot, recursive: true);
        }
        catch
        {
            // Ignorar falhas de limpeza em ambientes de CI.
        }
    }
}
