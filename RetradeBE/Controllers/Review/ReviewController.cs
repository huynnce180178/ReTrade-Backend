using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetradeBE.Models.DTOs;
using RetradeBE.Services;

namespace RetradeBE.Controllers.Review
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ReviewController : ControllerBase
    {
        private readonly IReviewService _reviewService;

        public ReviewController(IReviewService reviewService)
        {
            _reviewService = reviewService;
        }

        [HttpGet("buyer/{buyerId}/order/{orderId}")]
        public async Task<IActionResult> GetReviewByBuyerAndOrder(string buyerId, string orderId)
        {
            var review = await _reviewService.GetByBuyerOrderAsync(buyerId, orderId);
            if (review == null) return NotFound("Review not found.");

            return Ok(review);
        }

        [HttpPost("buyer/{buyerId}")]
        public async Task<IActionResult> CreateReview(string buyerId, [FromBody] ReviewCreateDto request)
        {
            try
            {
                var review = await _reviewService.CreateAsync(buyerId, request);
                if (review == null) return NotFound("Order not found.");

                return CreatedAtAction(nameof(GetReviewByBuyerAndOrder), new { buyerId, orderId = request.OrderId }, review);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
