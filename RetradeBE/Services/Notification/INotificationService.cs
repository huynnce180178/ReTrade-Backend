using RetradeBE.Models.DTOs;

namespace RetradeBE.Services
{
    public interface INotificationService
    {
        Task<PagedResultDto<NotificationDto>> GetNotificationsAsync(string userId, NotificationQueryDto query);
        Task<int> GetUnreadCountAsync(string userId);
        Task<NotificationDto?> MarkAsReadAsync(string userId, string notificationId);
        Task MarkAllAsReadAsync(string userId);
        Task<bool> DeleteNotificationAsync(string userId, string notificationId);
        Task<NotificationDto> CreateAndSendAsync(CreateNotificationDto dto);
    }
}
