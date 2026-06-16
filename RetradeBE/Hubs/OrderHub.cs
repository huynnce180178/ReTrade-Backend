using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using RetradeBE.Models.Enums;
using RetradeBE.Repositories;
using System.Security.Claims;

namespace RetradeBE.Hubs
{
    [Authorize]
    public class OrderHub : Hub
    {
        private readonly IAccountRepository _accountRepository;

        public OrderHub(IAccountRepository accountRepository)
        {
            _accountRepository = accountRepository;
        }

        public async Task JoinSellerOrderGroup(string sellerId)
        {
            var accountId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(accountId))
            {
                throw new HubException("Unauthorized.");
            }

            var account = await _accountRepository.GetByIdAsync(accountId);
            var roles = await _accountRepository.GetRolesAsync(accountId);
            var isAdmin = roles.Any(role => string.Equals(role, nameof(RoleEnum.Admin), StringComparison.OrdinalIgnoreCase));

            if (!isAdmin && account?.UserId != sellerId)
            {
                throw new HubException("Forbidden.");
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, GetSellerOrderGroupName(sellerId));
        }

        public Task LeaveSellerOrderGroup(string sellerId)
        {
            return Groups.RemoveFromGroupAsync(Context.ConnectionId, GetSellerOrderGroupName(sellerId));
        }

        public static string GetSellerOrderGroupName(string sellerId) => $"seller-orders-{sellerId}";
    }
}
