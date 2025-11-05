using System;
using Asp.Versioning;
using CleverBudget.Api.Extensions;
using CleverBudget.Core.Common;
using CleverBudget.Core.DTOs;
using CleverBudget.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CleverBudget.Api.Controllers;

[ApiVersion("2.0")]
[Authorize]
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
public class BudgetsController : ControllerBase
{
    private readonly IBudgetService _budgetService;
    private readonly ILogger<BudgetsController> _logger;

    public BudgetsController(IBudgetService budgetService, ILogger<BudgetsController> logger)
    {
        _budgetService = budgetService;
        _logger = logger;
    }

    private string GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    /// <summary>
    /// Lista todos os orçamentos do usuário
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<BudgetResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int? year,
        [FromQuery] int? month,
        [FromQuery] string? scope = null,
        [FromQuery] string? view = null)
    {
        var userId = GetUserId();

        if (string.Equals(view, "summary", StringComparison.OrdinalIgnoreCase))
        {
            var now = DateTime.UtcNow;
            var targetMonth = month ?? now.Month;
            var targetYear = year ?? now.Year;

            var totalBudget = await _budgetService.GetTotalBudgetForMonthAsync(userId, targetMonth, targetYear);
            var totalSpent = await _budgetService.GetTotalSpentForMonthAsync(userId, targetMonth, targetYear);
            var remaining = totalBudget - totalSpent;
            var percentageUsed = totalBudget > 0 ? (totalSpent / totalBudget) * 100 : 0;

            var summary = new
            {
                month = targetMonth,
                year = targetYear,
                totalBudget,
                totalSpent,
                remaining,
                percentageUsed = Math.Round(percentageUsed, 2),
                status = percentageUsed >= 100 ? "Excedido"
                       : percentageUsed >= 80 ? "Crítico"
                       : percentageUsed >= 50 ? "Alerta"
                       : "Normal"
            };

            var summaryEtag = EtagGenerator.Create(summary);
            if (this.RequestHasMatchingEtag(summaryEtag))
            {
                return this.CachedStatus();
            }

            this.SetEtagHeader(summaryEtag);
            return Ok(summary);
        }

        if (string.Equals(scope, "current", StringComparison.OrdinalIgnoreCase))
        {
            var currentBudgets = await _budgetService.GetCurrentMonthBudgetsAsync(userId);
            var currentEtag = EtagGenerator.Create(currentBudgets);

            if (this.RequestHasMatchingEtag(currentEtag))
            {
                return this.CachedStatus();
            }

            this.SetEtagHeader(currentEtag);
            return Ok(currentBudgets);
        }

        var budgets = await _budgetService.GetAllAsync(userId, year, month);
        var etag = EtagGenerator.Create(budgets);

        if (this.RequestHasMatchingEtag(etag))
        {
            return this.CachedStatus();
        }

        this.SetEtagHeader(etag);
        return Ok(budgets);
    }

    /// <summary>
    /// Lista orçamentos paginados
    /// </summary>
    [HttpGet("paged")]
    [ProducesResponseType(typeof(PagedResult<BudgetResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPaged(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortOrder = "desc",
        [FromQuery] int? year = null,
        [FromQuery] int? month = null)
    {
        var userId = GetUserId();
        var paginationParams = new PaginationParams
        {
            Page = page,
            PageSize = pageSize,
            SortBy = sortBy,
            SortOrder = sortOrder
        };

        var result = await _budgetService.GetPagedAsync(userId, paginationParams, year, month);
        var etag = EtagGenerator.Create(result);

        if (this.RequestHasMatchingEtag(etag))
        {
            return this.CachedStatus();
        }

        this.SetEtagHeader(etag);
        return Ok(result);
    }

    /// <summary>
    /// Busca orçamento por ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(BudgetResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var userId = GetUserId();
        var budget = await _budgetService.GetByIdAsync(id, userId);

        if (budget == null)
            return NotFound(new { message = "Orçamento não encontrado" });

        var etag = EtagGenerator.Create(budget);

        if (this.RequestHasMatchingEtag(etag))
        {
            return this.CachedStatus();
        }

        this.SetEtagHeader(etag);
        return Ok(budget);
    }

    /// <summary>
    /// Busca orçamento por categoria e período
    /// </summary>
    [HttpGet("category/{categoryId}/period")]
    [ProducesResponseType(typeof(BudgetResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByCategoryAndPeriod(
        int categoryId, 
        [FromQuery] int month, 
        [FromQuery] int year)
    {
        var userId = GetUserId();
        var budget = await _budgetService.GetByCategoryAndPeriodAsync(categoryId, month, year, userId);

        if (budget == null)
            return NotFound(new { message = "Orçamento não encontrado para esta categoria e período" });

        var etag = EtagGenerator.Create(budget);

        if (this.RequestHasMatchingEtag(etag))
        {
            return this.CachedStatus();
        }

        this.SetEtagHeader(etag);
        return Ok(budget);
    }

    /// <summary>
    /// Cria novo orçamento
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(BudgetResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateBudgetDto dto)
    {
        var userId = GetUserId();
        var budget = await _budgetService.CreateAsync(dto, userId);

        if (budget == null)
            return BadRequest(new { message = "Não foi possível criar o orçamento. Verifique se a categoria existe e se já não existe orçamento para esta categoria neste período." });

        return CreatedAtAction(nameof(GetById), new { id = budget.Id }, budget);
    }

    /// <summary>
    /// Atualiza orçamento existente
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(BudgetResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateBudgetDto dto)
    {
        var userId = GetUserId();
        var budget = await _budgetService.UpdateAsync(id, dto, userId);

        if (budget == null)
            return NotFound(new { message = "Orçamento não encontrado" });

        return Ok(budget);
    }

    /// <summary>
    /// Exclui orçamento
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = GetUserId();
        var success = await _budgetService.DeleteAsync(id, userId);

        if (!success)
            return NotFound(new { message = "Orçamento não encontrado" });

        return NoContent();
    }
}
