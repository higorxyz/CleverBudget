using Asp.Versioning;
using CleverBudget.Api.Extensions;
using CleverBudget.Core.Common;
using CleverBudget.Core.DTOs;
using CleverBudget.Core.Enums;
using CleverBudget.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;

namespace CleverBudget.Api.Controllers;

[ApiVersion("2.0")]
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
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
    /// Listar transações com paginação
    /// </summary>
    /// <param name="page">Número da página (padrão: 1)</param>
    /// <param name="pageSize">Itens por página (padrão: 10, máximo: 100)</param>
    /// <param name="sortBy">Campo para ordenação: date, amount, description, category</param>
    /// <param name="sortOrder">Ordem: asc ou desc (padrão: desc)</param>
    /// <param name="type">Filtrar por tipo: 1=Income ou 2=Expense</param>
    /// <param name="categoryId">Filtrar por ID da categoria</param>
    /// <param name="startDate">Data inicial do filtro (formato: yyyy-MM-dd)</param>
    /// <param name="endDate">Data final do filtro (formato: yyyy-MM-dd)</param>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<TransactionResponseDto>), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? sortBy = "date",
        [FromQuery] string? sortOrder = "desc",
        [FromQuery] TransactionType? type = null,
        [FromQuery] int? categoryId = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery(Name = "q")] string? search = null,
        [FromQuery] decimal? minAmount = null,
        [FromQuery] decimal? maxAmount = null,
        [FromQuery] string? include = null)
    {
        var userId = GetUserId();
        
        var paginationParams = new PaginationParams
        {
            Page = page,
            PageSize = pageSize,
            SortBy = sortBy,
            SortOrder = sortOrder
        };

        var includes = (include ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(i => i.ToLowerInvariant())
            .ToHashSet();

        var includeCategory = includes.Contains("category");

        var result = await _transactionService.GetPagedAsync(
            userId, 
            paginationParams, 
            type, 
            categoryId, 
            startDate, 
            endDate,
            search,
            minAmount,
            maxAmount,
            includeCategory
        );

        var etag = EtagGenerator.Create(result);
        if (this.RequestHasMatchingEtag(etag))
        {
            return this.CachedStatus();
        }

        this.SetEtagHeader(etag);
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

        var etag = EtagGenerator.Create(transaction);
        if (this.RequestHasMatchingEtag(etag))
        {
            return this.CachedStatus();
        }

        this.SetEtagHeader(etag);
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

    /// <summary>
    /// Importar transações em massa via CSV
    /// </summary>
    [HttpPost("import")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(TransactionImportResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ImportCsv([FromForm] TransactionImportForm form)
    {
        if (form.File == null || form.File.Length == 0)
        {
            return BadRequest(new { message = "Arquivo CSV é obrigatório." });
        }

        await using var stream = form.File.OpenReadStream();

        var options = new TransactionImportOptions
        {
            HasHeader = form.HasHeader,
            Delimiter = form.Delimiter,
            UpsertExisting = form.UpsertExisting,
            CategoryFallbackKind = form.CategoryFallbackKind
        };

        var userId = GetUserId();
        var result = await _transactionService.ImportFromCsvAsync(userId, stream, options);
        return Ok(result);
    }

    public sealed class TransactionImportForm
    {
        [Required]
        public IFormFile? File { get; init; }

        public bool HasHeader { get; init; } = true;

        public string Delimiter { get; init; } = ",";

        public bool UpsertExisting { get; init; }

        public string CategoryFallbackKind { get; init; } = "Essential";
    }
}