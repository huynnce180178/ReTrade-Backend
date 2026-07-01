using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RetradeBE.Data;
using RetradeBE.Models;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace RetradeBE.Controllers.Refund
{
    public class UpdateBankDetailsDto
    {
        public string BankName { get; set; } = null!;
        public string BankAccountNumber { get; set; } = null!;
        public string BankAccountHolder { get; set; } = null!;
    }

    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class RefundController : ControllerBase
    {
        private readonly AppDbContext _context;

        public RefundController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("my")]
        public async Task<IActionResult> GetMyRefunds()
        {
            var accountId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(accountId)) return Unauthorized();

            var account = await _context.Account.AsNoTracking().FirstOrDefaultAsync(a => a.AccountId == accountId);
            if (account == null || string.IsNullOrEmpty(account.UserId)) return Unauthorized();

            var refunds = await _context.RefundRequest
                .AsNoTracking()
                .Where(r => r.UserId == account.UserId)
                .OrderByDescending(r => r.RequestedAt)
                .Select(r => new
                {
                    r.RefundRequestId,
                    r.UserId,
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

        [HttpPut("{id}/bank-details")]
        public async Task<IActionResult> UpdateBankDetails(string id, [FromBody] UpdateBankDetailsDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.BankName) || 
                string.IsNullOrWhiteSpace(dto.BankAccountNumber) || 
                string.IsNullOrWhiteSpace(dto.BankAccountHolder))
            {
                return BadRequest("Invalid bank details.");
            }

            var accountId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(accountId)) return Unauthorized();

            var account = await _context.Account.AsNoTracking().FirstOrDefaultAsync(a => a.AccountId == accountId);
            if (account == null || string.IsNullOrEmpty(account.UserId)) return Unauthorized();

            var refund = await _context.RefundRequest.FindAsync(id);
            if (refund == null) return NotFound("Refund request not found.");

            if (refund.UserId != account.UserId) return Forbid();

            if (refund.Status != "NotReady" && refund.Status != "Pending")
            {
                return BadRequest("Bank details can only be edited for NotReady or Pending refund requests.");
            }

            refund.BankName = dto.BankName.Trim();
            refund.BankAccountNumber = dto.BankAccountNumber.Trim();
            refund.BankAccountHolder = dto.BankAccountHolder.Trim();

            if (refund.Status == "NotReady")
            {
                refund.Status = "Pending";
            }

            refund.UpdatedAt = DateTime.UtcNow.AddHours(7);

            await _context.SaveChangesAsync();
            return Ok(refund);
        }

        [HttpPost("{id}/received")]
        public async Task<IActionResult> ConfirmReceived(string id)
        {
            var accountId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(accountId)) return Unauthorized();

            var account = await _context.Account.AsNoTracking().FirstOrDefaultAsync(a => a.AccountId == accountId);
            if (account == null || string.IsNullOrEmpty(account.UserId)) return Unauthorized();

            var refund = await _context.RefundRequest.FindAsync(id);
            if (refund == null) return NotFound("Refund request not found.");

            if (refund.UserId != account.UserId) return Forbid();

            if (refund.Status != "Processed")
            {
                return BadRequest("Confirming receipt is only allowed after Admin has processed the refund.");
            }

            refund.Status = "Completed";
            refund.UpdatedAt = DateTime.UtcNow.AddHours(7);

            // If it is an auction deposit refund, update AuctionDeposit status to "Refunded"
            if (!string.IsNullOrWhiteSpace(refund.Note) && refund.Note.Contains("Auction"))
            {
                var auctionId = ExtractAuctionId(refund.Note);
                if (!string.IsNullOrEmpty(auctionId))
                {
                    var deposit = await _context.AuctionDeposit
                        .Where(d => d.AuctionId == auctionId && d.UserId == refund.UserId && d.Status == "RefundPending")
                        .FirstOrDefaultAsync();

                    if (deposit != null)
                    {
                        deposit.Status = "Refunded";
                    }
                }
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Refund confirmed as received." });
        }

        private static string? ExtractAuctionId(string note)
        {
            int index = note.IndexOf("AUC_", StringComparison.OrdinalIgnoreCase);
            if (index == -1) return null;

            var sub = note.Substring(index);
            var endChars = new[] { ' ', '.', ',' };
            int endIdx = sub.IndexOfAny(endChars);
            return endIdx == -1 ? sub : sub.Substring(0, endIdx);
        }
    }
}
