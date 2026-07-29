using RetradeBE.Models;

namespace RetradeBE.Repositories
{
    public interface INotificationRepository
    {
        IQueryable<Notification> Query();
        Task<Notification?> GetByIdAsync(string notificationId);
        Task AddAsync(Notification notification);
        Task UpdateAsync(Notification notification);
        Task<int> GetUnreadCountAsync(string userId);
        Task MarkAllAsReadAsync(string userId);
    }
}
