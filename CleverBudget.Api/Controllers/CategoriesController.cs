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
public class CategoriesController : ControllerBase
{
    private readonly ICategoryService _categoryService;

    public CategoriesController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    private string GetUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    }

    /// <summary>
    /// Listar categorias com paginação
    /// </summary>
    /// <param name="page">Número da página (padrão: 1)</param>
    /// <param name="pageSize">Itens por página (padrão: 20, máximo: 100)</param>
    /// <param name="sortBy">Campo para ordenação: name, createdAt, isDefault</param>
    /// <param name="sortOrder">Ordem: asc ou desc (padrão: asc)</param>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<CategoryResponseDto>), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? sortBy = "name",
        [FromQuery] string? sortOrder = "asc")
    {
        var userId = GetUserId();
        
        var paginationParams = new PaginationParams
        {
            Page = page,
            PageSize = pageSize,
            SortBy = sortBy,
            SortOrder = sortOrder
        };

        var result = await _categoryService.GetPagedAsync(userId, paginationParams);
        var etag = EtagGenerator.Create(result);
        if (this.RequestHasMatchingEtag(etag))
        {
            return this.CachedStatus();
        }

        this.SetEtagHeader(etag);
        return Ok(result);
    }

    /// <summary>
    /// Listar todas as categorias (sem paginação) - use apenas para dropdowns
    /// </summary>
    [HttpGet("all")]
    public async Task<IActionResult> GetAllWithoutPagination()
    {
        var userId = GetUserId();
        var categories = await _categoryService.GetAllAsync(userId);
        var etag = EtagGenerator.Create(categories);

        if (this.RequestHasMatchingEtag(etag))
        {
            return this.CachedStatus();
        }

        this.SetEtagHeader(etag);
        return Ok(categories);
    }

    /// <summary>
    /// Obter categoria por ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var userId = GetUserId();
        var category = await _categoryService.GetByIdAsync(id, userId);

        if (category == null)
            return NotFound(new { message = "Categoria não encontrada." });

        var etag = EtagGenerator.Create(category);

        if (this.RequestHasMatchingEtag(etag))
        {
            return this.CachedStatus();
        }

        this.SetEtagHeader(etag);
        return Ok(category);
    }

    /// <summary>
    /// Criar nova categoria personalizada
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCategoryDto dto)
    {
        var userId = GetUserId();
        var category = await _categoryService.CreateAsync(dto, userId);

        if (category == null)
            return BadRequest(new { message = "Já existe uma categoria com esse nome." });

        return CreatedAtAction(nameof(GetById), new { id = category.Id }, category);
    }

    /// <summary>
    /// Atualizar categoria (apenas categorias customizadas)
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCategoryDto dto)
    {
        var userId = GetUserId();
        var category = await _categoryService.UpdateAsync(id, dto, userId);

        if (category == null)
            return BadRequest(new { message = "Categoria não encontrada, é uma categoria padrão, ou o nome já existe." });

        return Ok(category);
    }

    /// <summary>
    /// Deletar categoria (apenas customizadas e sem transações)
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = GetUserId();
        var success = await _categoryService.DeleteAsync(id, userId);

        if (!success)
            return BadRequest(new { message = "Não é possível deletar: categoria padrão, não encontrada ou possui transações associadas." });

        return NoContent();
    }
}