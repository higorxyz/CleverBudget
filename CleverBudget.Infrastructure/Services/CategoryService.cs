using CleverBudget.Core.DTOs;
using CleverBudget.Core.Entities;
using CleverBudget.Core.Interfaces;
using CleverBudget.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CleverBudget.Infrastructure.Services;

public class CategoryService : ICategoryService
{
    private readonly AppDbContext _context;

    public CategoryService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<CategoryResponseDto>> GetAllAsync(string userId)
    {
        var categories = await _context.Categories
            .Where(c => c.UserId == userId)
            .OrderBy(c => c.Name)
            .Select(c => new CategoryResponseDto
            {
                Id = c.Id,
                Name = c.Name,
                Icon = c.Icon,
                Color = c.Color,
                IsDefault = c.IsDefault,
                CreatedAt = c.CreatedAt
            })
            .ToListAsync();

        return categories;
    }

    public async Task<CategoryResponseDto?> GetByIdAsync(int id, string userId)
    {
        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

        if (category == null)
            return null;

        return new CategoryResponseDto
        {
            Id = category.Id,
            Name = category.Name,
            Icon = category.Icon,
            Color = category.Color,
            IsDefault = category.IsDefault,
            CreatedAt = category.CreatedAt
        };
    }

    public async Task<CategoryResponseDto?> CreateAsync(CreateCategoryDto dto, string userId)
    {
        // Verificar se já existe categoria com esse nome para o usuário
        var exists = await _context.Categories
            .AnyAsync(c => c.UserId == userId && c.Name.ToLower() == dto.Name.ToLower());

        if (exists)
            return null;

        var category = new Category
        {
            UserId = userId,
            Name = dto.Name,
            Icon = dto.Icon,
            Color = dto.Color,
            IsDefault = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        return await GetByIdAsync(category.Id, userId);
    }

    public async Task<CategoryResponseDto?> UpdateAsync(int id, UpdateCategoryDto dto, string userId)
    {
        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

        if (category == null)
            return null;

        // Não permitir editar categorias padrão
        if (category.IsDefault)
            return null;

        if (!string.IsNullOrEmpty(dto.Name))
        {
            // Verificar se novo nome já existe
            var nameExists = await _context.Categories
                .AnyAsync(c => c.UserId == userId && c.Id != id && c.Name.ToLower() == dto.Name.ToLower());

            if (nameExists)
                return null;

            category.Name = dto.Name;
        }

        if (dto.Icon != null)
            category.Icon = dto.Icon;

        if (dto.Color != null)
            category.Color = dto.Color;

        await _context.SaveChangesAsync();

        return await GetByIdAsync(category.Id, userId);
    }

    public async Task<bool> DeleteAsync(int id, string userId)
    {
        var category = await _context.Categories
            .Include(c => c.Transactions)
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

        if (category == null)
            return false;

        // Não permitir deletar categorias padrão
        if (category.IsDefault)
            return false;

        // Não permitir deletar se houver transações associadas
        if (category.Transactions.Any())
            return false;

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();

        return true;
    }
}