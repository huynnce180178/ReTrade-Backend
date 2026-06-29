using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetradeBE.Models.DTOs;
using RetradeBE.Models.Enums;
using RetradeBE.Services;
using System.Security.Claims;

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

        [HttpGet("seller")]
        [Authorize(Roles = $"{nameof(RoleEnum.Seller)},{nameof(RoleEnum.Admin)}")]
        public async Task<IActionResult> GetSellerReviews([FromQuery] ReviewQueryDto query)
        {
            var accountId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(accountId)) return Unauthorized();

            if (User.IsInRole(nameof(RoleEnum.Admin)))
            {
                return Ok(await _reviewService.GetAdminReviewsAsync(query));
            }

            return Ok(await _reviewService.GetSellerReviewsAsync(accountId, query));
        }

        [HttpGet("seller/summary")]
        [Authorize(Roles = $"{nameof(RoleEnum.Seller)},{nameof(RoleEnum.Admin)}")]
        public async Task<IActionResult> GetSellerReviewSummary([FromQuery] ReviewQueryDto query)
        {
            var accountId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(accountId)) return Unauthorized();

            if (User.IsInRole(nameof(RoleEnum.Admin)))
            {
                return Ok(await _reviewService.GetAdminReviewSummaryAsync(query));
            }

            return Ok(await _reviewService.GetSellerReviewSummaryAsync(accountId));
        }

        [HttpGet("admin")]
        [Authorize(Roles = nameof(RoleEnum.Admin))]
        public async Task<IActionResult> GetAdminReviews([FromQuery] ReviewQueryDto query)
        {
            return Ok(await _reviewService.GetAdminReviewsAsync(query));
        }

        [HttpGet("admin/summary")]
        [Authorize(Roles = nameof(RoleEnum.Admin))]
        public async Task<IActionResult> GetAdminReviewSummary([FromQuery] ReviewQueryDto query)
        {
            return Ok(await _reviewService.GetAdminReviewSummaryAsync(query));
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

        [HttpPost("{reviewId}/report")]
        [Authorize(Roles = $"{nameof(RoleEnum.Seller)},{nameof(RoleEnum.Admin)}")]
        public async Task<IActionResult> ReportReview(string reviewId, [FromBody] ReviewReportCreateDto request)
        {
            var accountId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(accountId)) return Unauthorized();

            try
            {
                var report = await _reviewService.ReportReviewAsync(
                    accountId,
                    reviewId,
                    request,
                    User.IsInRole(nameof(RoleEnum.Admin)));

                return CreatedAtAction(nameof(GetSellerReviews), new { }, report);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
