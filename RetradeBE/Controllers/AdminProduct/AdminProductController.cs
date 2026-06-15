using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using RetradeBE.Models.DTOs;
using RetradeBE.Models.Enums;
using RetradeBE.Services;
using System.Threading.Tasks;

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
        [EnableQuery(PageSize = 20, MaxTop = 100)]
        public IActionResult GetAll()
        {
            return Ok(_service.Query());
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
