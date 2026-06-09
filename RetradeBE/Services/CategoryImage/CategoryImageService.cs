using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using RetradeBE.Models;
using RetradeBE.Repositories;

namespace RetradeBE.Services
{
    public class CategoryImageService : ICategoryImageService
    {
        private readonly ICategoryImageRepository _categoryImageRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly ICloudinaryService _cloudinaryService;

        public CategoryImageService(
            ICategoryImageRepository categoryImageRepository,
            ICategoryRepository categoryRepository,
            ICloudinaryService cloudinaryService)
        {
            _categoryImageRepository = categoryImageRepository;
            _categoryRepository = categoryRepository;
            _cloudinaryService = cloudinaryService;
        }

        public async Task<string> UploadCategoryImageAsync(string categoryId, IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("No file uploaded.");

            var category = await _categoryRepository.GetByIdAsync(categoryId);
            if (category == null)
                throw new KeyNotFoundException($"Category with ID '{categoryId}' not found.");

            // Upload image to Cloudinary under the "Categories" folder
            var url = await _cloudinaryService.UploadImageAsync(file, "Categories");
            if (string.IsNullOrEmpty(url))
                throw new Exception("Failed to upload image to Cloudinary.");

            // Check if category already has an associated image
            var existingLink = await _categoryImageRepository.GetByCategoryIdAsync(categoryId);
            if (existingLink != null)
            {
                // Delete old association
                await _categoryImageRepository.DeleteCategoryImageAsync(existingLink);

                // Fetch and delete the old image record
                var oldImage = await _categoryImageRepository.GetImageByIdAsync(existingLink.ImageId);
                if (oldImage != null)
                {
                    await _categoryImageRepository.DeleteImageAsync(oldImage);
                }
            }

            // Create new Image entity
            var newImage = new Image
            {
                ImageId = $"IMG_{Guid.NewGuid():N}",
                ImageUrl = url,
                AltText = $"Category {category.Name} Image",
                CreatedAt = DateTime.UtcNow
            };

            // Create new CategoryImage junction entity
            var newCategoryImageLink = new CategoryImage
            {
                CategoryId = categoryId,
                ImageId = newImage.ImageId,
                CreatedAt = DateTime.UtcNow
            };

            // Save to database
            await _categoryImageRepository.AddImageAsync(newImage);
            await _categoryImageRepository.AddCategoryImageAsync(newCategoryImageLink);
            await _categoryImageRepository.SaveChangesAsync();

            return url;
        }
    }
}
