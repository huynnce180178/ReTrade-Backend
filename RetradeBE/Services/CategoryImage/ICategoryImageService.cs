using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace RetradeBE.Services
{
    public interface ICategoryImageService
    {
        Task<string> UploadCategoryImageAsync(string categoryId, IFormFile file);
    }
}
