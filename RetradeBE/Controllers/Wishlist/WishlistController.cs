using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using RetradeBE.Models.DTOs;
using RetradeBE.Services;
using System.Security.Claims;
using System.Threading.Tasks;

namespace RetradeBE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class WishlistController : ControllerBase
    {
        private readonly IWishlistService _service;

        public WishlistController(IWishlistService service)
        {
            _service = service;
        }

        [HttpPost]
        public async Task<IActionResult> AddToWishlist([FromBody] AddToWishlistDto dto)
        {
            var accountId = GetAccountId();
            if (accountId == null) return Unauthorized();

            try
            {
                var result = await _service.AddToWishlistAsync(accountId, dto);
                return Ok(result);
            }
            catch (System.Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetWishlistDetail()
        {
            var accountId = GetAccountId();
            if (accountId == null) return Unauthorized();

            try
            {
                var result = await _service.GetWishlistDetailAsync(accountId);
                return Ok(result);
            }
            catch (System.Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("items")]
        [EnableQuery]
        public async Task<IActionResult> GetWishlistItems()
        {
            var accountId = GetAccountId();
            if (accountId == null) return Unauthorized();

            try
            {
                var result = await _service.GetWishlistItemsQueryAsync(accountId);
                return Ok(result);
            }
            catch (System.Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("items/{wishlistItemId}")]
        public async Task<IActionResult> RemoveWishlistItem(string wishlistItemId)
        {
            var accountId = GetAccountId();
            if (accountId == null) return Unauthorized();

            try
            {
                await _service.RemoveWishlistItemAsync(accountId, wishlistItemId);
                return NoContent();
            }
            catch (System.Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        private string? GetAccountId() =>
            User.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}
