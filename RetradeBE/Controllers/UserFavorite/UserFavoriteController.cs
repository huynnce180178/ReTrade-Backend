using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetradeBE.Models.DTOs;
using RetradeBE.Services;
using System.Security.Claims;

namespace RetradeBE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UserFavoriteController : ControllerBase
    {
        private readonly IUserFavoriteService _service;

        public UserFavoriteController(IUserFavoriteService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetFavorites()
        {
            var accountId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(accountId))
                return Unauthorized();

            try
            {
                var result = await _service.GetFavoritesAsync(accountId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddFavorite([FromBody] UserFavoriteCreateDto dto)
        {
            var accountId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(accountId))
                return Unauthorized();

            try
            {
                var result = await _service.AddFavoriteAsync(accountId, dto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("{categoryId}")]
        public async Task<IActionResult> RemoveFavorite(string categoryId)
        {
            var accountId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(accountId))
                return Unauthorized();

            try
            {
                await _service.RemoveFavoriteAsync(accountId, categoryId);
                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
