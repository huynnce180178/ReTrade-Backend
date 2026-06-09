using System.Threading.Tasks;
using RetradeBE.Models;

namespace RetradeBE.Repositories
{
    public interface ICategoryImageRepository
    {
        Task AddImageAsync(Image image);
        Task AddCategoryImageAsync(CategoryImage categoryImage);
        Task<CategoryImage?> GetByCategoryIdAsync(string categoryId);
        Task DeleteCategoryImageAsync(CategoryImage categoryImage);
        Task DeleteImageAsync(Image image);
        Task<Image?> GetImageByIdAsync(string imageId);
        Task SaveChangesAsync();
    }
}
