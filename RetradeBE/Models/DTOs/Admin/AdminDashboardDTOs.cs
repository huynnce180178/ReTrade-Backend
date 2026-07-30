using System.Collections.Generic;

namespace RetradeBE.Models.DTOs.Admin
{
    public class PackageStatisticDto
    {
        public string ServiceId { get; set; } = string.Empty;
        public string ServiceName { get; set; } = string.Empty;
        public int SubscriberCount { get; set; }
        public decimal Revenue { get; set; }
    }

    public class MonthlyRevenueDto
    {
        public string Month { get; set; } = string.Empty; // e.g., "Jan", "Feb"
        public decimal Revenue { get; set; }
    }

    public class SubscriptionStatisticsDto
    {
        public int TotalSubscribers { get; set; }
        public int ActiveSubscribers { get; set; }
        public decimal TotalRevenue { get; set; }
        public List<PackageStatisticDto> PackageBreakdown { get; set; } = new List<PackageStatisticDto>();
        public List<MonthlyRevenueDto> MonthlyRevenue { get; set; } = new List<MonthlyRevenueDto>();
    }
}
