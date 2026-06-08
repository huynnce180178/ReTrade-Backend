using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using RetradeBE.Models.DTOs;
using RetradeBE.Services;

namespace RetradeBE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CategoryController : ControllerBase
    {
        private readonly ICategoryService _service;

        public CategoryController(ICategoryService service)
        {
            _service = service;
        }

        [HttpGet]
        [EnableQuery(
            PageSize = 20,
            MaxTop = 100)]
        public IActionResult GetAll()
        {
            return Ok(_service.Query());
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var category =
                await _service.GetByIdAsync(id);

            if (category == null)
                return NotFound();

            return Ok(category);
        }

        [HttpPost]
        public async Task<IActionResult> Create(
            CategoryCreateDto dto)
        {
            var result =
                await _service.CreateAsync(dto);

            return CreatedAtAction(
                nameof(GetById),
                new { id = result.CategoryId },
                result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(
            string id,
            CategoryUpdateDto dto)
        {
            return Ok(
                await _service.UpdateAsync(id, dto));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Inactive(
            string id)
        {
            await _service.InactiveAsync(id);

            return NoContent();
        }

        [HttpPatch("{id}/restore")]
        public async Task<IActionResult> Restore(
            string id)
        {
            await _service.RestoreAsync(id);

            return NoContent();
        }
    }
}