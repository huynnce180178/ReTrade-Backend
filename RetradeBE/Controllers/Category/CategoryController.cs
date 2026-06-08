using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using RetradeBE.Models.DTOs;
using RetradeBE.Services;

namespace RetradeBE.Controllers.Category
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoryController : ControllerBase
    {
        private readonly ICategoryService _service;

        public CategoryController(ICategoryService service)
        {
            _service = service;
        }

        /// <summary>
        /// Lấy danh sách tất cả Category (kèm cả Active và Inactive)
        /// Hỗ trợ OData: $filter, $orderby, $search, $skip, $top
        /// Ví dụ:
        /// GET /api/category?$filter=status eq 'Active'&$orderby=name asc
        /// GET /api/category?$filter=status eq 'Inactive'
        /// GET /api/category?$search='Điện thoại'&$orderby=name asc&$skip=0&$top=10
        [HttpGet]
        [EnableQuery]
        public async Task<IActionResult> GetAll()
        {
            var categories = await _service.GetAllAsync();
            return Ok(categories.AsQueryable());
        }

        /// Lấy chi tiết Category theo ID (kèm Attributes)
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            try
            {
                var category = await _service.GetByIdAsync(id);
                if (category == null)
                    return NotFound(new { message = $"Category '{id}' không tồn tại" });

                return Ok(category);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }


        /// Tạo Category mới + Attributes
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CategoryCreateDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var category = await _service.CreateAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = category.CategoryId }, category);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        /// Cập nhật Category + Attributes
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] CategoryUpdateDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var category = await _service.UpdateAsync(id, dto);
                return Ok(category);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// Soft delete (Inactive) Category
        [HttpDelete("{id}")]
        public async Task<IActionResult> Inactive(string id)
        {
            try
            {
                await _service.InactiveAsync(id);
                return Ok(new { message = $"Category '{id}' đã được vô hiệu hóa" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Khôi phục Category
        /// </summary>
        [HttpPatch("{id}/restore")]
        public async Task<IActionResult> Restore(string id)
        {
            try
            {
                await _service.RestoreAsync(id);
                return Ok(new { message = $"Category '{id}' đã được khôi phục" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
