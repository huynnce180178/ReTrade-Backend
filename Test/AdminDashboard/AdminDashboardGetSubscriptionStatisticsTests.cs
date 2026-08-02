using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using RetradeBE.Models;
using RetradeBE.Models.DTOs.Admin;
using RetradeBE.Repositories;
using RetradeBE.Services.AdminDashboard;
using Xunit;

namespace Test.AdminDashboardTests
{
    public class AdminDashboardGetSubscriptionStatisticsTests
    {
        private readonly Mock<IMyServiceRepository> _myServiceRepo;
        private readonly Mock<IServiceSubscriptionRepository> _serviceSubscriptionRepo;
        private readonly AdminDashboardService _service;

        public AdminDashboardGetSubscriptionStatisticsTests()
        {
            _myServiceRepo = new Mock<IMyServiceRepository>();
            _serviceSubscriptionRepo = new Mock<IServiceSubscriptionRepository>();

            _service = new AdminDashboardService(
                _myServiceRepo.Object,
                _serviceSubscriptionRepo.Object
            );
        }

        #region Normal Tests (N)

        [Fact]
        public async Task GetSubscriptionStatisticsAsync_ShouldReturnCorrectStatistics_WhenDataExists()
        {
            // Arrange
            var now = DateTime.UtcNow;

            var service1 = new ServiceSubscription { ServiceId = "svc_1", Name = "Basic Package", Price = 100000 };
            var service2 = new ServiceSubscription { ServiceId = "svc_2", Name = "Pro Package", Price = 300000 };

            var myServices = new List<MyService>
            {
                new MyService { UserSubId = "ms_1", ServiceId = "svc_1", Service = service1, Status = "Active", CreatedAt = now },
                new MyService { UserSubId = "ms_2", ServiceId = "svc_1", Service = service1, Status = "Expired", CreatedAt = now.AddMonths(-1) },
                new MyService { UserSubId = "ms_3", ServiceId = "svc_2", Service = service2, Status = "Active", CreatedAt = now.AddMonths(-2) }
            };

            var allServices = new List<ServiceSubscription> { service1, service2 };

            _myServiceRepo.Setup(x => x.Query()).Returns(myServices.AsAsyncQueryable());
            _serviceSubscriptionRepo.Setup(x => x.Query()).Returns(allServices.AsAsyncQueryable());

            // Act
            var result = await _service.GetSubscriptionStatisticsAsync();

            // Assert
            result.Should().NotBeNull();
            result.TotalSubscribers.Should().Be(3);
            result.ActiveSubscribers.Should().Be(2);
            result.TotalRevenue.Should().Be(500000);

            result.PackageBreakdown.Should().HaveCount(2);
            result.PackageBreakdown[0].ServiceId.Should().Be("svc_2"); // Sorted by Revenue descending (300k vs 200k)
            result.PackageBreakdown[0].SubscriberCount.Should().Be(1);
            result.PackageBreakdown[0].Revenue.Should().Be(300000);

            result.PackageBreakdown[1].ServiceId.Should().Be("svc_1");
            result.PackageBreakdown[1].SubscriberCount.Should().Be(2);
            result.PackageBreakdown[1].Revenue.Should().Be(200000);

            result.MonthlyRevenue.Should().HaveCount(6);
        }

        [Fact]
        public async Task GetSubscriptionStatisticsAsync_ShouldIncludeServicesWithZeroSubscribers_WhenServiceSubscriptionExistsWithoutMyService()
        {
            // Arrange
            var serviceActive = new ServiceSubscription { ServiceId = "svc_active", Name = "Active Package", Price = 200000 };
            var serviceEmpty = new ServiceSubscription { ServiceId = "svc_empty", Name = "Enterprise Package", Price = 1000000 };

            var myServices = new List<MyService>
            {
                new MyService { UserSubId = "ms_1", ServiceId = "svc_active", Service = serviceActive, Status = "Active", CreatedAt = DateTime.UtcNow }
            };

            var allServices = new List<ServiceSubscription> { serviceActive, serviceEmpty };

            _myServiceRepo.Setup(x => x.Query()).Returns(myServices.AsAsyncQueryable());
            _serviceSubscriptionRepo.Setup(x => x.Query()).Returns(allServices.AsAsyncQueryable());

            // Act
            var result = await _service.GetSubscriptionStatisticsAsync();

            // Assert
            result.Should().NotBeNull();
            result.PackageBreakdown.Should().HaveCount(2);

            var emptyStat = result.PackageBreakdown.FirstOrDefault(p => p.ServiceId == "svc_empty");
            emptyStat.Should().NotBeNull();
            emptyStat!.ServiceName.Should().Be("Enterprise Package");
            emptyStat.SubscriberCount.Should().Be(0);
            emptyStat.Revenue.Should().Be(0);
        }

