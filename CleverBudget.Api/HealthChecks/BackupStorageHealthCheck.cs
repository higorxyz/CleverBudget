using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CleverBudget.Core.Options;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace CleverBudget.Api.HealthChecks;

public sealed class BackupStorageHealthCheck : IHealthCheck
{
    private readonly BackupOptions _options;
    private readonly IHostEnvironment _environment;

    public BackupStorageHealthCheck(IOptions<BackupOptions> options, IHostEnvironment environment)
    {
        _options = options.Value;
        _environment = environment;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var path = ResolveBackupPath();
            var directory = new DirectoryInfo(path);

            if (!directory.Exists)
            {
                directory.Create();
            }

            if (!directory.Exists)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy($"Backup directory '{path}' could not be created."));
            }

            long availableSpaceBytes = -1;
            try
            {
                var drive = new DriveInfo(directory.Root.FullName);
                availableSpaceBytes = drive.AvailableFreeSpace;
            }
            catch
            {
                // Ignore if unable to determine drive info.
            }

            var data = new Dictionary<string, object>
            {
                ["path"] = directory.FullName,
                ["availableSpaceBytes"] = availableSpaceBytes
            };

            return Task.FromResult(HealthCheckResult.Healthy("Backup storage is reachable.", data));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("Backup storage check failed.", ex));
        }
    }

    private string ResolveBackupPath()
    {
        if (Path.IsPathRooted(_options.RootPath))
        {
            return _options.RootPath;
        }

        return Path.Combine(_environment.ContentRootPath, _options.RootPath);
    }
}
