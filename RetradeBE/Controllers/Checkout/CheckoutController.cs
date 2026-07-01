using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetradeBE.Models.DTOs;
using RetradeBE.Services.Checkout;

namespace RetradeBE.Controllers.Checkout
{
    [Route("api/[controller]")]
    [ApiController]
    public class CheckoutController : ControllerBase
    {
        private readonly ICheckoutService _checkoutService;

        public CheckoutController(ICheckoutService checkoutService)
        {
            _checkoutService = checkoutService;
        }

        [HttpPost("calculate-fee")]
        [Authorize]
        public async Task<IActionResult> CalculateFee([FromBody] CalculateFeeRequestDto request)
        {
            try
            {
                var response = await _checkoutService.CalculateShippingFeeAsync(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> ProcessCheckout([FromBody] CheckoutRequestDto request)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("User not found in token");
                }

                var orderId = await _checkoutService.ProcessCheckoutAsync(request, userId);
                return Ok(new { OrderId = orderId, Message = "Checkout successful" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("validate-voucher")]
        [Authorize]
        public async Task<IActionResult> ValidateVoucher([FromQuery] string code, [FromQuery] string productId)
        {
            try
            {
                var response = await _checkoutService.ValidateVoucherAsync(code, productId);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}

