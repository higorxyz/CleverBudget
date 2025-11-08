using Asp.Versioning;
using CleverBudget.Api.Extensions;
using CleverBudget.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CleverBudget.Api.Controllers;

[ApiVersion("2.0")]
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    private string GetUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    }

    /// <summary>
    /// Obter resumo geral (receitas, despesas, saldo)
    /// </summary>
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var userId = GetUserId();
        var summary = await _reportService.GetSummaryAsync(userId, startDate, endDate);
        var etag = EtagGenerator.Create(summary);

        if (this.RequestHasMatchingEtag(etag))
        {
            return this.CachedStatus();
        }

        this.SetEtagHeader(etag);
        return Ok(summary);
    }

    /// <summary>
    /// Obter visão consolidada para dashboards (orçamentos, caixa, insights)
    /// </summary>
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard(
        [FromQuery] int? year = null,
        [FromQuery] int? month = null,
        [FromQuery] int budgetTrendMonths = 6,
        [FromQuery] int cashflowMonths = 6)
    {
        var userId = GetUserId();
        var dashboard = await _reportService.GetDashboardOverviewAsync(userId, year, month, budgetTrendMonths, cashflowMonths);
        var etag = EtagGenerator.Create(dashboard);

        if (this.RequestHasMatchingEtag(etag))
        {
            return this.CachedStatus();
        }

        this.SetEtagHeader(etag);
        return Ok(dashboard);
    }

    /// <summary>
    /// Obter relatório por categorias
    /// </summary>
    [HttpGet("categories")]
    public async Task<IActionResult> GetCategoryReport(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] bool expensesOnly = true)
    {
        var userId = GetUserId();
        var report = await _reportService.GetCategoryReportAsync(userId, startDate, endDate, expensesOnly);
        var etag = EtagGenerator.Create(report);

        if (this.RequestHasMatchingEtag(etag))
        {
            return this.CachedStatus();
        }

        this.SetEtagHeader(etag);
        return Ok(report);
    }

    /// <summary>
    /// Obter histórico mensal
    /// </summary>
    [HttpGet("monthly")]
    public async Task<IActionResult> GetMonthlyReport([FromQuery] int months = 12)
    {
        var userId = GetUserId();
        var report = await _reportService.GetMonthlyReportAsync(userId, months);
        var etag = EtagGenerator.Create(report);

        if (this.RequestHasMatchingEtag(etag))
        {
            return this.CachedStatus();
        }

        this.SetEtagHeader(etag);
        return Ok(report);
    }

    /// <summary>
    /// Obter relatório completo e detalhado
    /// </summary>
    [HttpGet("detailed")]
    public async Task<IActionResult> GetDetailedReport(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var userId = GetUserId();
        var report = await _reportService.GetDetailedReportAsync(userId, startDate, endDate);
        var etag = EtagGenerator.Create(report);

        if (this.RequestHasMatchingEtag(etag))
        {
            return this.CachedStatus();
        }

        this.SetEtagHeader(etag);
        return Ok(report);
    }
}