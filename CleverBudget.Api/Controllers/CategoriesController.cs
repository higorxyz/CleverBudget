using CleverBudget.Core.DTOs;
using CleverBudget.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CleverBudget.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
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
    /// Listar todas as categorias do usuário
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var userId = GetUserId();
        var categories = await _categoryService.GetAllAsync(userId);
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