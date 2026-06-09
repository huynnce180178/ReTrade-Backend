using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RetradeBE.Services;

namespace RetradeBE.Controllers
{
    public class UploadCategoryImageDto
    {
        public string CategoryId { get; set; } = null!;
        public IFormFile Image { get; set; } = null!;
    }

    [ApiController]
    [Route("api/[controller]")]
    public class CategoryImageController : ControllerBase
    {
        private readonly ICategoryImageService _categoryImageService;

        public CategoryImageController(ICategoryImageService categoryImageService)
        {
            _categoryImageService = categoryImageService;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadImage([FromForm] UploadCategoryImageDto dto)
        {
            if (dto == null)
                return BadRequest("Invalid request data.");

            if (string.IsNullOrWhiteSpace(dto.CategoryId))
                return BadRequest("CategoryId is required.");

            if (dto.Image == null || dto.Image.Length == 0)
                return BadRequest("An image file is required.");

            try
            {
                var imageUrl = await _categoryImageService.UploadCategoryImageAsync(dto.CategoryId, dto.Image);
                return Ok(new { imageUrl });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}
