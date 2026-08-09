using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using RetradeBE.Models.Enums;
using RetradeBE.Repositories;
using System.Security.Claims;

namespace RetradeBE.Hubs
{
    [Authorize]
    public class OfferHub : Hub
    {
        private readonly IAccountRepository _accountRepository;

        public OfferHub(IAccountRepository accountRepository)
        {
            _accountRepository = accountRepository;
        }

        public async Task JoinSellerOfferGroup(string sellerId)
        {
            var accountId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(accountId))
            {
                throw new HubException("Unauthorized.");
            }

            var account = await _accountRepository.GetByIdAsync(accountId);
            var roles = await _accountRepository.GetRolesAsync(accountId);
            var isAdmin = roles.Any(role => string.Equals(role, nameof(RoleEnum.Admin), StringComparison.OrdinalIgnoreCase));

            if (!isAdmin && account?.UserId != sellerId && account?.AccountId != sellerId)
            {
                throw new HubException("Forbidden.");
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, GetSellerOfferGroupName(sellerId));
        }

        public Task LeaveSellerOfferGroup(string sellerId)
        {
            return Groups.RemoveFromGroupAsync(Context.ConnectionId, GetSellerOfferGroupName(sellerId));
        }

        public async Task JoinBuyerOfferGroup(string buyerId)
        {
            var accountId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(accountId))
            {
                throw new HubException("Unauthorized.");
            }

            var account = await _accountRepository.GetByIdAsync(accountId);
            var roles = await _accountRepository.GetRolesAsync(accountId);
            var isAdmin = roles.Any(role => string.Equals(role, nameof(RoleEnum.Admin), StringComparison.OrdinalIgnoreCase));

            if (!isAdmin && account?.UserId != buyerId && account?.AccountId != buyerId)
            {
                throw new HubException("Forbidden.");
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, GetBuyerOfferGroupName(buyerId));
        }

        public Task LeaveBuyerOfferGroup(string buyerId)
        {
            return Groups.RemoveFromGroupAsync(Context.ConnectionId, GetBuyerOfferGroupName(buyerId));
        }

        public static string GetSellerOfferGroupName(string sellerId) => $"seller-offers-{sellerId}";
        public static string GetBuyerOfferGroupName(string buyerId) => $"buyer-offers-{buyerId}";
    }
}
