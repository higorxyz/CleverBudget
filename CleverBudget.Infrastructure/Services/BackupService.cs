using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization;
using CleverBudget.Core.DTOs;
using CleverBudget.Core.Entities;
using CleverBudget.Core.Enums;
using CleverBudget.Core.Interfaces;
using CleverBudget.Core.Options;
using CleverBudget.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CleverBudget.Infrastructure.Services;

public class BackupService : IBackupService
{
    private readonly AppDbContext _context;
    private readonly ILogger<BackupService> _logger;
    private readonly BackupOptions _options;
    private readonly IHostEnvironment _environment;
    private readonly JsonSerializerOptions _serializerOptions;

    public BackupService(
        AppDbContext context,
        IOptions<BackupOptions> options,
        ILogger<BackupService> logger,
        IHostEnvironment environment)
    {
        _context = context;
        _logger = logger;
        _options = options.Value;
        _environment = environment;
        _serializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            Converters = { new JsonStringEnumConverter() }
        };
    }

    public async Task<BackupResult> CreateBackupAsync(bool persistToDisk = true, CancellationToken cancellationToken = default)
    {
        var snapshot = new DatabaseSnapshot
        {
            Version = DatabaseSnapshot.CurrentVersion,
            GeneratedAtUtc = DateTime.UtcNow,
            Users = await _context.Users.AsNoTracking()
                .Select(u => new UserSnapshot
                {
                    Id = u.Id,
                    UserName = u.UserName,
                    NormalizedUserName = u.NormalizedUserName,
                    Email = u.Email,
                    NormalizedEmail = u.NormalizedEmail,
                    EmailConfirmed = u.EmailConfirmed,
                    PasswordHash = u.PasswordHash,
                    SecurityStamp = u.SecurityStamp,
                    ConcurrencyStamp = u.ConcurrencyStamp,
                    PhoneNumber = u.PhoneNumber,
                    PhoneNumberConfirmed = u.PhoneNumberConfirmed,
                    TwoFactorEnabled = u.TwoFactorEnabled,
                    LockoutEnd = u.LockoutEnd,
                    LockoutEnabled = u.LockoutEnabled,
                    AccessFailedCount = u.AccessFailedCount,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    PhotoUrl = u.PhotoUrl,
                    CreatedAt = u.CreatedAt
                })
                .ToListAsync(cancellationToken),
            Roles = await _context.Roles.AsNoTracking()
                .Select(r => new RoleSnapshot
                {
                    Id = r.Id,
                    Name = r.Name,
                    NormalizedName = r.NormalizedName,
                    ConcurrencyStamp = r.ConcurrencyStamp
                })
                .ToListAsync(cancellationToken),
            RoleClaims = await _context.RoleClaims.AsNoTracking()
                .Select(rc => new RoleClaimSnapshot
                {
                    Id = rc.Id,
                    RoleId = rc.RoleId,
                    ClaimType = rc.ClaimType,
                    ClaimValue = rc.ClaimValue
                })
                .ToListAsync(cancellationToken),
            UserClaims = await _context.UserClaims.AsNoTracking()
                .Select(uc => new UserClaimSnapshot
                {
                    Id = uc.Id,
                    UserId = uc.UserId,
                    ClaimType = uc.ClaimType,
                    ClaimValue = uc.ClaimValue
                })
                .ToListAsync(cancellationToken),
            UserLogins = await _context.UserLogins.AsNoTracking()
                .Select(ul => new UserLoginSnapshot
                {
                    LoginProvider = ul.LoginProvider,
                    ProviderKey = ul.ProviderKey,
                    ProviderDisplayName = ul.ProviderDisplayName,
                    UserId = ul.UserId
                })
                .ToListAsync(cancellationToken),
            UserTokens = await _context.UserTokens.AsNoTracking()
                .Select(ut => new UserTokenSnapshot
                {
                    UserId = ut.UserId,
                    LoginProvider = ut.LoginProvider,
                    Name = ut.Name,
                    Value = ut.Value
                })
                .ToListAsync(cancellationToken),
            UserRoles = await _context.UserRoles.AsNoTracking()
                .Select(ur => new UserRoleSnapshot
                {
                    UserId = ur.UserId,
                    RoleId = ur.RoleId
                })
                .ToListAsync(cancellationToken),
            Categories = await _context.Categories.AsNoTracking()
                .Select(c => new CategorySnapshot
                {
                    Id = c.Id,
                    UserId = c.UserId,
                    Name = c.Name,
                    Icon = c.Icon,
                    Color = c.Color,
                    IsDefault = c.IsDefault,
                    CreatedAt = c.CreatedAt
                })
                .ToListAsync(cancellationToken),
            Transactions = await _context.Transactions.AsNoTracking()
                .Select(t => new TransactionSnapshot
                {
                    Id = t.Id,
                    UserId = t.UserId,
                    Amount = t.Amount,
                    Type = t.Type,
                    Description = t.Description,
                    CategoryId = t.CategoryId,
                    Date = t.Date,
                    CreatedAt = t.CreatedAt
                })
                .ToListAsync(cancellationToken),
            Goals = await _context.Goals.AsNoTracking()
                .Select(g => new GoalSnapshot
                {
                    Id = g.Id,
                    UserId = g.UserId,
                    CategoryId = g.CategoryId,
                    TargetAmount = g.TargetAmount,
                    Month = g.Month,
                    Year = g.Year,
                    CreatedAt = g.CreatedAt
                })
                .ToListAsync(cancellationToken),
            Budgets = await _context.Budgets.AsNoTracking()
                .Select(b => new BudgetSnapshot
                {
                    Id = b.Id,
                    UserId = b.UserId,
                    CategoryId = b.CategoryId,
                    Amount = b.Amount,
                    Month = b.Month,
                    Year = b.Year,
                    AlertAt50Percent = b.AlertAt50Percent,
                    AlertAt80Percent = b.AlertAt80Percent,
                    AlertAt100Percent = b.AlertAt100Percent,
                    Alert50Sent = b.Alert50Sent,
                    Alert80Sent = b.Alert80Sent,
                    Alert100Sent = b.Alert100Sent,
                    CreatedAt = b.CreatedAt
                })
                .ToListAsync(cancellationToken),
            RecurringTransactions = await _context.RecurringTransactions.AsNoTracking()
                .Select(r => new RecurringTransactionSnapshot
                {
                    Id = r.Id,
                    UserId = r.UserId,
                    Amount = r.Amount,
                    Type = r.Type,
                    Description = r.Description,
                    CategoryId = r.CategoryId,
                    Frequency = r.Frequency,
                    StartDate = r.StartDate,
                    EndDate = r.EndDate,
                    DayOfMonth = r.DayOfMonth,
                    DayOfWeek = r.DayOfWeek,
                    IsActive = r.IsActive,
                    LastGeneratedDate = r.LastGeneratedDate,
                    CreatedAt = r.CreatedAt
                })
                .ToListAsync(cancellationToken)
        };

        var fileName = $"cleverbudget-backup-{snapshot.GeneratedAtUtc:yyyyMMdd-HHmmss}.json";
        await using var memoryStream = new MemoryStream();
        await using (var gzipStream = new GZipStream(memoryStream, CompressionLevel.Optimal, leaveOpen: true))
        {
            await JsonSerializer.SerializeAsync(gzipStream, snapshot, _serializerOptions, cancellationToken);
        }

        memoryStream.Position = 0;
        var compressed = memoryStream.ToArray();
        string? storedPath = null;

        if (persistToDisk)
        {
            storedPath = await PersistToDiskAsync(fileName, compressed, cancellationToken);
            await PruneOldBackupsAsync(cancellationToken);
        }

        _logger.LogInformation("📦 Backup gerado com sucesso ({FileName})", fileName);
        return new BackupResult(fileName + ".gz", compressed, storedPath);
    }

    public async Task RestoreBackupAsync(Stream backupStream, CancellationToken cancellationToken = default)
    {
        if (backupStream == null || !backupStream.CanRead)
        {
            throw new ArgumentException("Backup inválido", nameof(backupStream));
        }

        await using var gzipStream = new GZipStream(backupStream, CompressionMode.Decompress, leaveOpen: true);
        var snapshot = await JsonSerializer.DeserializeAsync<DatabaseSnapshot>(gzipStream, _serializerOptions, cancellationToken);

        if (snapshot == null)
        {
            throw new InvalidOperationException("Arquivo de backup corrompido ou incompatível");
        }

        if (snapshot.Version != DatabaseSnapshot.CurrentVersion)
        {
            _logger.LogWarning("⚠️ Versão de backup diferente da versão atual ({BackupVersion} != {CurrentVersion})", snapshot.Version, DatabaseSnapshot.CurrentVersion);
        }

        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var restoreIdentityData = snapshot.Users.Count > 0
                || snapshot.Roles.Count > 0
                || snapshot.UserRoles.Count > 0
                || snapshot.UserClaims.Count > 0
                || snapshot.UserLogins.Count > 0
                || snapshot.UserTokens.Count > 0
                || snapshot.RoleClaims.Count > 0;

            if (!restoreIdentityData)
            {
                var requiredUserIds = snapshot.Categories.Select(c => c.UserId)
                    .Concat(snapshot.Budgets.Select(b => b.UserId))
                    .Concat(snapshot.Goals.Select(g => g.UserId))
                    .Concat(snapshot.RecurringTransactions.Select(r => r.UserId))
                    .Concat(snapshot.Transactions.Select(t => t.UserId))
                    .Where(id => !string.IsNullOrWhiteSpace(id))
                    .ToHashSet(StringComparer.Ordinal);

                if (requiredUserIds.Count > 0)
                {
                    var existingUserIds = await _context.Users
                        .Where(u => requiredUserIds.Contains(u.Id))
                        .Select(u => u.Id)
                        .ToListAsync(cancellationToken);

                    if (existingUserIds.Count != requiredUserIds.Count)
                    {
                        throw new InvalidOperationException("O backup não contém dados de identidade e o banco alvo não possui todas as contas referenciadas. Gere um novo backup com a versão atualizada antes de restaurar em um ambiente limpo.");
                    }
                }
            }

            await ClearExistingDataAsync(restoreIdentityData, cancellationToken);
            await InsertSnapshotAsync(snapshot, restoreIdentityData, cancellationToken);
            await ResetSequencesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            if (!restoreIdentityData)
            {
                _logger.LogWarning("⚠️ Backup restaurado sem dados de identidade. Contas existentes foram mantidas.");
            }

            _logger.LogInformation("✅ Backup restaurado com sucesso ({GeneratedAt})", snapshot.GeneratedAtUtc);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError("❌ Falha ao restaurar backup. Transação revertida.");
            throw;
        }
    }

    private async Task<string?> PersistToDiskAsync(string fileName, byte[] content, CancellationToken cancellationToken)
    {
        var root = ResolveBackupPath();
        Directory.CreateDirectory(root);

        var fullPath = Path.Combine(root, fileName + ".gz");
        await File.WriteAllBytesAsync(fullPath, content, cancellationToken);
        return fullPath;
    }

    private string ResolveBackupPath()
    {
        if (Path.IsPathRooted(_options.RootPath))
        {
            return _options.RootPath;
        }

        return Path.Combine(_environment.ContentRootPath, _options.RootPath);
    }

    private Task PruneOldBackupsAsync(CancellationToken cancellationToken)
    {
        if (_options.RetentionDays <= 0)
        {
            return Task.CompletedTask;
        }

        var directory = ResolveBackupPath();
        if (!Directory.Exists(directory))
        {
            return Task.CompletedTask;
        }

        var threshold = DateTime.UtcNow.AddDays(-_options.RetentionDays);
        foreach (var file in Directory.GetFiles(directory, "cleverbudget-backup-*.json.gz"))
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            var info = new FileInfo(file);
            if (info.CreationTimeUtc < threshold)
            {
                try
                {
                    info.Delete();
                    _logger.LogInformation("🧹 Backup antigo removido: {File}", info.Name);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Não foi possível remover backup antigo {File}", info.Name);
                }
            }
        }

        return Task.CompletedTask;
    }

    private async Task ClearExistingDataAsync(bool clearIdentityData, CancellationToken cancellationToken)
    {
        if (clearIdentityData)
        {
            _context.UserTokens.RemoveRange(_context.UserTokens);
            _context.UserLogins.RemoveRange(_context.UserLogins);
            _context.UserClaims.RemoveRange(_context.UserClaims);
            _context.UserRoles.RemoveRange(_context.UserRoles);
            _context.RoleClaims.RemoveRange(_context.RoleClaims);
        }

        _context.Transactions.RemoveRange(_context.Transactions);
        _context.RecurringTransactions.RemoveRange(_context.RecurringTransactions);
        _context.Budgets.RemoveRange(_context.Budgets);
        _context.Goals.RemoveRange(_context.Goals);
        _context.Categories.RemoveRange(_context.Categories);

        if (clearIdentityData)
        {
            _context.Roles.RemoveRange(_context.Roles);
            _context.Users.RemoveRange(_context.Users);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task InsertSnapshotAsync(DatabaseSnapshot snapshot, bool restoreIdentityData, CancellationToken cancellationToken)
    {
        if (restoreIdentityData && snapshot.Roles.Count > 0)
        {
            var roles = snapshot.Roles.Select(r => new IdentityRole
            {
                Id = r.Id,
                Name = r.Name,
                NormalizedName = r.NormalizedName,
                ConcurrencyStamp = r.ConcurrencyStamp
            }).ToList();

            _context.Roles.AddRange(roles);
        }

        if (restoreIdentityData && snapshot.Users.Count > 0)
        {
            var users = snapshot.Users.Select(u => new User
            {
                Id = u.Id,
                UserName = u.UserName,
                NormalizedUserName = u.NormalizedUserName,
                Email = u.Email,
                NormalizedEmail = u.NormalizedEmail,
                EmailConfirmed = u.EmailConfirmed,
                PasswordHash = u.PasswordHash,
                SecurityStamp = u.SecurityStamp,
                ConcurrencyStamp = u.ConcurrencyStamp,
                PhoneNumber = u.PhoneNumber,
                PhoneNumberConfirmed = u.PhoneNumberConfirmed,
                TwoFactorEnabled = u.TwoFactorEnabled,
                LockoutEnd = u.LockoutEnd,
                LockoutEnabled = u.LockoutEnabled,
                AccessFailedCount = u.AccessFailedCount,
                FirstName = u.FirstName,
                LastName = u.LastName,
                PhotoUrl = u.PhotoUrl,
                CreatedAt = u.CreatedAt
            }).ToList();

            _context.Users.AddRange(users);
        }

        if (restoreIdentityData && (snapshot.Roles.Count > 0 || snapshot.Users.Count > 0))
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        if (restoreIdentityData && snapshot.RoleClaims.Count > 0)
        {
            var roleClaims = snapshot.RoleClaims.Select(rc => new IdentityRoleClaim<string>
            {
                Id = rc.Id,
                RoleId = rc.RoleId,
                ClaimType = rc.ClaimType,
                ClaimValue = rc.ClaimValue
            }).ToList();

            _context.RoleClaims.AddRange(roleClaims);
        }

        if (restoreIdentityData && snapshot.UserClaims.Count > 0)
        {
            var userClaims = snapshot.UserClaims.Select(uc => new IdentityUserClaim<string>
            {
                Id = uc.Id,
                UserId = uc.UserId,
                ClaimType = uc.ClaimType,
                ClaimValue = uc.ClaimValue
            }).ToList();

            _context.UserClaims.AddRange(userClaims);
        }

        if (restoreIdentityData && snapshot.UserLogins.Count > 0)
        {
            var userLogins = snapshot.UserLogins.Select(ul => new IdentityUserLogin<string>
            {
                LoginProvider = ul.LoginProvider,
                ProviderKey = ul.ProviderKey,
                ProviderDisplayName = ul.ProviderDisplayName,
                UserId = ul.UserId
            }).ToList();

            _context.UserLogins.AddRange(userLogins);
        }

        if (restoreIdentityData && snapshot.UserTokens.Count > 0)
        {
            var userTokens = snapshot.UserTokens.Select(ut => new IdentityUserToken<string>
            {
                UserId = ut.UserId,
                LoginProvider = ut.LoginProvider,
                Name = ut.Name,
                Value = ut.Value
            }).ToList();

            _context.UserTokens.AddRange(userTokens);
        }

        if (restoreIdentityData && snapshot.UserRoles.Count > 0)
        {
            var userRoles = snapshot.UserRoles.Select(ur => new IdentityUserRole<string>
            {
                UserId = ur.UserId,
                RoleId = ur.RoleId
            }).ToList();

            _context.UserRoles.AddRange(userRoles);
        }

        if (restoreIdentityData && (snapshot.RoleClaims.Count > 0 || snapshot.UserClaims.Count > 0 || snapshot.UserLogins.Count > 0 || snapshot.UserTokens.Count > 0 || snapshot.UserRoles.Count > 0))
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        if (snapshot.Categories.Count > 0)
        {
            var categories = snapshot.Categories.Select(c => new Category
            {
                Id = c.Id,
                UserId = c.UserId,
                Name = c.Name,
                Icon = c.Icon,
                Color = c.Color,
                IsDefault = c.IsDefault,
                CreatedAt = c.CreatedAt
            }).ToList();

            _context.Categories.AddRange(categories);
        }

        if (snapshot.Budgets.Count > 0)
        {
            var budgets = snapshot.Budgets.Select(b => new Budget
            {
                Id = b.Id,
                UserId = b.UserId,
                CategoryId = b.CategoryId,
                Amount = b.Amount,
                Month = b.Month,
                Year = b.Year,
                AlertAt50Percent = b.AlertAt50Percent,
                AlertAt80Percent = b.AlertAt80Percent,
                AlertAt100Percent = b.AlertAt100Percent,
                Alert50Sent = b.Alert50Sent,
                Alert80Sent = b.Alert80Sent,
                Alert100Sent = b.Alert100Sent,
                CreatedAt = b.CreatedAt
            }).ToList();

            _context.Budgets.AddRange(budgets);
        }

        if (snapshot.Goals.Count > 0)
        {
            var goals = snapshot.Goals.Select(g => new Goal
            {
                Id = g.Id,
                UserId = g.UserId,
                CategoryId = g.CategoryId,
                TargetAmount = g.TargetAmount,
                Month = g.Month,
                Year = g.Year,
                CreatedAt = g.CreatedAt
            }).ToList();

            _context.Goals.AddRange(goals);
        }

        if (snapshot.RecurringTransactions.Count > 0)
        {
            var recurring = snapshot.RecurringTransactions.Select(r => new RecurringTransaction
            {
                Id = r.Id,
                UserId = r.UserId,
                Amount = r.Amount,
                Type = r.Type,
                Description = r.Description,
                CategoryId = r.CategoryId,
                Frequency = r.Frequency,
                StartDate = r.StartDate,
                EndDate = r.EndDate,
                DayOfMonth = r.DayOfMonth,
                DayOfWeek = r.DayOfWeek,
                IsActive = r.IsActive,
                LastGeneratedDate = r.LastGeneratedDate,
                CreatedAt = r.CreatedAt
            }).ToList();

            _context.RecurringTransactions.AddRange(recurring);
        }

        if (snapshot.Transactions.Count > 0)
        {
            var transactions = snapshot.Transactions.Select(t => new Transaction
            {
                Id = t.Id,
                UserId = t.UserId,
                Amount = t.Amount,
                Type = t.Type,
                Description = t.Description,
                CategoryId = t.CategoryId,
                Date = t.Date,
                CreatedAt = t.CreatedAt
            }).ToList();

            _context.Transactions.AddRange(transactions);
        }

        if (snapshot.Categories.Count > 0 || snapshot.Budgets.Count > 0 || snapshot.Goals.Count > 0 || snapshot.RecurringTransactions.Count > 0 || snapshot.Transactions.Count > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task ResetSequencesAsync(CancellationToken cancellationToken)
    {
        if (!_context.Database.IsNpgsql())
        {
            return;
        }

        var tables = new[] { "Categories", "Budgets", "Goals", "RecurringTransactions", "Transactions" };
        foreach (var table in tables)
        {
            var sql = $"SELECT setval(pg_get_serial_sequence('\"{table}\"','\"Id\"'), COALESCE((SELECT MAX(\"Id\") FROM \"{table}\"), 1))";
            await _context.Database.ExecuteSqlRawAsync(sql, cancellationToken);
        }
    }

    private sealed class DatabaseSnapshot
    {
    public const int CurrentVersion = 2;

        public int Version { get; set; } = CurrentVersion;
        public DateTime GeneratedAtUtc { get; set; }
        public List<UserSnapshot> Users { get; set; } = new();
        public List<RoleSnapshot> Roles { get; set; } = new();
        public List<UserRoleSnapshot> UserRoles { get; set; } = new();
        public List<UserClaimSnapshot> UserClaims { get; set; } = new();
        public List<UserLoginSnapshot> UserLogins { get; set; } = new();
        public List<UserTokenSnapshot> UserTokens { get; set; } = new();
        public List<RoleClaimSnapshot> RoleClaims { get; set; } = new();
        public List<CategorySnapshot> Categories { get; set; } = new();
        public List<TransactionSnapshot> Transactions { get; set; } = new();
        public List<GoalSnapshot> Goals { get; set; } = new();
        public List<BudgetSnapshot> Budgets { get; set; } = new();
        public List<RecurringTransactionSnapshot> RecurringTransactions { get; set; } = new();
    }

    private sealed class UserSnapshot
    {
        public string Id { get; set; } = string.Empty;
        public string? UserName { get; set; }
        public string? NormalizedUserName { get; set; }
        public string? Email { get; set; }
        public string? NormalizedEmail { get; set; }
        public bool EmailConfirmed { get; set; }
        public string? PasswordHash { get; set; }
        public string? SecurityStamp { get; set; }
        public string? ConcurrencyStamp { get; set; }
        public string? PhoneNumber { get; set; }
        public bool PhoneNumberConfirmed { get; set; }
        public bool TwoFactorEnabled { get; set; }
        public DateTimeOffset? LockoutEnd { get; set; }
        public bool LockoutEnabled { get; set; }
        public int AccessFailedCount { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? PhotoUrl { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    private sealed class RoleSnapshot
    {
        public string Id { get; set; } = string.Empty;
        public string? Name { get; set; }
        public string? NormalizedName { get; set; }
        public string? ConcurrencyStamp { get; set; }
    }

    private sealed class RoleClaimSnapshot
    {
        public int Id { get; set; }
        public string RoleId { get; set; } = string.Empty;
        public string? ClaimType { get; set; }
        public string? ClaimValue { get; set; }
    }

    private sealed class UserRoleSnapshot
    {
        public string UserId { get; set; } = string.Empty;
        public string RoleId { get; set; } = string.Empty;
    }

    private sealed class UserClaimSnapshot
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string? ClaimType { get; set; }
        public string? ClaimValue { get; set; }
    }

    private sealed class UserLoginSnapshot
    {
        public string LoginProvider { get; set; } = string.Empty;
        public string ProviderKey { get; set; } = string.Empty;
        public string? ProviderDisplayName { get; set; }
        public string UserId { get; set; } = string.Empty;
    }

    private sealed class UserTokenSnapshot
    {
        public string UserId { get; set; } = string.Empty;
        public string LoginProvider { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Value { get; set; }
    }

    private sealed class CategorySnapshot
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Icon { get; set; }
        public string? Color { get; set; }
        public bool IsDefault { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    private sealed class TransactionSnapshot
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public TransactionType Type { get; set; }
        public string Description { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public DateTime Date { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    private sealed class GoalSnapshot
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public decimal TargetAmount { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    private sealed class BudgetSnapshot
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public decimal Amount { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        public bool AlertAt50Percent { get; set; }
        public bool AlertAt80Percent { get; set; }
        public bool AlertAt100Percent { get; set; }
        public bool Alert50Sent { get; set; }
        public bool Alert80Sent { get; set; }
        public bool Alert100Sent { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    private sealed class RecurringTransactionSnapshot
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public TransactionType Type { get; set; }
        public string Description { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public RecurrenceFrequency Frequency { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? DayOfMonth { get; set; }
        public DayOfWeek? DayOfWeek { get; set; }
        public bool IsActive { get; set; }
        public DateTime? LastGeneratedDate { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
