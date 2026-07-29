using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.EntityFrameworkCore;
using RetradeBE.Data;
using RetradeBE.Models;
using RetradeBE.Models.Enums;
using RetradeBE.Models.DTOs;
using RetradeBE.Services;
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
        private readonly AppDbContext _context;
        private readonly INotificationService _notificationService;

        public AdminController(IAccountService accountService, AppDbContext context, INotificationService notificationService)
        {
            _accountService = accountService;
            _context = context;
            _notificationService = notificationService;
        }

        [HttpGet("refunds")]
        public async Task<IActionResult> GetRefunds()
        {
            var refunds = await _context.RefundRequest
                .AsNoTracking()
                .Include(r => r.User)
                .OrderByDescending(r => r.RequestedAt)
                .Select(r => new
                {
                    r.RefundRequestId,
                    r.UserId,
                    UserName = r.User != null ? (r.User.FirstName + " " + r.User.LastName).Trim() : string.Empty,
                    UserEmail = r.User != null ? r.User.Email : string.Empty,
                    r.Amount,
                    r.Note,
                    r.Status,
                    r.RejectReason,
                    r.RequestedAt,
                    r.UpdatedAt,
                    r.BankName,
                    r.BankAccountNumber,
                    r.BankAccountHolder
                })
                .ToListAsync();

            return Ok(refunds);
        }

        [HttpPost("refunds/{id}/done")]
        public async Task<IActionResult> MarkRefundDone(string id)
        {
            var refund = await _context.RefundRequest.FindAsync(id);
            if (refund == null) return NotFound("Refund request not found.");

            if (refund.Status != "Pending")
                return BadRequest("Only pending refund requests can be processed.");

            refund.Status = "Processed";
            refund.UpdatedAt = DateTime.UtcNow.AddHours(7);

            await _context.SaveChangesAsync();

            try
            {
                await _notificationService.CreateAndSendAsync(new CreateNotificationDto
                {
                    UserId = refund.UserId,
                    Title = "Refund Processed",
                    Message = $"Your refund request for {refund.Amount:N0} VND has been processed and transferred to your bank account.",
                    Type = nameof(NotificationTypeEnum.Payment),
                    ReferenceId = refund.RefundRequestId
                });
            }
            catch { }

            return Ok(new { message = "Refund marked as processed." });
        }

        [HttpGet("user-list")]
        [EnableQuery(PageSize = 20, MaxTop = 100)]
        public IActionResult UserList()
        {
            return Ok(_accountService.QueryUserList());
        }

        [HttpPatch("users/{id}/ban")]
        public async Task<IActionResult> BanUser(string id)
        {
            var account = await _accountService.GetByIdAsync(id);
            if (account == null)
            {
                return NotFound("Account not found.");
            }

            var wasInactive = account.Status == AccountStatusEnum.Inactive.ToString();
            var banned = await _accountService.BanUserAsync(id);
            if (!banned)
            {
                return NotFound("Account not found.");
            }

            var message = wasInactive
                ? "User activated successfully."
                : "User banned successfully.";

            return Ok(new { message });
        }
    }
}