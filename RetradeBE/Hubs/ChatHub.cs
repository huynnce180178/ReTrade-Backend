using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using RetradeBE.Data;

namespace RetradeBE.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly AppDbContext _context;

        public ChatHub(AppDbContext context)
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

        public async Task JoinRoom(string roomId)
        {
            if (!string.IsNullOrWhiteSpace(roomId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, GetRoomGroupName(roomId));
            }
        }

        public async Task LeaveRoom(string roomId)
        {
            if (!string.IsNullOrWhiteSpace(roomId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, GetRoomGroupName(roomId));
            }
        }

        public async Task JoinUserNotifications()
        {
            var userId = await GetCurrentUserIdAsync();
            if (!string.IsNullOrWhiteSpace(userId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, GetUserGroupName(userId));
            }
        }

        public static string GetRoomGroupName(string roomId) => $"chat-room-{roomId}";

        public static string GetUserGroupName(string userId) => $"chat-user-{userId}";

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
