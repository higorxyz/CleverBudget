namespace CleverBudget.Core.DTOs;

public record BackupResult(string FileName, byte[] Content, string? StoredAt = null);
