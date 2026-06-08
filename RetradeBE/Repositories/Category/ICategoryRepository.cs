using RetradeBE.Models;

namespace RetradeBE.Repositories
{
    public interface ICategoryRepository
    {
        IQueryable<Category> Query();

        Task<Category?> GetByIdAsync(string categoryId);

        Task AddAsync(Category category);

        Task UpdateAsync(Category category);
    }
}