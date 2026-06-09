using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RetradeBE.Data;
using RetradeBE.Models;

namespace RetradeBE.Repositories
{
    public class CategoryImageRepository : ICategoryImageRepository
    {
        private readonly AppDbContext _context;

        public CategoryImageRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddImageAsync(Image image)
        {
            await _context.Image.AddAsync(image);
        }

        public async Task AddCategoryImageAsync(CategoryImage categoryImage)
        {
            await _context.CategoryImage.AddAsync(categoryImage);
        }

        public async Task<CategoryImage?> GetByCategoryIdAsync(string categoryId)
        {
            return await _context.CategoryImage
                .Include(ci => ci.Image)
                .FirstOrDefaultAsync(ci => ci.CategoryId == categoryId);
        }

        public async Task DeleteCategoryImageAsync(CategoryImage categoryImage)
        {
            _context.CategoryImage.Remove(categoryImage);
            await Task.CompletedTask;
        }

        public async Task DeleteImageAsync(Image image)
        {
            _context.Image.Remove(image);
            await Task.CompletedTask;
        }

        public async Task<Image?> GetImageByIdAsync(string imageId)
        {
            return await _context.Image.FirstOrDefaultAsync(i => i.ImageId == imageId);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
