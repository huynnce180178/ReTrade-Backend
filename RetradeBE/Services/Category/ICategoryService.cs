using RetradeBE.Models.DTOs;

namespace RetradeBE.Services
{
    public interface ICategoryService
    {
        //Lấy tất cả các category Active
        Task<IEnumerable<CategoryResponseDto>> GetAllAsync();

        //Lấy Category theo ID
        Task<CategoryResponseDto?> GetByIdAsync(string categoryId);

        // Tạo Category mới + Attributes
        Task<CategoryResponseDto> CreateAsync(CategoryCreateDto dto);

        /// Cập nhật Category + Attributes
        Task<CategoryResponseDto> UpdateAsync(string categoryId, CategoryUpdateDto dto);

        // Soft delete (Inactive) Category
        Task InactiveAsync(string categoryId);

        // Khôi phục Category
        Task RestoreAsync(string categoryId);
    }
}
