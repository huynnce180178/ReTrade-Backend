using RetradeBE.Models;

namespace RetradeBE.Repositories
{
    public interface ICategoryRepository
    {
        /// <summary>
        /// Lấy tất cả Category (OData sẽ filter theo Status)
        /// </summary>
        Task<IEnumerable<Category>> GetAllAsync();

        /// <summary>
        /// Lấy Category theo ID (kèm Attributes)
        /// </summary>
        Task<Category?> GetByIdAsync(string categoryId);

        /// <summary>
        /// Lấy Category theo tên
        /// </summary>
        Task<Category?> GetByNameAsync(string name);

        /// <summary>
        /// Thêm Category mới (Aggregate Root)
        /// </summary>
        Task AddAsync(Category category);

        /// <summary>
        /// Cập nhật Category (Aggregate Root)
        /// </summary>
        Task UpdateAsync(Category category);

        /// <summary>
        /// Soft delete Category (chỉ thay đổi Status)
        /// </summary>
        Task InactiveAsync(string categoryId);

        /// <summary>
        /// Khôi phục Category (Restore)
        /// </summary>
        Task RestoreAsync(string categoryId);

        /// <summary>
        /// Kiểm tra Category có tồn tại không
        /// </summary>
        Task<bool> ExistsAsync(string categoryId);

        /// <summary>
        /// Lấy CategoryId tiếp theo theo format CAT001, CAT002, ...
        /// </summary>
        Task<string> GetNextCategoryIdAsync();
    }
}
