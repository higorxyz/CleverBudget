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
    /// Listar todas as transações do usuário
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
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