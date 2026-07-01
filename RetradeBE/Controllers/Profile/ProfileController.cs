using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using RetradeBE.Models.DTOs;
using RetradeBE.Services;
using System.Security.Claims;

namespace RetradeBE.Controllers.Profile
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProfileController : ControllerBase
    {
        private readonly IProfileService _profileService;

        public ProfileController(IProfileService profileService)
        {
            _profileService = profileService;
        }

        [Authorize]
        [HttpGet("my-profile")]
        public async Task<IActionResult> GetMyProfile()
        {
            var accountId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(accountId)) return Unauthorized();

            var profile = await _profileService.GetMyProfileAsync(accountId);
            if (profile == null) return NotFound("User profile not found.");

            return Ok(profile);
        }

        [HttpGet("user-profile/{userId}")]
        public async Task<IActionResult> GetUserProfile(string userId)
        {
            var profile = await _profileService.GetUserProfileAsync(userId);
            if (profile == null) return NotFound("User profile not found.");

            return Ok(profile);
        }

        [Authorize]
        [HttpPut("my-profile")]
        public async Task<IActionResult> UpdateMyProfile([FromBody] ProfileUpdateDto dto)
        {
            var accountId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(accountId)) return Unauthorized();

            try
            {
                var profile = await _profileService.UpdateMyProfileAsync(accountId, dto);
                if (profile == null) return NotFound("User profile not found.");

                return Ok(profile);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [Authorize]
        [HttpGet("my-vouchers")]
        [EnableQuery]
        public async Task<IActionResult> GetMyVouchers()
        {
            var accountId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(accountId)) return Unauthorized();

            try
            {
                var result = await _profileService.GetMyVouchersQueryAsync(accountId);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [Authorize]
        [HttpGet("my-vouchers/{userVoucherId}")]
        public async Task<IActionResult> GetMyVoucherDetail(string userVoucherId)
        {
            var accountId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(accountId)) return Unauthorized();

            try
            {
                var result = await _profileService.GetMyVoucherDetailAsync(accountId, userVoucherId);
                if (result == null) return NotFound("Voucher not found or access denied.");
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
