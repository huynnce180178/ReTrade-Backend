using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Moq;
using RetradeBE.Hubs;
using RetradeBE.Models;
using RetradeBE.Repositories;
using RetradeBE.Services;
using Xunit;

namespace Test.OrderTests
{
    public class OrderGetSellerSalesStatisticsTests
    {
        private readonly Mock<IOrderRepository> _orderRepository;
        private readonly Mock<IHubContext<OrderHub>> _orderHub;
        private readonly Mock<INotificationService> _notificationService;
        private readonly OrderService _service;

        public OrderGetSellerSalesStatisticsTests()
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
        public async Task GetSellerSalesStatisticsAsync_ShouldCalculateStatisticsAndRevenueTrendCorrectly_WhenSellerIdIsValid()
        {
            // Arrange
            var sellerId = "seller_100";
            var nowUtc = DateTime.UtcNow;

            var orders = new List<Order>
            {
                new Order
                {
                    OrderId = "o1",
                    SellerId = sellerId,
                    Status = "Delivered",
                    Quantity = 2,
                    TotalAmount = 200,
                    ShippingFee = 20,
                    DiscountAmount = 10,
                    FinalAmount = 210,
                    CreatedAt = nowUtc.AddHours(-1)
                },
                new Order
                {
                    OrderId = "o2",
                    SellerId = sellerId,
                    Status = "Completed",
                    Quantity = 1,
                    TotalAmount = 100,
                    ShippingFee = 10,
                    DiscountAmount = 0,
                    FinalAmount = 110,
                    CreatedAt = nowUtc.AddHours(-2)
                },
                new Order
                {
                    OrderId = "o3",
                    SellerId = sellerId,
                    Status = "Pending",
                    Quantity = 5,
                    TotalAmount = 500,
                    FinalAmount = 500,
                    CreatedAt = nowUtc.AddHours(-3)
                },
                new Order
                {
                    OrderId = "o4",
                    SellerId = sellerId,
                    Status = "Cancelled",
                    Quantity = 1,
                    TotalAmount = 50,
                    FinalAmount = 50,
                    CreatedAt = nowUtc.AddHours(-4)
                },
                new Order
                {
                    OrderId = "o_other_seller",
                    SellerId = "other_seller",
                    Status = "Delivered",
                    Quantity = 10,
                    FinalAmount = 1000,
                    CreatedAt = nowUtc.AddHours(-1)
                }
            };

            _orderRepository
                .Setup(r => r.Query())
                .Returns(orders.AsAsyncQueryable());

            // Act
            var result = await _service.GetSellerSalesStatisticsAsync(sellerId, 30);

            // Assert
            result.Should().NotBeNull();
            result.PeriodDays.Should().Be(30);
            result.TotalOrders.Should().Be(4);
            result.DeliveredOrders.Should().Be(1);
            result.CompletedOrders.Should().Be(1);
            result.PendingOrders.Should().Be(1);
            result.CancelledOrders.Should().Be(1);

            result.SoldItems.Should().Be(3);
            result.GrossSales.Should().Be(300);
            result.ShippingCollected.Should().Be(30);
            result.DiscountGiven.Should().Be(10);
            result.NetSales.Should().Be(320);

            result.RevenueTrend.Should().NotBeNullOrEmpty();

            _orderRepository.Verify(r => r.Query(), Times.Once);
        }

        [Fact]
        public async Task GetSellerSalesStatisticsAsync_ShouldReturnEmptyStatistics_WhenSellerHasNoOrders()
        {
            // Arrange
            var sellerId = "seller_100";

            var orders = new List<Order>();

            _orderRepository
                .Setup(r => r.Query())
                .Returns(orders.AsAsyncQueryable());

            // Act
            var result = await _service.GetSellerSalesStatisticsAsync(sellerId, 30);

            // Assert
            result.Should().NotBeNull();
            result.PeriodDays.Should().Be(30);
            result.TotalOrders.Should().Be(0);
            result.DeliveredOrders.Should().Be(0);
            result.CompletedOrders.Should().Be(0);
            result.PendingOrders.Should().Be(0);
            result.CancelledOrders.Should().Be(0);

            result.SoldItems.Should().Be(0);
            result.GrossSales.Should().Be(0);
            result.ShippingCollected.Should().Be(0);
            result.DiscountGiven.Should().Be(0);
            result.NetSales.Should().Be(0);

            _orderRepository.Verify(r => r.Query(), Times.Once);
        }

