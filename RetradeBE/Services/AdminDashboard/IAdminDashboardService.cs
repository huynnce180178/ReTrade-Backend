using RetradeBE.Models.DTOs.Admin;
using System.Threading.Tasks;

namespace RetradeBE.Services.AdminDashboard
{
    public interface IAdminDashboardService
    {
        Task<SubscriptionStatisticsDto> GetSubscriptionStatisticsAsync();
    }
}
