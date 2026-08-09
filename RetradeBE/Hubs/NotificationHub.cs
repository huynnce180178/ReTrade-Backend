using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using RetradeBE.Data;

namespace RetradeBE.Hubs
{
    [Authorize]
    public class NotificationHub : Hub
    {
        private readonly AppDbContext _context;

        public NotificationHub(AppDbContext context)
        {
            _context = context;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = await GetCurrentUserIdAsync();
            if (!string.IsNullOrWhiteSpace(userId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, GetUserGroupName(userId));
            }

            var accountId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrWhiteSpace(accountId))
            {
                var isAdmin = await _context.AccountRole
                    .AsNoTracking()
                    .AnyAsync(ar => ar.AccountId == accountId && ar.Role != null && ar.Role.Name == "Admin");
                if (isAdmin)
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, "admin-notifications");
                }
            }

            await base.OnConnectedAsync();
        }

        public async Task JoinUserNotifications(string userId)
        {
            if (!string.IsNullOrWhiteSpace(userId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, GetUserGroupName(userId));
            }
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = await GetCurrentUserIdAsync();
            if (!string.IsNullOrWhiteSpace(userId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, GetUserGroupName(userId));
            }

            await base.OnDisconnectedAsync(exception);
        }

        public static string GetUserGroupName(string userId) => $"notification-user-{userId}";

        private async Task<string?> GetCurrentUserIdAsync()
        {
            var accountId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(accountId))
            {
                return null;
            }

            var userId = await _context.Account
                .AsNoTracking()
                .Where(a => a.AccountId == accountId)
                .Select(a => a.UserId)
                .FirstOrDefaultAsync();

            return userId ?? accountId;
        }
    }
}
