using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using RetradeBE.Models.Enums;
using RetradeBE.Repositories;
using System.Security.Claims;

namespace RetradeBE.Hubs
{
    [Authorize]
    public class AuctionHub : Hub
    {
        private readonly IAccountRepository _accountRepository;

        public AuctionHub(IAccountRepository accountRepository)
        {
            _accountRepository = accountRepository;
        }

        public Task JoinAuctionList()
        {
            return Groups.AddToGroupAsync(Context.ConnectionId, AuctionListGroupName);
        }

        public Task LeaveAuctionList()
        {
            return Groups.RemoveFromGroupAsync(Context.ConnectionId, AuctionListGroupName);
        }

        public Task JoinAuctionGroup(string auctionId)
        {
            if (string.IsNullOrWhiteSpace(auctionId))
            {
                throw new HubException("AuctionId is required.");
            }

            return Groups.AddToGroupAsync(Context.ConnectionId, GetAuctionGroupName(auctionId));
        }

        public Task LeaveAuctionGroup(string auctionId)
        {
            return Groups.RemoveFromGroupAsync(Context.ConnectionId, GetAuctionGroupName(auctionId));
        }

        public async Task JoinMySellerAuctionGroup()
        {
            var accountId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(accountId))
            {
                throw new HubException("Unauthorized.");
            }

            var account = await _accountRepository.GetByIdAsync(accountId);
            if (account == null || string.IsNullOrWhiteSpace(account.UserId))
            {
                throw new HubException("Account is not linked to a user.");
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, GetSellerAuctionGroupName(account.UserId));
        }

        public async Task JoinSellerAuctionGroup(string sellerId)
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

            await Groups.AddToGroupAsync(Context.ConnectionId, GetSellerAuctionGroupName(sellerId));
        }

        public async Task JoinMyAuctionDepositGroup(string auctionId)
        {
            var accountId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(accountId))
            {
                throw new HubException("Unauthorized.");
            }

            var account = await _accountRepository.GetByIdAsync(accountId);
            if (account == null || string.IsNullOrWhiteSpace(account.UserId))
            {
                throw new HubException("Account is not linked to a user.");
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, GetAuctionUserGroupName(auctionId, account.UserId));
        }

        public const string AuctionListGroupName = "auctions-live";
        public static string GetAuctionGroupName(string auctionId) => $"auction-{auctionId}";
        public static string GetSellerAuctionGroupName(string sellerId) => $"seller-auctions-{sellerId}";
        public static string GetAuctionUserGroupName(string auctionId, string userId) => $"auction-{auctionId}-user-{userId}";
    }
}
