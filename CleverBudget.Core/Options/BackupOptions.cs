namespace CleverBudget.Core.Options;

public sealed class BackupOptions
{
    public const string SectionName = "BackupSettings";

    public bool EnableAutomaticBackups { get; set; } = false;
    public string RootPath { get; set; } = "Backups";
    public int RetentionDays { get; set; } = 7;
    public TimeSpan Interval { get; set; } = TimeSpan.FromDays(1);
    public bool RunOnStartup { get; set; } = true;
}
