using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using RetradeBE.Models.DTOs;
using RetradeBE.Models.Enums;
using RetradeBE.Services;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Linq;

namespace RetradeBE.Controllers.AdminProduct
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AdminProductController : ControllerBase
    {
        private readonly IAdminProductService _service;

        public AdminProductController(IAdminProductService service)
        {
            _service = service;
        }

        [HttpGet]
        [Authorize(Roles = nameof(RoleEnum.Admin))]
        public IActionResult GetAll(ODataQueryOptions<ProductListDto> options)
        {
            var query = _service.Query();
            var settings = new ODataQuerySettings { HandleNullPropagation = HandleNullPropagationOption.False };

            try
            {
                var filtered = options.Filter != null ? options.Filter.ApplyTo(query, settings) : query;
                long count = (filtered as IQueryable<ProductListDto>)?.Count() ?? 0;

                var paginated = options.ApplyTo(query, settings);
                return Ok(new { items = paginated, totalCount = count });
            }
            catch (System.Exception)
            {
                var list = query.ToList();
                if (options.Filter != null)
                {
                    list = options.Filter.ApplyTo(list.AsQueryable(), settings).Cast<ProductListDto>().ToList();
                }
                long count = list.Count;

                if (options.OrderBy != null)
                {
                    list = options.OrderBy.ApplyTo(list.AsQueryable(), settings).Cast<ProductListDto>().ToList();
                }
                else
                {
                    list = list.OrderByDescending(p => p.UpdatedAt ?? p.CreatedAt).ToList();
                }

                if (options.Skip != null)
                {
                    list = options.Skip.ApplyTo(list.AsQueryable(), settings).Cast<ProductListDto>().ToList();
                }
                if (options.Top != null)
                {
                    list = options.Top.ApplyTo(list.AsQueryable(), settings).Cast<ProductListDto>().ToList();
                }

                return Ok(new { items = list, totalCount = count });
            }
        }

        [HttpGet("{id}")]
        [Authorize(Roles = nameof(RoleEnum.Admin))]
        public async Task<IActionResult> GetById(string id)
        {
            var product = await _service.GetProductByIdAsync(id);
            if (product == null)
                return NotFound("Sản phẩm không tồn tại.");

            return Ok(product);
        }

        [HttpPut("{id}/approve")]
        [Authorize(Roles = nameof(RoleEnum.Admin))]
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

        [HttpPut("{id}/remove")]
        [Authorize(Roles = nameof(RoleEnum.Admin))]
        public async Task<IActionResult> Remove(string id, [FromBody] AdminProductApprovalDto dto)
        {
            try
            {
                var reason = dto?.RejectReason;
                var success = await _service.RemoveProductAsync(id, reason ?? "Gỡ sản phẩm do vi phạm quy định sàn.");
                return Ok(new { message = "Gỡ sản phẩm thành công." });
            }
            catch (System.Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("{id}/reactivate")]
        [Authorize(Roles = nameof(RoleEnum.Admin))]
        public async Task<IActionResult> Reactivate(string id)
        {
            try
            {
                var success = await _service.ReactivateProductAsync(id);
                return Ok(new { message = "Khôi phục sản phẩm thành công." });
            }
            catch (System.Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("{id}/appeal")]
        [Authorize]
        public async Task<IActionResult> Appeal(string id, [FromBody] ProductAppealDto dto)
        {
            try
            {
                var accountId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                             ?? User.FindFirstValue("AccountId")
                             ?? User.FindFirstValue("UserId")
                             ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(accountId))
                    return Unauthorized("Token không hợp lệ.");

                var success = await _service.AppealProductAsync(id, accountId, dto?.Reason ?? "");
                return Ok(new { message = "Gửi kháng cáo thành công. Ban quản trị sẽ xem xét yêu cầu của bạn." });
            }
            catch (System.Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
