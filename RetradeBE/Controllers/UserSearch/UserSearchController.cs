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
    public class UserSearchController : ControllerBase
    {
        private readonly IUserSearchService _service;

        public UserSearchController(IUserSearchService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetHistory([FromQuery] int limit = 20)
        {
            var accountId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(accountId))
                return Unauthorized();

            try
            {
                var result = await _service.GetSearchHistoryAsync(accountId, limit);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveSearch([FromBody] UserSearchCreateDto dto)
        {
            var accountId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(accountId))
                return Unauthorized();

            try
            {
                var result = await _service.SaveSearchAsync(accountId, dto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSearch(string id)
        {
            var accountId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(accountId))
                return Unauthorized();

            try
            {
                await _service.DeleteSearchAsync(accountId, id);
                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete]
        public async Task<IActionResult> ClearAll()
        {
            var accountId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(accountId))
                return Unauthorized();

            try
            {
                await _service.ClearAllSearchAsync(accountId);
                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