        [Fact]
        public async Task GetSubscriptionStatisticsAsync_ShouldCalculateMonthlyRevenueCorrectly_ForPastSixMonths()
        {
            // Arrange
            var now = DateTime.UtcNow;
            var service = new ServiceSubscription { ServiceId = "svc_1", Name = "Standard", Price = 100000 };

            var myServices = new List<MyService>
            {
                new MyService { UserSubId = "ms_0", Service = service, Status = "Active", CreatedAt = now },
                new MyService { UserSubId = "ms_1", Service = service, Status = "Active", CreatedAt = now.AddMonths(-1) },
                new MyService { UserSubId = "ms_2", Service = service, Status = "Active", CreatedAt = now.AddMonths(-1) }
            };

            _myServiceRepo.Setup(x => x.Query()).Returns(myServices.AsAsyncQueryable());
            _serviceSubscriptionRepo.Setup(x => x.Query()).Returns(new List<ServiceSubscription> { service }.AsAsyncQueryable());

            // Act
            var result = await _service.GetSubscriptionStatisticsAsync();

            // Assert
            result.Should().NotBeNull();
            result.MonthlyRevenue.Should().HaveCount(6);

            var currentMonthStr = now.ToString("MMM");
            var prevMonthStr = now.AddMonths(-1).ToString("MMM");

            var currentMonthDto = result.MonthlyRevenue.FirstOrDefault(m => m.Month == currentMonthStr);
            currentMonthDto.Should().NotBeNull();
            currentMonthDto!.Revenue.Should().Be(100000);

            var prevMonthDto = result.MonthlyRevenue.FirstOrDefault(m => m.Month == prevMonthStr);
            prevMonthDto.Should().NotBeNull();
            prevMonthDto!.Revenue.Should().Be(200000);
        }

        #endregion

        #region Abnormal Tests (A)

        [Fact]
        public async Task GetSubscriptionStatisticsAsync_ShouldReturnZeroes_WhenNoSubscriptionsOrMyServicesExist()
        {
            // Arrange
            _myServiceRepo.Setup(x => x.Query()).Returns(new List<MyService>().AsAsyncQueryable());
            _serviceSubscriptionRepo.Setup(x => x.Query()).Returns(new List<ServiceSubscription>().AsAsyncQueryable());

            // Act
            var result = await _service.GetSubscriptionStatisticsAsync();

            // Assert
            result.Should().NotBeNull();
            result.TotalSubscribers.Should().Be(0);
            result.ActiveSubscribers.Should().Be(0);
            result.TotalRevenue.Should().Be(0);
            result.PackageBreakdown.Should().BeEmpty();
            result.MonthlyRevenue.Should().HaveCount(6);
            result.MonthlyRevenue.All(m => m.Revenue == 0).Should().BeTrue();
        }

        [Fact]
        public async Task GetSubscriptionStatisticsAsync_ShouldReturnZeroSubscribersAndRevenue_WhenOnlyServiceSubscriptionsExistWithoutMyServices()
        {
            // Arrange
            var allServices = new List<ServiceSubscription>
            {
                new ServiceSubscription { ServiceId = "s1", Name = "Package 1", Price = 50000 },
                new ServiceSubscription { ServiceId = "s2", Name = "Package 2", Price = 150000 }
            };

            _myServiceRepo.Setup(x => x.Query()).Returns(new List<MyService>().AsAsyncQueryable());
            _serviceSubscriptionRepo.Setup(x => x.Query()).Returns(allServices.AsAsyncQueryable());

            // Act
            var result = await _service.GetSubscriptionStatisticsAsync();

            // Assert
            result.Should().NotBeNull();
            result.TotalSubscribers.Should().Be(0);
            result.ActiveSubscribers.Should().Be(0);
            result.TotalRevenue.Should().Be(0);
            result.PackageBreakdown.Should().HaveCount(2);
            result.PackageBreakdown.All(p => p.SubscriberCount == 0 && p.Revenue == 0).Should().BeTrue();
        }

