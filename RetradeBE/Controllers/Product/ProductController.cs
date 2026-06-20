using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetradeBE.Models.DTOs;
using RetradeBE.Services;
using System.Security.Claims;
using System.Threading.Tasks;

namespace RetradeBE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductController : ControllerBase
    {
        private readonly IProductService _service;
        private readonly ICloudinaryService _cloudinaryService;

        public ProductController(IProductService service, ICloudinaryService cloudinaryService)
        {
            _service = service;
            _cloudinaryService = cloudinaryService;
        }

        [Authorize]
        [HttpPost("upload-image")]
        public async Task<IActionResult> UploadImage(Microsoft.AspNetCore.Http.IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            try
            {
                var url = await _cloudinaryService.UploadImageAsync(file, "Product/images");
                if (string.IsNullOrEmpty(url))
                    return StatusCode(500, "Failed to upload image.");

                return Ok(new { imageUrl = url });
            }
            catch (System.Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] ProductSearchQueryDto query)
        {
            var result = await _service.GetProductsAsync(query);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var product = await _service.GetProductByIdAsync(id);
            if (product == null)
                return NotFound("Sản phẩm không tồn tại.");

            return Ok(product);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ProductCreateDto dto)
        {
            var accountId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(accountId))
                return Unauthorized();

            try
            {
                var result = await _service.CreateProductAsync(accountId, dto);
                return CreatedAtAction(nameof(GetById), new { id = result.ProductId }, result);
            }
            catch (System.Exception ex)
            {
                var details = ex.InnerException != null ? $"{ex.Message} -> Inner: {ex.InnerException.Message}" : ex.Message;
                return BadRequest(details);
            }
        }

        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] ProductUpdateDto dto)
        {
            var accountId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(accountId))
                return Unauthorized();

            try
            {
                var result = await _service.UpdateProductAsync(id, accountId, dto);
                return Ok(result);
            }
            catch (System.Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var accountId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(accountId))
                return Unauthorized();

            try
            {
                await _service.DeleteProductAsync(id, accountId);
                return NoContent();
            }
            catch (System.Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
