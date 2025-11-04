using CleverBudget.Core.DTOs;

namespace CleverBudget.Core.Interfaces;

public interface IBackupService
{
    Task<BackupResult> CreateBackupAsync(bool persistToDisk = true, CancellationToken cancellationToken = default);
    Task RestoreBackupAsync(Stream backupStream, CancellationToken cancellationToken = default);
}
