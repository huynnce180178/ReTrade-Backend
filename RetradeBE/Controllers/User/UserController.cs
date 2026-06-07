using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using RetradeBE.Repositories;
using RetradeBE.Services;
using System.Security.Claims;

namespace RetradeBE.Controllers.User
{
    public class UploadAvatarDto
    {
        public IFormFile Avatar { get; set; } = null!;
    }

    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly ICloudinaryService _cloudinaryService;
        private readonly IUserRepository _userRepository;
        private readonly IAccountRepository _accountRepository;

        public UserController(ICloudinaryService cloudinaryService, IUserRepository userRepository, IAccountRepository accountRepository)
        {
            _cloudinaryService = cloudinaryService;
            _userRepository = userRepository;
            _accountRepository = accountRepository;
        }

        [Authorize]
        [HttpPost("upload-avatar")]
        public async Task<IActionResult> UploadAvatar([FromForm] UploadAvatarDto dto)
        {
            var avatar = dto.Avatar;
            if (avatar == null || avatar.Length == 0)
                return BadRequest("No file uploaded.");

            var accountId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(accountId)) return Unauthorized();

            var account = await _accountRepository.GetByIdAsync(accountId);
            if (account == null) return NotFound("Account not found.");
            if (string.IsNullOrEmpty(account.UserId)) return BadRequest("Associated user not found for this account.");

            // Upload to Cloudinary under folder User/profile
            var url = await _cloudinaryService.UploadImageAsync(avatar, "User/profile");
            if (string.IsNullOrEmpty(url)) return StatusCode(500, "Failed to upload image.");

            var user = await _userRepository.GetByIdAsync(account.UserId);
            if (user == null) return NotFound("User not found.");

            user.AvatarUrl = url;
            user.UpdatedAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);

            return Ok(new { avatarUrl = url });
        }
    }
}
