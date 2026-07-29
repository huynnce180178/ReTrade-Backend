using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetradeBE.Models.Enums;
using RetradeBE.Services.AdminDashboard;
using System.Threading.Tasks;

namespace RetradeBE.Controllers.Admin
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = nameof(RoleEnum.Admin))]
    public class AdminDashboardController : ControllerBase
    {
        private readonly IAdminDashboardService _dashboardService;

        public AdminDashboardController(IAdminDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        [HttpGet("subscription-statistics")]
        public async Task<IActionResult> GetSubscriptionStatistics()
        {
            var statistics = await _dashboardService.GetSubscriptionStatisticsAsync();
            return Ok(statistics);
        }
    }
}
