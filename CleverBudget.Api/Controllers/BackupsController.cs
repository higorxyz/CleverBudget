using Asp.Versioning;
using CleverBudget.Api.Extensions;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CleverBudget.Core.Interfaces;
using CleverBudget.Core.Options;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CleverBudget.Api.Controllers;

[ApiVersion("2.0")]
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class BackupsController : ControllerBase
{
    private static readonly Regex SafeFileNameRegex = new("^[a-z0-9\\-_.]+$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private readonly IBackupService _backupService;
    private readonly ILogger<BackupsController> _logger;
    private readonly IOptionsSnapshot<BackupOptions> _options;
    private readonly IHostEnvironment _environment;

    public BackupsController(
        IBackupService backupService,
        ILogger<BackupsController> logger,
        IOptionsSnapshot<BackupOptions> options,
        IHostEnvironment environment)
    {
        _backupService = backupService;
        _logger = logger;
        _options = options;
        _environment = environment;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<BackupFileMetadata>), StatusCodes.Status200OK)]
    public IActionResult ListBackups()
    {
        var root = ResolveBackupPath();
        if (!Directory.Exists(root))
        {
            return Ok(Array.Empty<BackupFileMetadata>());
        }

        var backups = Directory.GetFiles(root, "cleverbudget-backup-*.json.gz")
            .Select(filePath =>
            {
                var info = new FileInfo(filePath);
                return new BackupFileMetadata(
                    info.Name,
                    info.Length,
                    info.CreationTimeUtc);
            })
            .OrderByDescending(file => file.CreatedAtUtc)
            .ToList();

        var etag = EtagGenerator.Create(backups);

        if (this.RequestHasMatchingEtag(etag))
        {
            return this.CachedStatus();
        }

        this.SetEtagHeader(etag);
        return Ok(backups);
    }

    [HttpPost]
    [ProducesResponseType(typeof(BackupCreatedResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateBackup([FromQuery] bool download = false, CancellationToken cancellationToken = default)
    {
        var result = await _backupService.CreateBackupAsync(persistToDisk: !download, cancellationToken);

        if (download)
        {
            return File(result.Content, "application/gzip", result.FileName);
        }

        return Ok(new BackupCreatedResponse(result.FileName, StoredOnDisk: !string.IsNullOrEmpty(result.StoredAt)));
    }

    [HttpGet("{fileName}")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadBackup(string fileName, CancellationToken cancellationToken = default)
    {
        if (!IsSafeFileName(fileName))
        {
            return BadRequest("Arquivo inválido.");
        }

        var root = ResolveBackupPath();
        var fullPath = Path.Combine(root, fileName);

        if (!System.IO.File.Exists(fullPath))
        {
            return NotFound();
        }

        await using var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        await using var memory = new MemoryStream();
        await stream.CopyToAsync(memory, cancellationToken);
        memory.Position = 0;

        return File(memory.ToArray(), "application/gzip", fileName);
    }

    [HttpPost("restore")]
    [RequestFormLimits(ValueLengthLimit = int.MaxValue, MultipartBodyLengthLimit = 1024L * 1024L * 100)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RestoreBackup([FromForm] IFormFile file, CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("Nenhum arquivo foi enviado.");
        }

        await using var stream = file.OpenReadStream();
        await _backupService.RestoreBackupAsync(stream, cancellationToken);

        _logger.LogInformation("Backup {FileName} restaurado via API.", file.FileName);
        return NoContent();
    }

    private string ResolveBackupPath()
    {
        var options = _options.Value;
        if (Path.IsPathRooted(options.RootPath))
        {
            return options.RootPath;
        }

        return Path.Combine(_environment.ContentRootPath, options.RootPath);
    }

    private static bool IsSafeFileName(string fileName)
    {
        return !string.IsNullOrWhiteSpace(fileName) && SafeFileNameRegex.IsMatch(fileName);
    }

    public record BackupFileMetadata(string FileName, long SizeBytes, DateTime CreatedAtUtc)
    {
        public string CreatedAt => CreatedAtUtc.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
    }

    public record BackupCreatedResponse(string FileName, bool StoredOnDisk);
}
