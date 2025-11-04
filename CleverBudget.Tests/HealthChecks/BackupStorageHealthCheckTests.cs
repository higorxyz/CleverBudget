using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CleverBudget.Api.HealthChecks;
using CleverBudget.Core.Options;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Xunit;

namespace CleverBudget.Tests.HealthChecks;

public sealed class BackupStorageHealthCheckTests : IDisposable
{
    private readonly string _tempRoot;

    public BackupStorageHealthCheckTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "cleverbudget-health", Guid.NewGuid().ToString("N"));
    }

    [Fact]
    public async Task CheckHealthAsync_WhenDirectoryExists_ReturnsHealthy()
    {
        Directory.CreateDirectory(_tempRoot);

        var options = Options.Create(new BackupOptions
        {
            RootPath = _tempRoot
        });

        var env = new TestHostEnvironment(Path.GetTempPath(), "Development");
        var healthCheck = new BackupStorageHealthCheck(options, env);

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.True(result.Data.TryGetValue("path", out var path) && path?.ToString() == _tempRoot);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenDirectoryInvalid_ReturnsUnhealthy()
    {
        var invalidSegment = new string(Path.GetInvalidFileNameChars().Distinct().DefaultIfEmpty('*').ToArray());
        var options = Options.Create(new BackupOptions
        {
            RootPath = Path.Combine("Backups", invalidSegment)
        });

        var env = new TestHostEnvironment(Path.GetTempPath(), "Development");
        var healthCheck = new BackupStorageHealthCheck(options, env);

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
        {
            Directory.Delete(_tempRoot, recursive: true);
        }
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public TestHostEnvironment(string contentRootPath, string environmentName)
        {
            ContentRootPath = contentRootPath;
            EnvironmentName = environmentName;
            ApplicationName = "CleverBudget.Tests";
            ContentRootFileProvider = new PhysicalFileProvider(contentRootPath);
        }

        public string EnvironmentName { get; set; }
        public string ApplicationName { get; set; }
        public string ContentRootPath { get; set; }
        public IFileProvider ContentRootFileProvider { get; set; }
    }
}
