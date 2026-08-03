using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using RetradeBE.Config;

namespace RetradeBE.Services
{
    public class CloudinaryService : ICloudinaryService
    {
        private readonly Cloudinary _cloudinary;

        public CloudinaryService(IOptions<CloudinarySettings> config)
        {
            var cloudName = string.IsNullOrWhiteSpace(config?.Value?.CloudName) ? "retrade_dummy" : config.Value.CloudName;
            var apiKey = string.IsNullOrWhiteSpace(config?.Value?.ApiKey) ? "1234567890" : config.Value.ApiKey;
            var apiSecret = string.IsNullOrWhiteSpace(config?.Value?.ApiSecret) ? "dummy_secret" : config.Value.ApiSecret;
            var acc = new Account(cloudName, apiKey, apiSecret);
            _cloudinary = new Cloudinary(acc);
        }

        public async Task<string> UploadImageAsync(IFormFile file, string? folder = null)
        {
            var uploadResult = new ImageUploadResult();

            if (file.Length > 0)
            {
                using var stream = file.OpenReadStream();
                var uploadParams = new ImageUploadParams
                {
                    File = new FileDescription(file.FileName, stream),
                    Folder = folder ?? "ReTrade"
                };

                uploadResult = await _cloudinary.UploadAsync(uploadParams);
            }

            return uploadResult.SecureUrl?.ToString() ?? string.Empty;
        }

        public async Task<bool> DeleteImageAsync(string publicId)
        {
            var deleteParams = new DeletionParams(publicId);
            var result = await _cloudinary.DestroyAsync(deleteParams);
            return result.Result == "ok";
        }
    }
}
