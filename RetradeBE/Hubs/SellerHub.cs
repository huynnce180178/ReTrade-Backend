using Microsoft.AspNetCore.SignalR;

namespace RetradeBE.Hubs
{
    public class SellerHub : Hub
    {
        public Task JoinSellerGroup(string sellerId)
        {
            return Groups.AddToGroupAsync(Context.ConnectionId, GetSellerGroupName(sellerId));
        }

        public Task LeaveSellerGroup(string sellerId)
        {
            return Groups.RemoveFromGroupAsync(Context.ConnectionId, GetSellerGroupName(sellerId));
        }

        public static string GetSellerGroupName(string sellerId) => $"seller-{sellerId}";
    }
}
