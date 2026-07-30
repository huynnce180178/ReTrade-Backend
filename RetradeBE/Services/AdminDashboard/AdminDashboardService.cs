using Microsoft.EntityFrameworkCore;
using RetradeBE.Models.DTOs.Admin;
using RetradeBE.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RetradeBE.Services.AdminDashboard
{
    public class AdminDashboardService : IAdminDashboardService
    {
        private readonly IMyServiceRepository _myServiceRepo;
        private readonly IServiceSubscriptionRepository _serviceSubscriptionRepo;

        public AdminDashboardService(IMyServiceRepository myServiceRepo, IServiceSubscriptionRepository serviceSubscriptionRepo)
        {
            _myServiceRepo = myServiceRepo;
            _serviceSubscriptionRepo = serviceSubscriptionRepo;
        }

        public async Task<SubscriptionStatisticsDto> GetSubscriptionStatisticsAsync()
        {
            var myServices = await _myServiceRepo.Query()
                .Include(m => m.Service)
                .AsNoTracking()
                .ToListAsync();

            var totalSubscribers = myServices.Count;
            var activeSubscribers = myServices.Count(m => m.Status == "Active");
            var totalRevenue = myServices.Sum(m => m.Service?.Price ?? 0);

            var packageBreakdown = myServices
                .Where(m => m.Service != null)
                .GroupBy(m => new { m.ServiceId, m.Service!.Name })
                .Select(g => new PackageStatisticDto
                {
                    ServiceId = g.Key.ServiceId ?? string.Empty,
                    ServiceName = g.Key.Name ?? string.Empty,
                    SubscriberCount = g.Count(),
                    Revenue = g.Sum(m => m.Service?.Price ?? 0)
                })
                .ToList();

            // Also include services that have 0 subscribers to give a complete picture
            var allServices = await _serviceSubscriptionRepo.Query().AsNoTracking().ToListAsync();
            foreach (var svc in allServices)
            {
                if (!packageBreakdown.Any(p => p.ServiceId == svc.ServiceId))
                {
                    packageBreakdown.Add(new PackageStatisticDto
                    {
                        ServiceId = svc.ServiceId,
                        ServiceName = svc.Name ?? string.Empty,
                        SubscriberCount = 0,
                        Revenue = 0
                    });
                }
            }

            var monthlyRevenue = new List<MonthlyRevenueDto>();
            var now = DateTime.UtcNow;
            for (int i = 5; i >= 0; i--)
            {
                var targetMonth = now.AddMonths(-i);
                var monthStr = targetMonth.ToString("MMM");
                
                var rev = myServices
                    .Where(m => m.CreatedAt.HasValue && m.CreatedAt.Value.Year == targetMonth.Year && m.CreatedAt.Value.Month == targetMonth.Month)
                    .Sum(m => m.Service?.Price ?? 0);
                    
                monthlyRevenue.Add(new MonthlyRevenueDto
                {
                    Month = monthStr,
                    Revenue = rev
                });
            }

            return new SubscriptionStatisticsDto
            {
                TotalSubscribers = totalSubscribers,
                ActiveSubscribers = activeSubscribers,
                TotalRevenue = totalRevenue,
                PackageBreakdown = packageBreakdown.OrderByDescending(p => p.Revenue).ToList(),
                MonthlyRevenue = monthlyRevenue
            };
        }
    }
}
