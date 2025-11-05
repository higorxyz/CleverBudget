using Asp.Versioning;
using CleverBudget.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CleverBudget.Api.Controllers;

[ApiVersion("2.0")]
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
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

    #region CSV Exports

    /// <summary>
    /// Exportar transações para CSV
    /// </summary>
    [HttpGet("transactions/csv")]
    [ProducesResponseType(typeof(FileContentResult), 200)]
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
    [ProducesResponseType(typeof(FileContentResult), 200)]
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
    [ProducesResponseType(typeof(FileContentResult), 200)]
    public async Task<IActionResult> ExportGoalsCsv(
        [FromQuery] int? month = null,
        [FromQuery] int? year = null)
    {
        var userId = GetUserId();
        var csv = await _exportService.ExportGoalsToCsvAsync(userId, month, year);

        var fileName = $"metas_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
        return File(csv, "text/csv", fileName);
    }

    #endregion

    #region PDF Exports

    /// <summary>
    /// Exportar transações para PDF
    /// </summary>
    /// <param name="startDate">Data inicial (formato: yyyy-MM-dd)</param>
    /// <param name="endDate">Data final (formato: yyyy-MM-dd)</param>
    [HttpGet("transactions/pdf")]
    [ProducesResponseType(typeof(FileContentResult), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> ExportTransactionsPdf(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var userId = GetUserId();
        var pdf = await _exportService.ExportTransactionsToPdfAsync(userId, startDate, endDate);

        var fileName = $"transacoes_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
        return File(pdf, "application/pdf", fileName);
    }

    /// <summary>
    /// Exportar relatório financeiro completo para PDF
    /// </summary>
    /// <param name="startDate">Data inicial (formato: yyyy-MM-dd)</param>
    /// <param name="endDate">Data final (formato: yyyy-MM-dd)</param>
    [HttpGet("financial-report/pdf")]
    [ProducesResponseType(typeof(FileContentResult), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> ExportFinancialReportPdf(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var userId = GetUserId();
        var pdf = await _exportService.ExportFinancialReportToPdfAsync(userId, startDate, endDate);

        var fileName = $"relatorio_financeiro_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
        return File(pdf, "application/pdf", fileName);
    }

    /// <summary>
    /// Exportar relatório de metas para PDF
    /// </summary>
    /// <param name="month">Mês (1-12)</param>
    /// <param name="year">Ano</param>
    [HttpGet("goals-report/pdf")]
    [ProducesResponseType(typeof(FileContentResult), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> ExportGoalsReportPdf(
        [FromQuery] int? month = null,
        [FromQuery] int? year = null)
    {
        var userId = GetUserId();
        var pdf = await _exportService.ExportGoalsReportToPdfAsync(userId, month, year);

        var fileName = $"metas_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
        return File(pdf, "application/pdf", fileName);
    }

    #endregion
}