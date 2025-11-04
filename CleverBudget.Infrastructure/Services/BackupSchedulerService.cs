using System;
using CleverBudget.Core.Interfaces;
using CleverBudget.Core.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CleverBudget.Infrastructure.Services;

/// <summary>
/// Background worker that triggers database backups based on <see cref="BackupOptions"/>.
/// </summary>
public sealed class BackupSchedulerService : BackgroundService
{
    private static readonly TimeSpan DisabledPollInterval = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan FallbackInterval = TimeSpan.FromHours(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BackupSchedulerService> _logger;
    private readonly IOptionsMonitor<BackupOptions> _optionsMonitor;

    private bool _startupBackupExecuted;

    public BackupSchedulerService(
        IServiceScopeFactory scopeFactory,
        ILogger<BackupSchedulerService> logger,
        IOptionsMonitor<BackupOptions> optionsMonitor)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _optionsMonitor = optionsMonitor;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var options = _optionsMonitor.CurrentValue;

            if (options.EnableAutomaticBackups && !_startupBackupExecuted && options.RunOnStartup)
            {
                await RunBackupAsync(stoppingToken);
                _startupBackupExecuted = true;
            }

            var interval = options.Interval <= TimeSpan.Zero ? FallbackInterval : options.Interval;

            if (!options.EnableAutomaticBackups)
            {
                _logger.LogDebug("Automatic backups disabled; scheduler sleeping for {Interval}.", DisabledPollInterval);
                try
                {
                    await Task.Delay(DisabledPollInterval, stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    break;
                }

                continue;
            }

            try
            {
                await Task.Delay(interval, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }

            if (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            await RunBackupAsync(stoppingToken);
            _startupBackupExecuted = true;
        }
    }

    private async Task RunBackupAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var backupService = scope.ServiceProvider.GetRequiredService<IBackupService>();

            await backupService.CreateBackupAsync(persistToDisk: true, cancellationToken);
            _logger.LogInformation("Automatic backup completed successfully.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Cancellation requested; no action required.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute automatic backup.");
        }
    }
}
