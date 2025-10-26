using CleverBudget.Core.Common;
using CleverBudget.Core.DTOs;
using CleverBudget.Core.Enums;
using CleverBudget.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CleverBudget.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TransactionsController : ControllerBase
{
    private readonly ITransactionService _transactionService;

    public TransactionsController(ITransactionService transactionService)
    {
        _transactionService = transactionService;
    }

    private string GetUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    }

    /// <summary>
    /// Listar todas as transações do usuário (SEM paginação - deprecated)
    /// </summary>
    [HttpGet("all")]
    [Obsolete("Use GET /api/transactions com paginação")]
    public async Task<IActionResult> GetAllWithoutPagination(
        [FromQuery] TransactionType? type = null,
        [FromQuery] int? categoryId = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var userId = GetUserId();
        var transactions = await _transactionService.GetAllAsync(userId, type, categoryId, startDate, endDate);
        return Ok(transactions);
    }

    /// <summary>
    /// Listar transações com paginação
    /// </summary>
    /// <param name="page">Número da página (padrão: 1)</param>
    /// <param name="pageSize">Itens por página (padrão: 10, máximo: 100)</param>
    /// <param name="sortBy">Campo para ordenação: date, amount, description, category</param>
    /// <param name="sortOrder">Ordem: asc ou desc (padrão: desc)</param>
    /// <param name="type">Filtrar por tipo: Income ou Expense</param>
    /// <param name="categoryId">Filtrar por ID da categoria</param>
    /// <param name="startDate">Data inicial do filtro</param>
    /// <param name="endDate">Data final do filtro</param>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<TransactionResponseDto>), 200)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortOrder = "desc",
        [FromQuery] TransactionType? type = null,
        [FromQuery] int? categoryId = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var userId = GetUserId();
        
        var paginationParams = new PaginationParams
        {
            Page = page,
            PageSize = pageSize,
            SortBy = sortBy,
            SortOrder = sortOrder
        };

        var result = await _transactionService.GetPagedAsync(
            userId, 
            paginationParams, 
            type, 
            categoryId, 
            startDate, 
            endDate
        );

        return Ok(result);
    }

    /// <summary>
    /// Obter transação por ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var userId = GetUserId();
        var transaction = await _transactionService.GetByIdAsync(id, userId);

        if (transaction == null)
            return NotFound(new { message = "Transação não encontrada." });

        return Ok(transaction);
    }

    /// <summary>
    /// Criar nova transação
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTransactionDto dto)
    {
        var userId = GetUserId();
        var transaction = await _transactionService.CreateAsync(dto, userId);

        if (transaction == null)
            return BadRequest(new { message = "Categoria inválida ou não pertence ao usuário." });

        return CreatedAtAction(nameof(GetById), new { id = transaction.Id }, transaction);
    }

    /// <summary>
    /// Atualizar transação
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateTransactionDto dto)
    {
        var userId = GetUserId();
        var transaction = await _transactionService.UpdateAsync(id, dto, userId);

        if (transaction == null)
            return NotFound(new { message = "Transação não encontrada ou categoria inválida." });

        return Ok(transaction);
    }

    /// <summary>
    /// Deletar transação
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = GetUserId();
        var success = await _transactionService.DeleteAsync(id, userId);

        if (!success)
            return NotFound(new { message = "Transação não encontrada." });

        return NoContent();
    }
}