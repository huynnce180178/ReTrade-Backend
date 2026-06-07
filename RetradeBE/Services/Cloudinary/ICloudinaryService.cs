using Microsoft.AspNetCore.Http;

namespace RetradeBE.Services
{
    public interface ICloudinaryService
    {
        Task<string> UploadImageAsync(IFormFile file, string? folder = null);
        Task<bool> DeleteImageAsync(string publicId);
    }
}
