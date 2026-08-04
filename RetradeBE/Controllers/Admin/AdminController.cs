using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.EntityFrameworkCore;
using RetradeBE.Data;
using RetradeBE.Models;
using RetradeBE.Models.Enums;
using RetradeBE.Models.DTOs;
using RetradeBE.Models.DTOs.Admin;
using RetradeBE.Services;
using RetradeBE.Services.Refund;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace RetradeBE.Controllers.Admin
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = nameof(RoleEnum.Admin))]
    public class AdminController : ControllerBase
    {
        private readonly IAccountService _accountService;
        private readonly IRefundService _refundService;
        private readonly IServiceSubscriptionService _subscriptionService;

        public AdminController(
            IAccountService accountService,
            IRefundService refundService,
            IServiceSubscriptionService subscriptionService)
        {
            _accountService = accountService;
            _refundService = refundService;
            _subscriptionService = subscriptionService;
        }

        [HttpGet("refunds")]
        public async Task<IActionResult> GetRefunds()
        {
            var refunds = await _refundService.GetAllRefundsAsync();
            return Ok(refunds);
        }

        [HttpPost("refunds/{id}/done")]
        public async Task<IActionResult> MarkRefundDone(string id)
        {
            var result = await _refundService.ApproveRefundAsync(id);
            if (!result.Success)
            {
                if (result.Message.Contains("not found"))
                    return NotFound(result.Message);
                return BadRequest(result.Message);
            }

            return Ok(new { message = result.Message });
        }

        [HttpPost("refunds/{id}/reject")]
        public async Task<IActionResult> RejectRefund(string id, [FromBody] RejectRefundRequestDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Reason))
            {
                return BadRequest("Reject reason is required.");
            }

            var result = await _refundService.RejectRefundAsync(id, dto);
            if (!result.Success)
            {
                if (result.Message.Contains("not found"))
                    return NotFound(result.Message);
                return BadRequest(result.Message);
            }

            return Ok(new { message = result.Message });
        }

        [HttpGet("user-list")]
        [EnableQuery(PageSize = 20, MaxTop = 100)]
        public IActionResult UserList()
        {
            return Ok(_accountService.QueryUserList());
        }

        [HttpPatch("users/{id}/ban")]
        public async Task<IActionResult> BanUser(string id, [FromBody] BanUserRequestDto? dto)
        {
            var account = await _accountService.GetByIdAsync(id);
            if (account == null)
            {
                return NotFound("Account not found.");
            }

            var wasInactive = account.Status == AccountStatusEnum.Inactive.ToString() || account.Status == AccountStatusEnum.Ban.ToString();
            var banned = await _accountService.BanUserAsync(id, dto?.Reason);
            if (!banned)
            {
                return NotFound("Account not found.");
            }

            var message = wasInactive
                ? "User activated successfully."
                : "User banned successfully.";

            return Ok(new { message });
        }

        [HttpPost("users/{id}/grant-seller-unlimited")]
        public async Task<IActionResult> GrantSellerUnlimited(string id)
        {
            var result = await _subscriptionService.GrantAdminSellerSubscriptionAsync(id);
            if (!result)
            {
                return BadRequest("Failed to grant seller subscription.");
            }

            return Ok(new { message = "Seller subscription granted successfully with unlimited duration by Admin." });
        }

        [HttpPost("users/{id}/revoke-seller")]
        public async Task<IActionResult> RevokeSeller(string id)
        {
            var result = await _subscriptionService.RevokeSellerSubscriptionAsync(id);
            if (!result)
            {
                return BadRequest("Failed to revoke seller subscription.");
            }

            return Ok(new { message = "Seller subscription and privileges revoked successfully." });
        }
    }
}