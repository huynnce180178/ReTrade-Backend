using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetradeBE.Models.DTOs;
using RetradeBE.Services.Offer;

namespace RetradeBE.Controllers.Offer
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class OfferController : ControllerBase
    {
        private readonly IOfferService _offerService;

        public OfferController(IOfferService offerService)
        {
            _offerService = offerService;
        }

        /// <summary>Buyer: submit a price offer on a product</summary>
        [HttpPost]
        public async Task<IActionResult> MakeOffer([FromBody] MakeOfferRequestDto request)
        {
            try
            {
                var accountId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(accountId)) return Unauthorized();
                var offer = await _offerService.MakeOfferAsync(accountId, request);
                return Ok(offer);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>Buyer: get my own offers (optionally filter by productId)</summary>
        [HttpGet("my-offers")]
        public async Task<IActionResult> GetMyOffers([FromQuery] string? productId)
        {
            try
            {
                var accountId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(accountId)) return Unauthorized();
                var offers = await _offerService.GetMyOffersAsync(accountId, productId);
                return Ok(offers);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>Seller: view all offers on a specific product</summary>
        [HttpGet("product/{productId}")]
        public async Task<IActionResult> GetOffersForProduct(string productId, [FromQuery] string sellerId)
        {
            try
            {
                if (string.IsNullOrEmpty(sellerId)) return BadRequest("sellerId is required.");
                var offers = await _offerService.GetOffersForProductAsync(sellerId, productId);
                return Ok(offers);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>Buyer: cancel a pending offer</summary>
        [HttpPatch("{offerId}/cancel")]
        public async Task<IActionResult> CancelOffer(string offerId)
        {
            try
            {
                var accountId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(accountId)) return Unauthorized();

                // Resolve userId from accountId
                var account = HttpContext.RequestServices
                    .GetRequiredService<RetradeBE.Data.AppDbContext>()
                    .Account.FirstOrDefault(a => a.AccountId == accountId);
                if (account == null) return Unauthorized();

                var offer = await _offerService.CancelOfferAsync(account.UserId!, offerId);
                return Ok(offer);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>Buyer: checkout using an accepted offer</summary>
        [HttpPost("checkout")]
        public async Task<IActionResult> CheckoutFromOffer([FromBody] OfferCheckoutRequestDto request)
        {
            try
            {
                var accountId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(accountId)) return Unauthorized();
                var orderId = await _offerService.CheckoutFromOfferAsync(request, accountId);
                return Ok(new { OrderId = orderId, Message = "Offer checkout successful" });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
