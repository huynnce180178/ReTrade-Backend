using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using RetradeBE.Data;
using RetradeBE.Hubs;
using RetradeBE.Models;
using RetradeBE.Models.DTOs;
using RetradeBE.Repositories;

namespace RetradeBE.Services
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly IHubContext<NotificationHub> _notificationHub;
        private readonly AppDbContext _context;

        public NotificationService(
            INotificationRepository notificationRepository,
            IHubContext<NotificationHub> notificationHub,
            AppDbContext context)
        {
            _notificationRepository = notificationRepository;
            _notificationHub = notificationHub;
            _context = context;
        }

        public async Task<PagedResultDto<NotificationDto>> GetNotificationsAsync(string userId, NotificationQueryDto query)
        {
            query.Page = query.Page < 1 ? 1 : query.Page;
            query.PageSize = query.PageSize < 1 ? 20 : Math.Min(query.PageSize, 50);

            var notifications = _notificationRepository.Query()
                .Where(n => n.UserId == userId);

            if (!string.IsNullOrWhiteSpace(query.Type))
            {
                notifications = notifications.Where(n => n.Type == query.Type);
            }

            if (query.IsRead.HasValue)
            {
                notifications = query.IsRead.Value
                    ? notifications.Where(n => n.IsRead == true)
                    : notifications.Where(n => n.IsRead != true);
            }

            var totalItems = await notifications.CountAsync();
            var totalPages = Math.Max(1, (int)Math.Ceiling((double)totalItems / query.PageSize));

            var items = await notifications
                .OrderByDescending(n => n.CreatedAt)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(n => new NotificationDto
                {
                    NotificationId = n.NotificationId,
                    UserId = n.UserId,
                    Title = n.Title,
                    Message = n.Message,
                    Type = n.Type,
                    ReferenceId = n.ReferenceId,
                    IsRead = n.IsRead,
                    ReadAt = n.ReadAt,
                    CreatedAt = n.CreatedAt
                })
                .ToListAsync();

            return new PagedResultDto<NotificationDto>
            {
                Items = items,
                TotalItems = totalItems,
                Page = query.Page,
                PageSize = query.PageSize,
                TotalPages = totalPages
            };
        }

        public Task<int> GetUnreadCountAsync(string userId)
        {
            return _notificationRepository.GetUnreadCountAsync(userId);
        }

        public async Task<NotificationDto?> MarkAsReadAsync(string userId, string notificationId)
        {
            var notification = await _notificationRepository.GetByIdAsync(notificationId);
            if (notification == null || notification.UserId != userId)
            {
                return null;
            }

            if (notification.IsRead != true)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
                await _notificationRepository.UpdateAsync(notification);

                var unreadCount = await _notificationRepository.GetUnreadCountAsync(userId);
                await _notificationHub.Clients
                    .Group(NotificationHub.GetUserGroupName(userId))
                    .SendAsync("UnreadCountUpdated", new { count = unreadCount });
            }

            return ToDto(notification);
        }

        public async Task MarkAllAsReadAsync(string userId)
        {
            await _notificationRepository.MarkAllAsReadAsync(userId);

            await _notificationHub.Clients
                .Group(NotificationHub.GetUserGroupName(userId))
                .SendAsync("AllNotificationsRead");

            await _notificationHub.Clients
                .Group(NotificationHub.GetUserGroupName(userId))
                .SendAsync("UnreadCountUpdated", new { count = 0 });
        }

        public async Task<bool> DeleteNotificationAsync(string userId, string notificationId)
        {
            var notification = await _notificationRepository.GetByIdAsync(notificationId);
            if (notification == null || notification.UserId != userId)
            {
                return false;
            }

            notification.IsDeleted = true;
            await _notificationRepository.UpdateAsync(notification);

            if (notification.IsRead != true)
            {
                var unreadCount = await _notificationRepository.GetUnreadCountAsync(userId);
                await _notificationHub.Clients
                    .Group(NotificationHub.GetUserGroupName(userId))
                    .SendAsync("UnreadCountUpdated", new { count = unreadCount });
            }

            return true;
        }

        public async Task<NotificationDto> CreateAndSendAsync(CreateNotificationDto dto)
        {
            string targetUserId = dto.UserId;

            // Resolve valid UserId matching the user table foreign key constraint (fk_notify_user)
            var userExists = await _context.User.AnyAsync(u => u.UserId == targetUserId);
            if (!userExists)
            {
                var account = await _context.Account.FirstOrDefaultAsync(a => a.AccountId == targetUserId || a.UserId == targetUserId);
                if (account != null && !string.IsNullOrEmpty(account.UserId))
                {
                    if (await _context.User.AnyAsync(u => u.UserId == account.UserId))
                    {
                        targetUserId = account.UserId;
                        userExists = true;
                    }
                }
            }

            var notification = new Notification
            {
                NotificationId = $"notif_{DateTime.UtcNow:yyyyMMdd}_{Guid.NewGuid():N}",
                UserId = targetUserId,
                Title = dto.Title,
                Message = dto.Message,
                Type = dto.Type,
                ReferenceId = dto.ReferenceId,
                IsRead = false,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow
            };

            if (userExists)
            {
                await _notificationRepository.AddAsync(notification);
            }

            var notificationDto = ToDto(notification);

            // Send SignalR real-time notification to all possible user group names
            var hubGroups = new List<string> { NotificationHub.GetUserGroupName(dto.UserId), NotificationHub.GetUserGroupName(targetUserId) };
            foreach (var group in hubGroups.Distinct())
            {
                await _notificationHub.Clients.Group(group).SendAsync("ReceiveNotification", notificationDto);
            }

            if (userExists)
            {
                var unreadCount = await _notificationRepository.GetUnreadCountAsync(targetUserId);
                foreach (var group in hubGroups.Distinct())
                {
                    await _notificationHub.Clients.Group(group).SendAsync("UnreadCountUpdated", new { count = unreadCount });
                }
            }

            return notificationDto;
        }

        private static NotificationDto ToDto(Notification n)
        {
            return new NotificationDto
            {
                NotificationId = n.NotificationId,
                UserId = n.UserId,
                Title = n.Title,
                Message = n.Message,
                Type = n.Type,
                ReferenceId = n.ReferenceId,
                IsRead = n.IsRead,
                ReadAt = n.ReadAt,
                CreatedAt = n.CreatedAt
            };
        }
        public async Task NotifyAdminsAsync(string title, string message, string type, string? referenceId = null)
        {
            try
            {
                var adminAccounts = await _context.Account
                    .Where(a => a.AccountRole.Any(ar => ar.Role != null && ar.Role.Name == "Admin"))
                    .Select(a => new { a.UserId, a.AccountId })
                    .ToListAsync();

                var adminUserIds = adminAccounts
                    .Select(a => !string.IsNullOrEmpty(a.UserId) ? a.UserId : a.AccountId)
                    .Distinct()
                    .ToList();

                foreach (var adminId in adminUserIds)
                {
                    await CreateAndSendAsync(new CreateNotificationDto
                    {
                        UserId = adminId,
                        Title = title,
                        Message = message,
                        Type = type,
                        ReferenceId = referenceId
                    });
                }

                await _notificationHub.Clients.Group("admin-notifications").SendAsync("ReceiveNotification", new NotificationDto
                {
                    NotificationId = "NOTIF" + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff"),
                    Title = title,
                    Message = message,
                    Type = type,
                    ReferenceId = referenceId,
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false
                });
            }
            catch
            {
                // Ignore errors so notification failure doesn't block the main flow
            }
        }

        public async Task BroadcastProductUnlistedAsync(string productId)
        {
            try
            {
                await _notificationHub.Clients.All.SendAsync("ProductUnlisted", new { productId, status = "Removed" });
            }
            catch { }
        }

        public async Task BroadcastProductRestoredAsync(string productId, string status)
        {
            try
            {
                await _notificationHub.Clients.All.SendAsync("ProductRestored", new { productId, status });
            }
            catch { }
        }
    }
}