        #endregion

        #region Abnormal Tests (A)

        [Fact]
        public async Task GetSellerSalesStatisticsAsync_ShouldReturnEmptyStatistics_WhenSellerIdIsNullOrEmpty()
        {
            // Scenario 1: sellerId = null
            var resultNull =
                await _service.GetSellerSalesStatisticsAsync(null!, 30);

            resultNull.Should().NotBeNull();
            resultNull.PeriodDays.Should().Be(30);
            resultNull.TotalOrders.Should().Be(0);

            // Scenario 2: sellerId = whitespace
            var resultEmpty =
                await _service.GetSellerSalesStatisticsAsync("   ", 30);

            resultEmpty.Should().NotBeNull();
            resultEmpty.PeriodDays.Should().Be(30);
            resultEmpty.TotalOrders.Should().Be(0);

            _orderRepository.Verify(r => r.Query(), Times.Never);
        }

        #endregion

        #region Boundary Tests (B)

        [Fact]
        public async Task GetSellerSalesStatisticsAsync_ShouldClampPeriodDaysAndFilterDateRange_Correctly()
        {
            // Arrange
            _orderRepository
                .Setup(r => r.Query())
                .Returns(new List<Order>().AsAsyncQueryable());

            // Scenario 1: periodDays < 7
            var resultUnderMin =
                await _service.GetSellerSalesStatisticsAsync("seller_100", 2);

            resultUnderMin.PeriodDays.Should().Be(7);

            // Scenario 2: periodDays > 365
            var resultOverMax =
                await _service.GetSellerSalesStatisticsAsync("seller_100", 1000);

            resultOverMax.PeriodDays.Should().Be(365);

            _orderRepository.Verify(r => r.Query(), Times.Exactly(2));
        }

        [Fact]
        public async Task GetSellerSalesStatisticsAsync_ShouldOnlyIncludeOrdersWithinSelectedPeriod()
        {
            // Arrange
            var sellerId = "seller_100";
            var nowUtc = DateTime.UtcNow;

            var orders = new List<Order>
            {
                // Inside 30-day period
                new Order
                {
                    OrderId = "order_inside_period",
                    SellerId = sellerId,
                    Status = "Delivered",
                    Quantity = 2,
                    TotalAmount = 200,
                    ShippingFee = 20,
                    DiscountAmount = 10,
                    FinalAmount = 210,
                    CreatedAt = nowUtc.AddDays(-5)
                },

                // Outside 30-day period
                new Order
                {
                    OrderId = "order_outside_period",
                    SellerId = sellerId,
                    Status = "Completed",
                    Quantity = 5,
                    TotalAmount = 500,
                    ShippingFee = 50,
                    DiscountAmount = 0,
                    FinalAmount = 550,
                    CreatedAt = nowUtc.AddDays(-40)
                },

                // Inside period but belongs to another seller
                new Order
                {
                    OrderId = "order_other_seller",
                    SellerId = "other_seller",
                    Status = "Delivered",
                    Quantity = 10,
                    TotalAmount = 1000,
                    FinalAmount = 1000,
                    CreatedAt = nowUtc.AddDays(-2)
                }
            };

            _orderRepository
                .Setup(r => r.Query())
                .Returns(orders.AsAsyncQueryable());

            // Act
            var result =
                await _service.GetSellerSalesStatisticsAsync(sellerId, 30);

            // Assert
            result.Should().NotBeNull();
            result.PeriodDays.Should().Be(30);

            // Only order_inside_period is counted
            result.TotalOrders.Should().Be(1);
            result.DeliveredOrders.Should().Be(1);
            result.CompletedOrders.Should().Be(0);

            result.SoldItems.Should().Be(2);
            result.GrossSales.Should().Be(200);
            result.ShippingCollected.Should().Be(20);
            result.DiscountGiven.Should().Be(10);
            result.NetSales.Should().Be(210);

            _orderRepository.Verify(r => r.Query(), Times.Once);
        }

        #endregion
    }
}
