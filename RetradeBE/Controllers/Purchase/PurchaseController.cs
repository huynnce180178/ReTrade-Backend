using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
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
        [EnableQuery(PageSize = 20, MaxTop = 100)]
        public IActionResult GetBuyerPurchases(string buyerId)
        {
            return Ok(_purchaseService.QueryByBuyerId(buyerId));
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
    }
}
