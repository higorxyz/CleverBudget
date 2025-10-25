using CleverBudget.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CleverBudget.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
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
        return Ok(summary);
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
        return Ok(report);
    }
}