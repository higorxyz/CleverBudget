using CleverBudget.Core.Common;
using CleverBudget.Core.DTOs;

namespace CleverBudget.Core.Interfaces;

public interface ICategoryService
{
    Task<IEnumerable<CategoryResponseDto>> GetAllAsync(string userId);
    Task<PagedResult<CategoryResponseDto>> GetPagedAsync(string userId, PaginationParams paginationParams);
    Task<CategoryResponseDto?> GetByIdAsync(int id, string userId);
    Task<CategoryResponseDto?> CreateAsync(CreateCategoryDto dto, string userId);
    Task<CategoryResponseDto?> UpdateAsync(int id, UpdateCategoryDto dto, string userId);
    Task<bool> DeleteAsync(int id, string userId);
}