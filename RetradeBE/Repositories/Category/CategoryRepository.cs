using Microsoft.EntityFrameworkCore;
using RetradeBE.Data;
using RetradeBE.Models;

namespace RetradeBE.Repositories
{
    public class CategoryRepository : ICategoryRepository
    {
        private readonly AppDbContext _context;

        public CategoryRepository(AppDbContext context)
        {
            _context = context;
        }

        public IQueryable<Category> Query()
        {
            return _context.Category
                .Include(c => c.Attributes)
                .AsNoTracking();
        }

        public async Task<Category?> GetByIdAsync(string categoryId)
        {
            return await _context.Category
                .Include(c => c.Attributes)
                .FirstOrDefaultAsync(c => c.CategoryId == categoryId);
        }

        public async Task AddAsync(Category category)
        {
            await _context.Category.AddAsync(category);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Category category)
        {
            _context.Category.Update(category);
            await _context.SaveChangesAsync();
        }
    }
}