using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetradeBE.Services;
using System.Security.Claims;

namespace RetradeBE.Controllers.Seller
{
    [Route("api/[controller]")]
    [ApiController]
    public class SellerController : ControllerBase
    {
        private readonly IProfileService _profileService;

        public SellerController(IProfileService profileService)
        {
            _profileService = profileService;
        }

        [HttpGet("{sellerId}")]
        public async Task<IActionResult> GetSellerInformation(string sellerId)
        {
            var currentAccountId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var seller = await _profileService.GetSellerInformationAsync(sellerId, currentAccountId);
            if (seller == null) return NotFound("Seller not found.");

            return Ok(seller);
        }

        [Authorize]
        [HttpPost("{sellerId}/follow")]
        public async Task<IActionResult> FollowSeller(string sellerId)
        {
            var accountId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(accountId)) return Unauthorized();

            try
            {
                var result = await _profileService.FollowSellerAsync(accountId, sellerId);
                if (result == null) return NotFound("Seller not found.");

                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [Authorize]
        [HttpDelete("{sellerId}/follow")]
        public async Task<IActionResult> UnfollowSeller(string sellerId)
        {
            var accountId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(accountId)) return Unauthorized();

            var result = await _profileService.UnfollowSellerAsync(accountId, sellerId);
            if (result == null) return NotFound("Seller not found.");

            return Ok(result);
        }
    }
}
