using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetradeBE.Models.DTOs;
using RetradeBE.Services;

namespace RetradeBE.Controllers.Notification
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpGet]
        public async Task<IActionResult> GetNotifications([FromQuery] string userId, [FromQuery] NotificationQueryDto query)
        {
            if (string.IsNullOrWhiteSpace(userId)) return BadRequest("UserId is required.");

            return Ok(await _notificationService.GetNotificationsAsync(userId, query));
        }

        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount([FromQuery] string userId)
        {
            if (string.IsNullOrWhiteSpace(userId)) return BadRequest("UserId is required.");

            var count = await _notificationService.GetUnreadCountAsync(userId);
            return Ok(new { count });
        }

        [HttpPatch("{notificationId}/read")]
        public async Task<IActionResult> MarkAsRead(string notificationId, [FromQuery] string userId)
        {
            if (string.IsNullOrWhiteSpace(userId)) return BadRequest("UserId is required.");

            var result = await _notificationService.MarkAsReadAsync(userId, notificationId);
            if (result == null) return NotFound("Notification not found.");

            return Ok(result);
        }

        [HttpPatch("read-all")]
        public async Task<IActionResult> MarkAllAsRead([FromQuery] string userId)
        {
            if (string.IsNullOrWhiteSpace(userId)) return BadRequest("UserId is required.");

            await _notificationService.MarkAllAsReadAsync(userId);
            return Ok(new { message = "All notifications marked as read." });
        }

        [HttpDelete("{notificationId}")]
        public async Task<IActionResult> DeleteNotification(string notificationId, [FromQuery] string userId)
        {
            if (string.IsNullOrWhiteSpace(userId)) return BadRequest("UserId is required.");

            var result = await _notificationService.DeleteNotificationAsync(userId, notificationId);
            if (!result) return NotFound("Notification not found.");

            return Ok(new { message = "Notification deleted." });
        }

        [HttpPost("test")]
        public async Task<IActionResult> SendTestNotification([FromQuery] string userId)
        {
            if (string.IsNullOrWhiteSpace(userId)) return BadRequest("UserId is required.");
            
            var result = await _notificationService.CreateAndSendAsync(new CreateNotificationDto
            {
                UserId = userId,
                Title = "Test Notification for Admin",
                Message = "This is a test notification to verify the bell dropdown works correctly.",
                Type = "System",
                ReferenceId = "test_123"
            });

            return Ok(result);
        }
    }
}
