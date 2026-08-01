using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetradeBE.Models.DTOs;
using RetradeBE.Services;

namespace RetradeBE.Controllers.Chat
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;
        private readonly ICloudinaryService _cloudinaryService;

        public ChatController(IChatService chatService, ICloudinaryService cloudinaryService)
        {
            _chatService = chatService;
            _cloudinaryService = cloudinaryService;
        }

        [HttpGet("rooms")]
        public async Task<IActionResult> GetRooms()
        {
            var accountId = GetAccountId();
            if (accountId == null) return Unauthorized();

            var rooms = await _chatService.GetRoomsAsync(accountId);
            return Ok(rooms);
        }

        [HttpPost("rooms")]
        public async Task<IActionResult> GetOrCreateRoom([FromBody] CreateChatRoomRequestDto request)
        {
            var accountId = GetAccountId();
            if (accountId == null) return Unauthorized();

            try
            {
                var room = await _chatService.GetOrCreateRoomAsync(accountId, request);
                return Ok(room);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, ex.Message);
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

        [HttpGet("{roomId}/messages")]
        public async Task<IActionResult> GetMessages(string roomId, [FromQuery] int page = 1, [FromQuery] int limit = 30)
        {
            var accountId = GetAccountId();
            if (accountId == null) return Unauthorized();

            try
            {
                var messages = await _chatService.GetMessagesAsync(accountId, roomId, page, limit);
                return Ok(messages);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpPost("{roomId}/messages")]
        public async Task<IActionResult> SendMessage(string roomId, [FromBody] SendMessageRequestDto request)
        {
            var accountId = GetAccountId();
            if (accountId == null) return Unauthorized();

            try
            {
                var message = await _chatService.SendMessageAsync(accountId, roomId, request);
                return CreatedAtAction(nameof(GetMessages), new { roomId }, message);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, ex.Message);
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

        [HttpDelete("{roomId}/messages/{messageId}")]
        public async Task<IActionResult> DeleteMessage(string roomId, string messageId)
        {
            var accountId = GetAccountId();
            if (accountId == null) return Unauthorized();

            try
            {
                await _chatService.DeleteMessageAsync(accountId, roomId, messageId);
                return Ok(new { deleted = true });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, ex.Message);
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

        [HttpDelete("{roomId}/messages")]
        public async Task<IActionResult> ClearRoomMessages(string roomId)
        {
            var accountId = GetAccountId();
            if (accountId == null) return Unauthorized();

            try
            {
                await _chatService.ClearRoomMessagesAsync(accountId, roomId);
                return Ok(new { cleared = true });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, ex.Message);
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


        [HttpPost("{roomId}/messages/{messageId}/recall")]
        public async Task<IActionResult> RecallMessage(string roomId, string messageId)
        {
            var accountId = GetAccountId();
            if (accountId == null) return Unauthorized();

            try
            {
                var message = await _chatService.RecallMessageAsync(accountId, roomId, messageId);
                return Ok(message);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, ex.Message);
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

        [HttpPost("upload-image")]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded.");
            }

            if (!file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest("Only image files are allowed.");
            }

            try
            {
                var url = await _cloudinaryService.UploadImageAsync(file, "Chat/images");
                if (string.IsNullOrWhiteSpace(url))
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, "Failed to upload image.");
                }

                return Ok(new { imageUrl = url });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("{roomId}/read")]
        public async Task<IActionResult> MarkAsRead(string roomId)
        {
            var accountId = GetAccountId();
            if (accountId == null) return Unauthorized();

            try
            {
                var count = await _chatService.MarkMessagesAsReadAsync(accountId, roomId);
                return Ok(new { readCount = count });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        private string? GetAccountId() => User.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}
