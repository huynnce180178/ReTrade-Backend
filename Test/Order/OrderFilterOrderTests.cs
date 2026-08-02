using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Moq;
using RetradeBE.Hubs;
using RetradeBE.Models;
using RetradeBE.Models.DTOs;
using RetradeBE.Repositories;
using RetradeBE.Services;
using Xunit;

namespace Test.OrderTests
{
    public class OrderFilterOrderTests
    {
        private readonly Mock<IOrderRepository> _orderRepository;
        private readonly Mock<IHubContext<OrderHub>> _orderHub;
        private readonly Mock<INotificationService> _notificationService;
        private readonly OrderService _service;

        public OrderFilterOrderTests()
        {
            _orderRepository = new Mock<IOrderRepository>();
            _orderHub = new Mock<IHubContext<OrderHub>>();
            _notificationService = new Mock<INotificationService>();

            _service = new OrderService(
                _orderRepository.Object,
                _orderHub.Object,
                _notificationService.Object
            );
        }

        #region Normal Tests (N)
        [Fact]
        public async Task GetAllOrdersAsync_ShouldFilterBySearchTermStatusAndMinTotal_AndSortAndPaginateCorrectly()
        {
            // Arrange
            var now = DateTime.UtcNow;
            var product1 = new Product { ProductId = "p1", Name = "Gaming Laptop" };
            var product2 = new Product { ProductId = "p2", Name = "Office Mouse" };

            var orders = new List<Order>
            {
                new Order
                {
                    OrderId = "o1",
                    OrderCode = "ORD-001",
                    Status = "Confirmed",
                    FinalAmount = 1500,
                    Product = product1,
                    CreatedAt = now.AddHours(-3)
                },
                new Order
                {
                    OrderId = "o2",
                    OrderCode = "ORD-002",
                    Status = "Confirmed",
                    FinalAmount = 2000,
                    Product = product1,
                    CreatedAt = now.AddHours(-1)
                },
                new Order
                {
                    OrderId = "o3",
                    OrderCode = "ORD-003",
                    Status = "Confirmed",
                    FinalAmount = 800, // < MinTotal (1000)
                    Product = product1,
                    CreatedAt = now
                },
                new Order
                {
                    OrderId = "o4",
                    OrderCode = "ORD-004",
                    Status = "Pending", // != Status (Confirmed)
                    FinalAmount = 1800,
                    Product = product2,
                    CreatedAt = now
                }
            };

            _orderRepository.Setup(r => r.Query()).Returns(orders.AsAsyncQueryable());

            var query = new OrderSearchQueryDto
            {
                SearchTerm = "Laptop",
                Status = "Confirmed",
                MinTotal = 1000,
                SortBy = "total_desc", // finalamount desc
                Page = 1,
                PageSize = 10
            };

            // Act
            var result = await _service.GetAllOrdersAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.TotalItems.Should().Be(2);
            result.Items.Should().HaveCount(2);
            result.Items[0].OrderId.Should().Be("o2"); // 2000 > 1500
            result.Items[1].OrderId.Should().Be("o1");
            result.Items[0].ProductName.Should().Be("Gaming Laptop");

            _orderRepository.Verify(r => r.Query(), Times.Once);
        }
        #endregion

        #region Abnormal Tests (A)
        [Fact]
        public async Task GetAllOrdersAsync_ShouldReturnEmptyPagedResult_WhenNoOrdersMatchFilters()
        {
            // Arrange
            var orders = new List<Order>
            {
                new Order { OrderId = "o1", Status = "Pending", FinalAmount = 500 }
            };

            _orderRepository.Setup(r => r.Query()).Returns(orders.AsAsyncQueryable());

            var query = new OrderSearchQueryDto
            {
                SearchTerm = "NonExistentKeyword",
                Status = "Delivered",
                MinTotal = 5000
            };

            // Act
            var result = await _service.GetAllOrdersAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.TotalItems.Should().Be(0);
            result.TotalPages.Should().Be(1);
            result.Items.Should().BeEmpty();

            _orderRepository.Verify(r => r.Query(), Times.Once);
        }
        #endregion

        #region Boundary Tests (B)
        [Fact]
        public async Task GetAllOrdersAsync_ShouldFilterByDateRangeWithTimeOfDayBoundaries_AndNormalizePagination()
        {
            // Arrange
            var todayZero = DateTime.UtcNow.Date;
            var buyer = new User { Email = "buyer@test.com", Phone = "0987654321", FirstName = "Nguyen", LastName = "Van A" };

            var orders = new List<Order>
            {
                new Order { OrderId = "o1", TrackingCode = "GHN_TRK001", CreatedAt = todayZero.AddHours(2) },
                new Order { OrderId = "o2", Buyer = buyer, CreatedAt = todayZero.AddDays(1).AddHours(5) }, // Out of date range
                new Order { OrderId = "o3", CreatedAt = todayZero.AddDays(-2) } // Before FromDate
            };

            _orderRepository.Setup(r => r.Query()).Returns(orders.AsAsyncQueryable());

            // Scenario 1: Date range boundary (ToDate với TimeOfDay == 0) & SearchTerm khớp với TrackingCode
            var queryDateBoundary = new OrderSearchQueryDto
            {
                FromDate = todayZero.AddDays(-1),
                ToDate = todayZero, // TimeOfDay == 0 -> exclusiveToDate = todayZero.AddDays(1)
                SearchTerm = "TRK001"
            };

            var resultDate = await _service.GetAllOrdersAsync(queryDateBoundary);
            resultDate.TotalItems.Should().Be(1);
            resultDate.Items[0].OrderId.Should().Be("o1");

            // Scenario 2: Chuẩn hóa phân trang (Page < 1 -> 1, PageSize > 100 -> 100) & SearchTerm khớp tên Buyer
            var queryPageNormalization = new OrderSearchQueryDto
            {
                Page = -5,
                PageSize = 500,
                SearchTerm = "Nguyen Van A"
            };

            var resultPage = await _service.GetAllOrdersAsync(queryPageNormalization);
            resultPage.Page.Should().Be(1);
            resultPage.PageSize.Should().Be(100);
            resultPage.TotalItems.Should().Be(1);
            resultPage.Items[0].OrderId.Should().Be("o2");
        }

        [Fact]
        public async Task GetAllOrdersAsync_ShouldHandleAllSortingOptions_Correctly()
        {
            // Arrange
            var now = DateTime.UtcNow;
            var orders = new List<Order>
            {
                new Order { OrderId = "o1", FinalAmount = 100, CreatedAt = now.AddHours(-10) },
                new Order { OrderId = "o2", FinalAmount = 500, CreatedAt = now.AddHours(-1) }
            };

            _orderRepository.Setup(r => r.Query()).Returns(orders.AsAsyncQueryable());

            // Scenario 1: "oldest" / "createdat asc"
            var resultOldest = await _service.GetAllOrdersAsync(new OrderSearchQueryDto { SortBy = "oldest" });
            resultOldest.Items[0].OrderId.Should().Be("o1");

            // Scenario 2: "total_asc" / "finalamount asc"
            var resultTotalAsc = await _service.GetAllOrdersAsync(new OrderSearchQueryDto { SortBy = "total_asc" });
            resultTotalAsc.Items[0].OrderId.Should().Be("o1");
        }
        #endregion
    }
}
