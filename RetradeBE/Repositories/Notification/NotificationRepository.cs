using Microsoft.EntityFrameworkCore;
using RetradeBE.Data;
using RetradeBE.Models;

namespace RetradeBE.Repositories
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly AppDbContext _context;

        public NotificationRepository(AppDbContext context)
        {
            _context = context;
        }

        public IQueryable<Notification> Query()
        {
            return _context.Notification
                .AsNoTracking()
                .Where(n => n.IsDeleted != true);
        }

        public async Task<Notification?> GetByIdAsync(string notificationId)
        {
            return await _context.Notification
                .FirstOrDefaultAsync(n => n.NotificationId == notificationId && n.IsDeleted != true);
        }

        public async Task AddAsync(Notification notification)
        {
            await _context.Notification.AddAsync(notification);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Notification notification)
        {
            _context.Notification.Update(notification);
            await _context.SaveChangesAsync();
        }

        public async Task<int> GetUnreadCountAsync(string userId)
        {
            return await _context.Notification
                .AsNoTracking()
                .CountAsync(n => n.UserId == userId && n.IsRead != true && n.IsDeleted != true);
        }

        public async Task MarkAllAsReadAsync(string userId)
        {
            var now = DateTime.UtcNow;
            await _context.Notification
                .Where(n => n.UserId == userId && n.IsRead != true && n.IsDeleted != true)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(n => n.IsRead, true)
                    .SetProperty(n => n.ReadAt, now));
        }
    }
}
