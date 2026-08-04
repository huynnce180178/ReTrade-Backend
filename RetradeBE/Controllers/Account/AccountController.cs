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
            var result = await _service.RegisterAsync(dto);
            if (result != "Register success. Please check your email for OTP.")
            {
                return BadRequest(new { message = result });
            }

            return Ok(new { message = result });
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
            var verified = await _service.VerifyAsync(dto);
            if (!verified)
            {
                return BadRequest(new { message = "Invalid or expired OTP." });
            }

            return Ok(new { verified = true, message = "Account verified successfully." });
        }

        [HttpPost("resend-otp")]
        public async Task<IActionResult> ResendOtp([FromBody] ResendOtpDto dto)
        {
            return Ok(await _service.ResendOtpAsync(dto.Email));
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            try
            {
                var response = await _service.LoginAsync(dto);
                if (response == null) return Unauthorized(new { code = "INVALID_CREDENTIALS", message = "Tên đăng nhập/email hoặc mật khẩu không chính xác." });

                return Ok(response);
            }
            catch (InvalidOperationException ex) when (ex.Message == "ACCOUNT_BANNED")
            {
                return Unauthorized(new { code = "ACCOUNT_BANNED", message = "Tài khoản của bạn đã bị KHÓA bởi Quản trị viên. Vui lòng kiểm tra email thông báo hoặc liên hệ Admin để được hỗ trợ." });
            }
            catch (InvalidOperationException ex) when (ex.Message == "ACCOUNT_INACTIVE")
            {
                return Unauthorized(new { code = "ACCOUNT_INACTIVE", message = "Tài khoản của bạn CHƯA ĐƯỢC KÍCH HOẠT. Vui lòng kiểm tra email để nhập mã OTP xác thực." });
            }
            catch (InvalidOperationException ex) when (ex.Message == "ACCOUNT_DELETED")
            {
                return Unauthorized(new { code = "ACCOUNT_DELETED", message = "Tài khoản này không tồn tại hoặc đã bị xóa khỏi hệ thống." });
            }
        }

        [HttpPost("login-with-google")]
        public async Task<IActionResult> LoginWithGoogle([FromBody] GoogleLoginDto dto)
        {
            if (string.IsNullOrEmpty(dto.AccessToken))
                return BadRequest("Google access token is required.");

            try
            {
                var response = await _service.LoginWithGoogleAsync(dto.AccessToken);
                if (response == null) return Unauthorized(new { code = "INVALID_CREDENTIALS", message = "Xác thực Google thất bại. Token không hợp lệ hoặc phiên làm việc đã hết hạn. Vui lòng thử lại." });

                return Ok(response);
            }
            catch (InvalidOperationException ex) when (ex.Message == "ACCOUNT_BANNED")
            {
                return Unauthorized(new { code = "ACCOUNT_BANNED", message = "Tài khoản của bạn đã bị KHÓA bởi Quản trị viên. Vui lòng kiểm tra email thông báo hoặc liên hệ Admin để được hỗ trợ." });
            }
            catch (InvalidOperationException ex) when (ex.Message == "ACCOUNT_INACTIVE")
            {
                return Unauthorized(new { code = "ACCOUNT_INACTIVE", message = "Tài khoản của bạn CHƯA ĐƯỢC KÍCH HOẠT. Vui lòng kiểm tra email để nhập mã OTP xác thực." });
            }
            catch (InvalidOperationException ex) when (ex.Message == "ACCOUNT_DELETED")
            {
                return Unauthorized(new { code = "ACCOUNT_DELETED", message = "Tài khoản này không tồn tại hoặc đã bị xóa khỏi hệ thống." });
            }
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
        {
            var result = await _service.ForgotPasswordAsync(dto.Email);
            if (result != "Password reset OTP has been sent to your email.")
            {
                return BadRequest(new { message = result });
            }
            return Ok(new { message = result });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            var result = await _service.ResetPasswordAsync(dto);
            if (result != "Password has been reset successfully.")
            {
                return BadRequest(new { message = result });
            }
            return Ok(new { message = result });
        }

        [HttpPost("password-recovery")]
        public async Task<IActionResult> PasswordRecovery([FromBody] ForgotPasswordDto dto)
        {
            var result = await _service.PasswordRecoveryAsync(dto.Email);
            if (result != "Password reset successful. Please check your email for your new password.")
            {
                return BadRequest(new { message = result });
            }
            return Ok(new { message = result });
        }

        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] Models.DTOs.ChangePasswordDto dto)
        {
            var accountId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(accountId)) return Unauthorized();

            var result = await _service.ChangePasswordAsync(accountId, dto);
            if (result != "Password changed successfully.")
            {
                return BadRequest(new { message = result });
            }
            return Ok(new { message = result });
        }

        [Authorize]
        [HttpPost("set-password")]
        public async Task<IActionResult> SetPassword([FromBody] Models.DTOs.SetPasswordDto dto)
        {
            var accountId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(accountId)) return Unauthorized();

            var result = await _service.SetPasswordAsync(accountId, dto);
            if (result != "Password set successfully.")
            {
                return BadRequest(new { message = result });
            }
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

        [Authorize]
        [HttpPatch("deactivate-me")]
        public async Task<IActionResult> DeactivateMe()
        {
            var accountId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(accountId)) return Unauthorized();

            var result = await _service.DeactivateMyAccountAsync(accountId);
            if (!result)
            {
                return BadRequest("Unable to deactivate account.");
            }

            return Ok(new { message = "Account has been deactivated successfully." });
        }

        
    }
}
