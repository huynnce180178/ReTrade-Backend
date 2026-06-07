using Microsoft.AspNetCore.Mvc;
using RetradeBE.Models.DTOs;
using RetradeBE.Services;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OData.Query;

namespace RetradeBE.Controllers.Account
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly IAccountService _service;

        public AccountController(IAccountService service)
        {
            _service = service;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            return Ok(await _service.RegisterAsync(dto));
        }

        [HttpGet]
        [EnableQuery]
        public async Task<IActionResult> GetAll()
        {
            var accounts = await _service.GetAllAsync();
            return Ok(accounts.AsQueryable());
        }

        [HttpPost("verify")]
        public async Task<IActionResult> Verify([FromBody] VerifyDto dto)
        {
            return Ok(await _service.VerifyAsync(dto));
        }

        [HttpPost("resend-otp")]
        public async Task<IActionResult> ResendOtp([FromBody] ResendOtpDto dto)
        {
            return Ok(await _service.ResendOtpAsync(dto.Email));
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var response = await _service.LoginAsync(dto);
            if (response == null) return Unauthorized("Invalid credentials or account not active.");
            
            return Ok(response);
        }

        [HttpPost("login-with-google")]
        public async Task<IActionResult> LoginWithGoogle([FromBody] GoogleLoginDto dto)
        {
            if (string.IsNullOrEmpty(dto.AccessToken))
                return BadRequest("Google access token is required.");

            var response = await _service.LoginWithGoogleAsync(dto.AccessToken);
            if (response == null) return Unauthorized("Google login failed. Invalid token or account disabled.");

            return Ok(response);
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
        {
            var result = await _service.ForgotPasswordAsync(dto.Email);
            if (result.Contains("not found") || result.Contains("No account"))
            {
                return BadRequest(result);
            }
            return Ok(new { message = result });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            var result = await _service.ResetPasswordAsync(dto);
            if (result.Contains("Invalid") || result.Contains("not found"))
            {
                return BadRequest(result);
            }
            return Ok(new { message = result });
        }

        [HttpPost("password-recovery")]
        public async Task<IActionResult> PasswordRecovery([FromBody] ForgotPasswordDto dto)
        {
            var result = await _service.PasswordRecoveryAsync(dto.Email);
            if (result.Contains("not found"))
            {
                return BadRequest(result);
            }
            return Ok(new { message = result });
        }

        [Authorize]
        [HttpGet("my-account")]
        public async Task<IActionResult> GetMyAccount()
        {
            var accountId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(accountId)) return Unauthorized();

            var profile = await _service.GetProfileAsync(accountId);
            if (profile == null) return NotFound("User not found.");

            return Ok(profile);
        }

        [Authorize]
        [HttpPut("my-account")]
        public async Task<IActionResult> UpdateProfile([FromBody] Models.DTOs.UpdateProfileDto dto)
        {
            var accountId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(accountId)) return Unauthorized();

            var profile = await _service.UpdateProfileAsync(accountId, dto);
            if (profile == null) return NotFound("User not found.");

            return Ok(profile);
        }

        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] Models.DTOs.ChangePasswordDto dto)
        {
            var accountId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(accountId)) return Unauthorized();

            var result = await _service.ChangePasswordAsync(accountId, dto);
            if (result.Contains("incorrect") || result.Contains("not found")) return BadRequest(result);
            return Ok(new { message = result });
        }



        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var role = User.FindFirstValue(ClaimTypes.Role);

            if (currentUserId != id)
            {
                return StatusCode(403, "You do not have permission to delete this account.");
            }

            var account = await _service.GetByIdAsync(id);
            if (account == null)
            {
                return NotFound("Account not found.");
            }

            await _service.DeleteAsync(id);
            return NoContent();
        }

        [Authorize]
        [HttpPut("restore/{id}")]
        public async Task<IActionResult> Restore(string id)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (currentUserId != id && !User.IsInRole(RetradeBE.Models.Enums.RoleEnum.Admin.ToString()))
            {
                return StatusCode(403, "You do not have permission to restore this account.");
            }

            var account = await _service.GetByIdAsync(id);
            if (account == null)
            {
                return NotFound("Account not found.");
            }

            await _service.RestoreAsync(id);
            return Ok(new { message = "Account has been restored successfully." });
        }

        
    }
}
