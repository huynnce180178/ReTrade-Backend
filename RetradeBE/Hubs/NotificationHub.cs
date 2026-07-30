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

            await base.OnConnectedAsync();
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

            return await _context.Account
                .AsNoTracking()
                .Where(a => a.AccountId == accountId)
                .Select(a => a.UserId)
                .FirstOrDefaultAsync();
        }
    }
}
