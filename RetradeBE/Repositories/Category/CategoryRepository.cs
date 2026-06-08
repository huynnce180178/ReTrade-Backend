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

        public async Task<IEnumerable<Category>> GetAllAsync()
        {
            return await _context.Category
                .Include(c => c.Attributes)
                .ToListAsync();
        }

        public async Task<Category?> GetByIdAsync(string categoryId)
        {
            return await _context.Category
                .Include(c => c.Attributes)
                .FirstOrDefaultAsync(c => c.CategoryId == categoryId);
        }

        public async Task<Category?> GetByNameAsync(string name)
        {
            return await _context.Category
                .Include(c => c.Attributes)
                .FirstOrDefaultAsync(c => c.Name == name);
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

        public async Task InactiveAsync(string categoryId)
        {
            var category = await _context.Category.FindAsync(categoryId);
            if (category != null)
            {
                category.Status = "Inactive";
                category.UpdatedAt = DateTime.UtcNow;
                _context.Category.Update(category);
                await _context.SaveChangesAsync();
            }
        }

        public async Task RestoreAsync(string categoryId)
        {
            var category = await _context.Category.FindAsync(categoryId);
            if (category != null)
            {
                category.Status = "Active";
                category.UpdatedAt = DateTime.UtcNow;
                _context.Category.Update(category);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> ExistsAsync(string categoryId)
        {
            return await _context.Category
                .AnyAsync(c => c.CategoryId == categoryId);
        }

        public async Task<string> GetNextCategoryIdAsync()
        {
            var lastCategory = await _context.Category
                .OrderByDescending(c => c.CategoryId)
                .FirstOrDefaultAsync();

            int nextNumber = 1;
            if (lastCategory != null && lastCategory.CategoryId.StartsWith("CAT"))
            {
                if (int.TryParse(lastCategory.CategoryId.Substring(3), out int lastNumber))
                {
                    nextNumber = lastNumber + 1;
                }
            }

            return $"CAT{nextNumber:D3}";
        }
    }
}
