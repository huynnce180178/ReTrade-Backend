using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using RetradeBE.Models.DTOs.AssistantChat;
using RetradeBE.Services.AssistantChat;

namespace RetradeBE.Controllers.AssistantChat
{
    [ApiController]
    [Route("api/assistant/chat")]
    public class AssistantChatController : ControllerBase
    {
        private readonly IAssistantChatService _assistantChatService;

        public AssistantChatController(IAssistantChatService assistantChatService)
        {
            _assistantChatService = assistantChatService;
        }

        [HttpPost]
        public async Task<IActionResult> SendMessage([FromBody] AssistantChatRequestDto request, CancellationToken cancellationToken)
        {
            var accountId = GetAccountId();

            try
            {
                var response = await _assistantChatService.SendChatAssistantAsync(accountId, request, cancellationToken);
                return Ok(response);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("{sessionId}")]
        public async Task<IActionResult> GetHistory(string sessionId)
        {
            var accountId = GetAccountId();

            try
            {
                var history = await _assistantChatService.GetSessionHistoryAsync(accountId, sessionId);
                return Ok(history);
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
