using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.EntityFrameworkCore;
using RetradeBE.Models.DTOs;
using RetradeBE.Services;

namespace RetradeBE.Controllers.Purchase
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PurchaseController : ControllerBase
    {
        private readonly IPurchaseService _purchaseService;

        public PurchaseController(IPurchaseService purchaseService)
        {
            _purchaseService = purchaseService;
        }

        [HttpGet("buyer/{buyerId}")]
        public async Task<IActionResult> GetBuyerPurchases(string buyerId, [FromQuery] int? page, [FromQuery] int? pageSize, [FromQuery] string? status)
        {
            var query = _purchaseService.QueryByBuyerId(buyerId, status);
            if (page.HasValue)
            {
                var ps = pageSize.GetValueOrDefault(20);
                if (ps <= 0) ps = 20;
                if (ps > 100) ps = 100; // protect too-large requests

                var p = page.Value <= 0 ? 1 : page.Value;

                var total = await query.CountAsync();
                var items = await query.Skip((p - 1) * ps).Take(ps).ToListAsync();

                return Ok(new
                {
                    totalCount = total,
                    page = p,
                    pageSize = ps,
                    items
                });
            }

            // Otherwise return full query results (ensure newest-first ordering on DTO)
            var ordered = query.OrderByDescending(p => p.CreatedAt);
            return Ok(ordered);
        }

        [HttpGet("buyer/{buyerId}/{orderId}")]
        public async Task<IActionResult> GetBuyerPurchaseDetail(string buyerId, string orderId)
        {
            var purchase = await _purchaseService.GetByIdAsync(buyerId, orderId);
            if (purchase == null) return NotFound("Purchase not found.");

            return Ok(purchase);
        }

        [HttpPatch("buyer/{buyerId}/{orderId}/complete")]
        public async Task<IActionResult> CompletePurchase(string buyerId, string orderId)
        {
            try
            {
                var purchase = await _purchaseService.CompletePurchaseAsync(buyerId, orderId);
                if (purchase == null) return NotFound("Purchase not found.");

                return Ok(purchase);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPatch("buyer/{buyerId}/{orderId}/cancel")]
        public async Task<IActionResult> CancelPurchase(string buyerId, string orderId)
        {
            try
            {
                var purchase = await _purchaseService.CancelPurchaseAsync(buyerId, orderId);
                if (purchase == null) return NotFound("Purchase not found.");

                return Ok(purchase);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPatch("buyer/{buyerId}/{orderId}/return")]
        public async Task<IActionResult> RequestReturn(string buyerId, string orderId, ReturnPurchaseRequestDto dto)
        {
            try
            {
                var purchase = await _purchaseService.RequestReturnAsync(buyerId, orderId, dto);
                if (purchase == null) return NotFound("Purchase not found.");

                return Ok(purchase);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
