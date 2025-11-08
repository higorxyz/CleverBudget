using Asp.Versioning;
using CleverBudget.Api.Extensions;
using CleverBudget.Core.Common;
using CleverBudget.Core.DTOs;
using CleverBudget.Core.Enums;
using CleverBudget.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Security.Claims;

namespace CleverBudget.Api.Controllers;

[ApiVersion("2.0")]
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class GoalsController : ControllerBase
{
    private readonly IGoalService _goalService;

    public GoalsController(IGoalService goalService)
    {
        _goalService = goalService;
    }

    private string GetUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    }

    /// <summary>
    /// Listar metas com paginação
    /// </summary>
    /// <param name="page">Número da página (padrão: 1)</param>
    /// <param name="pageSize">Itens por página (padrão: 10, máximo: 100)</param>
    /// <param name="sortBy">Campo para ordenação: targetAmount, category, month, year</param>
    /// <param name="sortOrder">Ordem: asc ou desc (padrão: desc)</param>
    /// <param name="month">Filtrar por mês (1-12)</param>
    /// <param name="year">Filtrar por ano</param>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<GoalResponseDto>), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortOrder = "desc",
        [FromQuery] int? month = null,
        [FromQuery] int? year = null,
        [FromQuery] int? categoryId = null,
        [FromQuery] CategoryKind? kind = null)
    {
        var userId = GetUserId();
        
        var paginationParams = new PaginationParams
        {
            Page = page,
            PageSize = pageSize,
            SortBy = sortBy,
            SortOrder = sortOrder
        };

        var result = await _goalService.GetPagedAsync(userId, paginationParams, month, year, categoryId, kind);
        var etag = EtagGenerator.Create(result);

        if (this.RequestHasMatchingEtag(etag))
        {
            return this.CachedStatus();
        }

        this.SetEtagHeader(etag);
        return Ok(result);
    }

    /// <summary>
    /// Listar todas as metas (sem paginação)
    /// </summary>
    [HttpGet("all")]
    public async Task<IActionResult> GetAllWithoutPagination(
        [FromQuery] int? month = null,
        [FromQuery] int? year = null,
        [FromQuery] int? categoryId = null,
        [FromQuery] CategoryKind? kind = null)
    {
        var userId = GetUserId();
        var goals = await _goalService.GetAllAsync(userId, month, year, categoryId, kind);
        var etag = EtagGenerator.Create(goals);

        if (this.RequestHasMatchingEtag(etag))
        {
            return this.CachedStatus();
        }

        this.SetEtagHeader(etag);
        return Ok(goals);
    }

    /// <summary>
    /// Obter meta por ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var userId = GetUserId();
        var goal = await _goalService.GetByIdAsync(id, userId);

        if (goal == null)
            return NotFound(new { message = "Meta não encontrada." });

        var etag = EtagGenerator.Create(goal);

        if (this.RequestHasMatchingEtag(etag))
        {
            return this.CachedStatus();
        }

        this.SetEtagHeader(etag);
        return Ok(goal);
    }

    /// <summary>
    /// Criar nova meta mensal
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateGoalDto dto)
    {
        var userId = GetUserId();
        var goal = await _goalService.CreateAsync(dto, userId);

        if (goal == null)
            return BadRequest(new { message = "Categoria inválida ou já existe meta para essa categoria neste mês/ano." });

        return CreatedAtAction(nameof(GetById), new { id = goal.Id }, goal);
    }

    /// <summary>
    /// Atualizar meta
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateGoalDto dto)
    {
        var userId = GetUserId();
        var existing = await _goalService.GetByIdAsync(id, userId);

        if (existing == null)
            return NotFound(new { message = "Meta não encontrada." });

        if (!Request.Headers.TryGetValue("If-Match", out var etagHeader))
        {
            return StatusCode(StatusCodes.Status428PreconditionRequired, new { message = "Cabeçalho If-Match é obrigatório." });
        }

        var currentEtag = EtagGenerator.Create(existing);
        var providedEtag = etagHeader.FirstOrDefault()?.Trim('"');

        if (string.IsNullOrWhiteSpace(providedEtag) || !string.Equals(providedEtag, currentEtag, StringComparison.Ordinal))
        {
            return StatusCode(StatusCodes.Status412PreconditionFailed, new { message = "A versão da meta está desatualizada." });
        }

        var goal = await _goalService.UpdateAsync(id, dto, userId);

        if (goal == null)
            return NotFound(new { message = "Meta não encontrada." });

        var newEtag = EtagGenerator.Create(goal);
        this.SetEtagHeader(newEtag);

        return Ok(goal);
    }

    /// <summary>
    /// Deletar meta
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = GetUserId();
        var success = await _goalService.DeleteAsync(id, userId);

        if (!success)
            return NotFound(new { message = "Meta não encontrada." });

        return NoContent();
    }

    /// <summary>
    /// Obter status de todas as metas (com progresso calculado)
    /// </summary>
    [HttpGet("status")]
    public async Task<IActionResult> GetStatus([FromQuery] int? month = null, [FromQuery] int? year = null)
    {
        var userId = GetUserId();
        var status = await _goalService.GetStatusAsync(userId, month, year);
        var etag = EtagGenerator.Create(status);

        if (this.RequestHasMatchingEtag(etag))
        {
            return this.CachedStatus();
        }

        this.SetEtagHeader(etag);
        return Ok(status);
    }

    /// <summary>
    /// Obter visão de metas atrasadas, em risco e concluídas
    /// </summary>
    [HttpGet("insights")]
    public async Task<IActionResult> GetInsights(
        [FromQuery] int? month = null,
        [FromQuery] int? year = null,
        [FromQuery] int? categoryId = null,
        [FromQuery] CategoryKind? kind = null,
        [FromQuery] decimal riskThreshold = 80)
    {
        var userId = GetUserId();
        var filter = new GoalInsightsFilterDto
        {
            Month = month,
            Year = year,
            CategoryId = categoryId,
            CategoryKind = kind,
            RiskThresholdPercentage = riskThreshold
        };

        var insights = await _goalService.GetInsightsAsync(userId, filter);
        var etag = EtagGenerator.Create(insights);

        if (this.RequestHasMatchingEtag(etag))
        {
            return this.CachedStatus();
        }

        this.SetEtagHeader(etag);
        return Ok(insights);
    }
}