        #endregion

        #region Boundary Tests (B)

        [Fact]
        public async Task GetSubscriptionStatisticsAsync_ShouldOnlyCountStatusActiveAsActiveSubscribers_IgnoringCaseAndOtherStatuses()
        {
            // Arrange
            var service = new ServiceSubscription { ServiceId = "svc_1", Name = "Test Svc", Price = 100000 };

            var myServices = new List<MyService>
            {
                new MyService { UserSubId = "m1", Service = service, Status = "Active" },
                new MyService { UserSubId = "m2", Service = service, Status = "active" }, // Case sensitive check in code
                new MyService { UserSubId = "m3", Service = service, Status = "Expired" },
                new MyService { UserSubId = "m4", Service = service, Status = "Cancelled" },
                new MyService { UserSubId = "m5", Service = service, Status = null },
                new MyService { UserSubId = "m6", Service = service, Status = "" }
            };

            _myServiceRepo.Setup(x => x.Query()).Returns(myServices.AsAsyncQueryable());
            _serviceSubscriptionRepo.Setup(x => x.Query()).Returns(new List<ServiceSubscription> { service }.AsAsyncQueryable());

            // Act
            var result = await _service.GetSubscriptionStatisticsAsync();

            // Assert
            result.Should().NotBeNull();
            result.TotalSubscribers.Should().Be(6);
            result.ActiveSubscribers.Should().Be(1); // Only "Active" strictly matches
        }

        [Fact]
        public async Task GetSubscriptionStatisticsAsync_ShouldHandleNullServiceOrNullPrice_WithoutThrowingException()
        {
            // Arrange
            var serviceWithNullPrice = new ServiceSubscription { ServiceId = "svc_null_price", Name = "Free tier", Price = null };

            var myServices = new List<MyService>
            {
                new MyService { UserSubId = "m1", ServiceId = "svc_null_prod", Service = null, Status = "Active", CreatedAt = DateTime.UtcNow },
                new MyService { UserSubId = "m2", ServiceId = "svc_null_price", Service = serviceWithNullPrice, Status = "Active", CreatedAt = DateTime.UtcNow }
            };

            _myServiceRepo.Setup(x => x.Query()).Returns(myServices.AsAsyncQueryable());
            _serviceSubscriptionRepo.Setup(x => x.Query()).Returns(new List<ServiceSubscription> { serviceWithNullPrice }.AsAsyncQueryable());

            // Act
            var result = await _service.GetSubscriptionStatisticsAsync();

            // Assert
            result.Should().NotBeNull();
            result.TotalSubscribers.Should().Be(2);
            result.ActiveSubscribers.Should().Be(2);
            result.TotalRevenue.Should().Be(0); // Price null treated as 0
        }

        [Fact]
        public async Task GetSubscriptionStatisticsAsync_ShouldIgnoreMyServicesCreatedOutsideLastSixMonths_InMonthlyRevenue()
        {
            // Arrange
            var now = DateTime.UtcNow;
            var service = new ServiceSubscription { ServiceId = "svc_1", Price = 500000 };

            var myServices = new List<MyService>
            {
                new MyService { UserSubId = "m_old", Service = service, Status = "Active", CreatedAt = now.AddMonths(-7) }
            };

            _myServiceRepo.Setup(x => x.Query()).Returns(myServices.AsAsyncQueryable());
            _serviceSubscriptionRepo.Setup(x => x.Query()).Returns(new List<ServiceSubscription> { service }.AsAsyncQueryable());

            // Act
            var result = await _service.GetSubscriptionStatisticsAsync();

            // Assert
            result.Should().NotBeNull();
            result.TotalSubscribers.Should().Be(1);
            result.TotalRevenue.Should().Be(500000);
            result.MonthlyRevenue.Should().HaveCount(6);
            result.MonthlyRevenue.All(m => m.Revenue == 0).Should().BeTrue();
        }

        #endregion
    }
}
