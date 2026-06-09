using Microsoft.AspNetCore.SignalR;

namespace RetradeBE.Hubs
{
    public class AccountHub : Hub
    {
        public Task JoinAccountGroup(string accountId)
        {
            return Groups.AddToGroupAsync(Context.ConnectionId, GetAccountGroupName(accountId));
        }

        public Task LeaveAccountGroup(string accountId)
        {
            return Groups.RemoveFromGroupAsync(Context.ConnectionId, GetAccountGroupName(accountId));
        }

        public static string GetAccountGroupName(string accountId) => $"account-{accountId}";
    }
}