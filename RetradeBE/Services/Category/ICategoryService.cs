using RetradeBE.Models.DTOs;

namespace RetradeBE.Services
{
    public interface ICategoryService
    {
        IQueryable<CategoryResponseDto> Query();

        Task<CategoryResponseDto?> GetByIdAsync(string categoryId);

        Task<CategoryResponseDto> CreateAsync(CategoryCreateDto dto);

        Task<CategoryResponseDto> UpdateAsync(
            string categoryId,
            CategoryUpdateDto dto);

        Task InactiveAsync(string categoryId);

        Task RestoreAsync(string categoryId);
    }
}