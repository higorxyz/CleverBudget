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
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class RecurringTransactionsController : ControllerBase
{
    private readonly IRecurringTransactionService _recurringTransactionService;

    public RecurringTransactionsController(IRecurringTransactionService recurringTransactionService)
    {
        _recurringTransactionService = recurringTransactionService;
    }

    private string GetUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    }

    /// <summary>
    /// Listar transações recorrentes com paginação
    /// </summary>
    /// <param name="page">Número da página (padrão: 1)</param>
    /// <param name="pageSize">Itens por página (padrão: 10, máximo: 100)</param>
    /// <param name="sortBy">Campo para ordenação: amount, description, frequency, startdate</param>
    /// <param name="sortOrder">Ordem: asc ou desc (padrão: desc)</param>
    /// <param name="isActive">Filtrar por status ativo (true/false/null=todos)</param>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<RecurringTransactionResponseDto>), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortOrder = "desc",
        [FromQuery] bool? isActive = null)
    {
        var userId = GetUserId();

        var paginationParams = new PaginationParams
        {
            Page = page,
            PageSize = pageSize,
            SortBy = sortBy,
            SortOrder = sortOrder
        };

        var result = await _recurringTransactionService.GetPagedAsync(userId, paginationParams, isActive);
        var etag = EtagGenerator.Create(result);

        if (this.RequestHasMatchingEtag(etag))
        {
            return this.CachedStatus();
        }

        this.SetEtagHeader(etag);
        return Ok(result);
    }

    /// <summary>
    /// Listar todas as transações recorrentes (sem paginação)
    /// </summary>
    [HttpGet("all")]
    public async Task<IActionResult> GetAllWithoutPagination([FromQuery] bool? isActive = null)
    {
        var userId = GetUserId();
        var recurringTransactions = await _recurringTransactionService.GetAllAsync(userId, isActive);
        var etag = EtagGenerator.Create(recurringTransactions);

        if (this.RequestHasMatchingEtag(etag))
        {
            return this.CachedStatus();
        }

        this.SetEtagHeader(etag);
        return Ok(recurringTransactions);
    }

    /// <summary>
    /// Obter transação recorrente por ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var userId = GetUserId();
        var recurringTransaction = await _recurringTransactionService.GetByIdAsync(id, userId);

        if (recurringTransaction == null)
            return NotFound(new { message = "Transação recorrente não encontrada." });

        var etag = EtagGenerator.Create(recurringTransaction);

        if (this.RequestHasMatchingEtag(etag))
        {
            return this.CachedStatus();
        }

        this.SetEtagHeader(etag);
        return Ok(recurringTransaction);
    }

    /// <summary>
    /// Criar nova transação recorrente
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRecurringTransactionDto dto)
    {
        var userId = GetUserId();
        var recurringTransaction = await _recurringTransactionService.CreateAsync(dto, userId);

        if (recurringTransaction == null)
            return BadRequest(new { message = "Categoria inválida ou dados incompletos para a frequência selecionada." });

        return CreatedAtAction(nameof(GetById), new { id = recurringTransaction.Id }, recurringTransaction);
    }

    /// <summary>
    /// Atualizar transação recorrente
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateRecurringTransactionDto dto)
    {
        var userId = GetUserId();
        var recurringTransaction = await _recurringTransactionService.UpdateAsync(id, dto, userId);

        if (recurringTransaction == null)
            return NotFound(new { message = "Transação recorrente não encontrada." });

        var etag = EtagGenerator.Create(recurringTransaction);

        if (this.RequestHasMatchingEtag(etag))
        {
            return this.CachedStatus();
        }

        this.SetEtagHeader(etag);
        return Ok(recurringTransaction);
    }

    /// <summary>
    /// Deletar transação recorrente
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = GetUserId();
        var success = await _recurringTransactionService.DeleteAsync(id, userId);

        if (!success)
            return NotFound(new { message = "Transação recorrente não encontrada." });

        return NoContent();
    }
}