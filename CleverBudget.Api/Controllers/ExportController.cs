using CleverBudget.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CleverBudget.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ExportController : ControllerBase
{
    private readonly IExportService _exportService;

    public ExportController(IExportService exportService)
    {
        _exportService = exportService;
    }

    private string GetUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    }

    /// <summary>
    /// Exportar transações para CSV
    /// </summary>
    [HttpGet("transactions/csv")]
    public async Task<IActionResult> ExportTransactionsCsv(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var userId = GetUserId();
        var csv = await _exportService.ExportTransactionsToCsvAsync(userId, startDate, endDate);

        var fileName = $"transacoes_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
        return File(csv, "text/csv", fileName);
    }

    /// <summary>
    /// Exportar categorias para CSV
    /// </summary>
    [HttpGet("categories/csv")]
    public async Task<IActionResult> ExportCategoriesCsv()
    {
        var userId = GetUserId();
        var csv = await _exportService.ExportCategoriesToCsvAsync(userId);

        var fileName = $"categorias_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
        return File(csv, "text/csv", fileName);
    }

    /// <summary>
    /// Exportar metas para CSV
    /// </summary>
    [HttpGet("goals/csv")]
    public async Task<IActionResult> ExportGoalsCsv(
        [FromQuery] int? month = null,
        [FromQuery] int? year = null)
    {
        var userId = GetUserId();
        var csv = await _exportService.ExportGoalsToCsvAsync(userId, month, year);

        var fileName = $"metas_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
        return File(csv, "text/csv", fileName);
    }
}