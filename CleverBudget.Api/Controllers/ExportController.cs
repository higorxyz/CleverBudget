using Asp.Versioning;
using CleverBudget.Core.DTOs;
using CleverBudget.Core.Enums;
using CleverBudget.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Globalization;

namespace CleverBudget.Api.Controllers;

[ApiVersion("2.0")]
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class ExportController : ControllerBase
{
    private readonly IExportService _exportService;
    private readonly IExportDeliveryService _exportDeliveryService;

    public ExportController(IExportService exportService, IExportDeliveryService exportDeliveryService)
    {
        _exportService = exportService;
        _exportDeliveryService = exportDeliveryService;
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
        [FromQuery] DateTime? endDate = null,
        [FromQuery] ExportOptionsQuery? export = null)
    {
        var userId = GetUserId();
        var options = BuildOptions(export);
        var csv = await _exportService.ExportTransactionsToCsvAsync(userId, startDate, endDate, options);

        var fileName = $"transacoes_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
        return await DeliverAsync(userId, fileName, "text/csv", csv, options);
    }

    /// <summary>
    /// Exportar categorias para CSV
    /// </summary>
    [HttpGet("categories/csv")]
    [ProducesResponseType(typeof(FileContentResult), 200)]
    public async Task<IActionResult> ExportCategoriesCsv([FromQuery] ExportOptionsQuery? export = null)
    {
        var userId = GetUserId();
        var options = BuildOptions(export);
        var csv = await _exportService.ExportCategoriesToCsvAsync(userId, options);

        var fileName = $"categorias_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
        return await DeliverAsync(userId, fileName, "text/csv", csv, options);
    }

    /// <summary>
    /// Exportar visão consolidada de orçamentos para CSV
    /// </summary>
    [HttpGet("budgets/overview/csv")]
    [ProducesResponseType(typeof(FileContentResult), 200)]
    public async Task<IActionResult> ExportBudgetOverviewCsv(
        [FromQuery] int? year = null,
        [FromQuery] int? month = null,
        [FromQuery] ExportOptionsQuery? export = null)
    {
        var userId = GetUserId();
        var options = BuildOptions(export);
        var csv = await _exportService.ExportBudgetOverviewToCsvAsync(userId, year, month, options);

        var fileName = $"orcamentos_overview_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
        return await DeliverAsync(userId, fileName, "text/csv", csv, options);
    }

    /// <summary>
    /// Exportar metas para CSV
    /// </summary>
    [HttpGet("goals/csv")]
    [ProducesResponseType(typeof(FileContentResult), 200)]
    public async Task<IActionResult> ExportGoalsCsv(
        [FromQuery] int? month = null,
        [FromQuery] int? year = null,
        [FromQuery] ExportOptionsQuery? export = null)
    {
        var userId = GetUserId();
        var options = BuildOptions(export);
        var csv = await _exportService.ExportGoalsToCsvAsync(userId, month, year, options);

        var fileName = $"metas_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
        return await DeliverAsync(userId, fileName, "text/csv", csv, options);
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
        [FromQuery] DateTime? endDate = null,
        [FromQuery] ExportOptionsQuery? export = null)
    {
        var userId = GetUserId();
        var options = BuildOptions(export);
        var pdf = await _exportService.ExportTransactionsToPdfAsync(userId, startDate, endDate, options);

        var fileName = $"transacoes_{DateTime.UtcNow:yyyyMMdd_HHmmss}.pdf";
        return await DeliverAsync(userId, fileName, "application/pdf", pdf, options);
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
        [FromQuery] DateTime? endDate = null,
        [FromQuery] ExportOptionsQuery? export = null)
    {
        var userId = GetUserId();
        var options = BuildOptions(export);
        var pdf = await _exportService.ExportFinancialReportToPdfAsync(userId, startDate, endDate, options);

        var fileName = $"relatorio_financeiro_{DateTime.UtcNow:yyyyMMdd_HHmmss}.pdf";
        return await DeliverAsync(userId, fileName, "application/pdf", pdf, options);
    }

    /// <summary>
    /// Exportar visão consolidada de orçamentos para PDF
    /// </summary>
    [HttpGet("budgets/overview/pdf")]
    [ProducesResponseType(typeof(FileContentResult), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> ExportBudgetOverviewPdf(
        [FromQuery] int? year = null,
        [FromQuery] int? month = null,
        [FromQuery] ExportOptionsQuery? export = null)
    {
        var userId = GetUserId();
        var options = BuildOptions(export);
        var pdf = await _exportService.ExportBudgetOverviewToPdfAsync(userId, year, month, options);

        var fileName = $"orcamentos_overview_{DateTime.UtcNow:yyyyMMdd_HHmmss}.pdf";
        return await DeliverAsync(userId, fileName, "application/pdf", pdf, options);
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
        [FromQuery] int? year = null,
        [FromQuery] ExportOptionsQuery? export = null)
    {
        var userId = GetUserId();
        var options = BuildOptions(export);
        var pdf = await _exportService.ExportGoalsReportToPdfAsync(userId, month, year, options);

        var fileName = $"metas_{DateTime.UtcNow:yyyyMMdd_HHmmss}.pdf";
        return await DeliverAsync(userId, fileName, "application/pdf", pdf, options);
    }

    #endregion

    private async Task<IActionResult> DeliverAsync(string userId, string artifactName, string contentType, byte[] payload, ExportRequestOptions options)
    {
        Response.Headers["Content-Language"] = options.Culture.Name;

        if (options.DeliveryMode == ExportDeliveryMode.Download)
        {
            Response.Headers["Cache-Control"] = $"public,max-age={(int)options.CacheDuration.TotalSeconds}";
            return File(payload, contentType, artifactName);
        }

        var delivery = await _exportDeliveryService.DeliverAsync(userId, artifactName, payload, options);

        if (!delivery.Delivered)
        {
            return Problem(
                statusCode: StatusCodes.Status422UnprocessableEntity,
                title: "Falha na entrega da exportação",
                detail: delivery.Message ?? "Não foi possível entregar o arquivo gerado.");
        }

        return Ok(new
        {
            delivery.Mode,
            delivery.Message,
            delivery.Location,
            ExpiresAt = DateTimeOffset.UtcNow.Add(options.CacheDuration)
        });
    }

    private static ExportRequestOptions BuildOptions(ExportOptionsQuery? export)
    {
        var culture = ResolveCulture(export?.Locale);
        var variant = ResolveVariant(export?.Variant);
        var delivery = ResolveDelivery(export?.Delivery);
        var cache = ResolveCacheDuration(export?.CacheSeconds);
        var currency = string.IsNullOrWhiteSpace(export?.Currency)
            ? culture.NumberFormat.CurrencySymbol
            : export!.Currency!;

        return new ExportRequestOptions
        {
            Culture = culture,
            CurrencySymbol = currency,
            Variant = variant,
            DeliveryMode = delivery,
            Email = export?.Email,
            CacheDuration = cache
        };
    }

    private static CultureInfo ResolveCulture(string? locale)
    {
        if (string.IsNullOrWhiteSpace(locale))
        {
            return CultureInfo.GetCultureInfo("pt-BR");
        }

        try
        {
            return CultureInfo.GetCultureInfo(locale);
        }
        catch (CultureNotFoundException)
        {
            return CultureInfo.GetCultureInfo("pt-BR");
        }
    }

    private static ExportVariant ResolveVariant(string? variant)
    {
        if (string.IsNullOrWhiteSpace(variant))
        {
            return ExportVariant.Detailed;
        }

        return Enum.TryParse<ExportVariant>(variant, true, out var parsed) ? parsed : ExportVariant.Detailed;
    }

    private static ExportDeliveryMode ResolveDelivery(string? delivery)
    {
        if (string.IsNullOrWhiteSpace(delivery))
        {
            return ExportDeliveryMode.Download;
        }

        return Enum.TryParse<ExportDeliveryMode>(delivery, true, out var parsed) ? parsed : ExportDeliveryMode.Download;
    }

    private static TimeSpan ResolveCacheDuration(int? cacheSeconds)
    {
        if (!cacheSeconds.HasValue || cacheSeconds.Value <= 0)
        {
            return TimeSpan.FromMinutes(5);
        }

        return TimeSpan.FromSeconds(cacheSeconds.Value);
    }

    public sealed class ExportOptionsQuery
    {
        public string? Locale { get; set; }
        public string? Variant { get; set; }
        public string? Delivery { get; set; }
        public string? Email { get; set; }
        public string? Currency { get; set; }
        public int? CacheSeconds { get; set; }
    }
}