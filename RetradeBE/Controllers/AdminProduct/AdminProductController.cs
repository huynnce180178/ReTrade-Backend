using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using RetradeBE.Models.DTOs;
using RetradeBE.Models.Enums;
using RetradeBE.Services;
using System.Threading.Tasks;
using System.Linq;

namespace RetradeBE.Controllers.AdminProduct
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = nameof(RoleEnum.Admin))]
    public class AdminProductController : ControllerBase
    {
        private readonly IAdminProductService _service;

        public AdminProductController(IAdminProductService service)
        {
            _service = service;
        }

        [HttpGet]
        public IActionResult GetAll(ODataQueryOptions<ProductListDto> options)
        {
            var query = _service.Query();
            
            // Apply filter only to get the total count
            var filtered = options.Filter != null ? options.Filter.ApplyTo(query, new ODataQuerySettings()) : query;
            long count = (filtered as IQueryable<ProductListDto>)?.Count() ?? 0;
            
            // Apply everything (Top, Skip, Filter, OrderBy)
            var paginated = options.ApplyTo(query);

            return Ok(new { items = paginated, totalCount = count });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var product = await _service.GetProductByIdAsync(id);
            if (product == null)
                return NotFound("Sản phẩm không tồn tại.");

            return Ok(product);
        }

        [HttpPut("{id}/approve")]
        public async Task<IActionResult> Approve(string id, [FromBody] AdminProductApprovalDto dto)
        {
            try
            {
                var success = await _service.ApproveProductAsync(id, dto);
                return Ok(new { message = dto.IsApproved ? "Phê duyệt sản phẩm thành công." : "Từ chối duyệt sản phẩm thành công." });
            }
            catch (System.Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
