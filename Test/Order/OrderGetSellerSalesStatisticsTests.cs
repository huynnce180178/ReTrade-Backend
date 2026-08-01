using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Moq;
using RetradeBE.Hubs;
using RetradeBE.Models;
using RetradeBE.Models.DTOs;
using RetradeBE.Models.Enums;
using RetradeBE.Repositories;
using RetradeBE.Services;
using Xunit;
using Test;

namespace Test.OrderTests
{
    public class OrderGetSellerSalesStatisticsTests
    {
        private readonly Mock<IOrderRepository> _orderRepository;
        private readonly Mock<IHubContext<OrderHub>> _orderHub;
        private readonly Mock<INotificationService> _notificationService;
        private readonly OrderService _service;
        private readonly List<Order> _orders;

        public OrderGetSellerSalesStatisticsTests()
        {
            _orderRepository = new Mock<IOrderRepository>();
            _orderHub = new Mock<IHubContext<OrderHub>>();
            _notificationService = new Mock<INotificationService>();

            _orders = new List<Order>();
            _orderRepository.Setup(x => x.Query()).Returns(_orders.AsMockDbSet().Object);

            _service = new OrderService(
                _orderRepository.Object,
                _orderHub.Object,
                _notificationService.Object
            );
        }

        private static DateTime ToBusinessLocal(DateTime dateTime)
        {
            var offset = TimeSpan.FromHours(7);
            var utc = dateTime.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(dateTime, DateTimeKind.Utc)
                : dateTime.ToUniversalTime();

            return DateTime.SpecifyKind(utc.Add(offset), DateTimeKind.Unspecified);
        }

        private static DateTime FromBusinessLocal(DateTime localDateTime)
        {
            var offset = TimeSpan.FromHours(7);
            return DateTime.SpecifyKind(localDateTime.Subtract(offset), DateTimeKind.Utc);
        }

        #region Normal Tests (N)

        [Fact]
        public async Task GetSellerSalesStatisticsAsync_ShouldReturnEmptyStats_WhenSellerIdIsEmpty()
        {
            // Act
            var result = await _service.GetSellerSalesStatisticsAsync(null!, 30);

            // Assert
            result.Should().NotBeNull();
            result.TotalOrders.Should().Be(0);
            result.PeriodDays.Should().Be(30);
        }

        [Fact]
        public async Task GetSellerSalesStatisticsAsync_ShouldReturnEmptyStats_WhenNoOrdersExist()
        {
            // Act
            var result = await _service.GetSellerSalesStatisticsAsync("seller_1", 30);

            // Assert
            result.Should().NotBeNull();
            result.TotalOrders.Should().Be(0);
            result.GrossSales.Should().Be(0);
            result.NetSales.Should().Be(0);
        }

        [Fact]
        public async Task GetSellerSalesStatisticsAsync_ShouldCalculateStatistics_WhenDeliveredOrdersExist()
        {
            // Arrange
            var sellerId = "seller_1";
            var baseTimeUtc = DateTime.UtcNow;

            var order1 = new Order
            {
                OrderId = "o1",
                SellerId = sellerId,
                Status = nameof(OrderStatusEnum.Delivered),
                CreatedAt = baseTimeUtc.AddHours(-1),
                Quantity = 2,
                TotalAmount = 200,
                ShippingFee = 10,
                DiscountAmount = 20,
                FinalAmount = 190
            };

            var order2 = new Order
            {
                OrderId = "o2",
                SellerId = sellerId,
                Status = nameof(OrderStatusEnum.Completed),
                CreatedAt = baseTimeUtc.AddHours(-2),
                Quantity = 1,
                TotalAmount = 100,
                ShippingFee = 5,
                DiscountAmount = 10,
                FinalAmount = 95
            };

            _orders.Add(order1);
            _orders.Add(order2);
            _orderRepository.Setup(x => x.Query()).Returns(_orders.AsMockDbSet().Object);

            // Act
            var result = await _service.GetSellerSalesStatisticsAsync(sellerId, 30);

            // Assert
            result.Should().NotBeNull();
            result.TotalOrders.Should().Be(2);
            result.SoldItems.Should().Be(3);
            result.GrossSales.Should().Be(300);
            result.ShippingCollected.Should().Be(15);
            result.DiscountGiven.Should().Be(30);
            result.NetSales.Should().Be(285);
        }

        [Fact]
        public async Task GetSellerSalesStatisticsAsync_ShouldCalculateStatistics_WhenVariousOrderStatusExist()
        {
            // Arrange
            var sellerId = "seller_1";
            var baseTimeUtc = DateTime.UtcNow;

            var statuses = new[]
            {
                OrderStatusEnum.AwaitingPayment,
                OrderStatusEnum.Pending,
                OrderStatusEnum.Confirmed,
                OrderStatusEnum.Shipping,
                OrderStatusEnum.Delivered,
                OrderStatusEnum.Completed,
                OrderStatusEnum.ReturnRequested,
                OrderStatusEnum.ReturnRejected,
                OrderStatusEnum.DeliveryFailed,
                OrderStatusEnum.Returned,
                OrderStatusEnum.Cancelled
            };

            int id = 1;
            foreach (var status in statuses)
            {
                _orders.Add(new Order
                {
                    OrderId = $"o_{id++}",
                    SellerId = sellerId,
                    Status = status.ToString(),
                    CreatedAt = baseTimeUtc.AddHours(-1)
                });
            }
            _orderRepository.Setup(x => x.Query()).Returns(_orders.AsMockDbSet().Object);

            // Act
            var result = await _service.GetSellerSalesStatisticsAsync(sellerId, 30);

            // Assert
            result.Should().NotBeNull();
            result.TotalOrders.Should().Be(11);
            result.AwaitingPaymentOrders.Should().Be(1);
            result.PendingOrders.Should().Be(1);
            result.ConfirmedOrders.Should().Be(1);
            result.ShippingOrders.Should().Be(1);
            result.DeliveredOrders.Should().Be(1);
            result.CompletedOrders.Should().Be(1);
            result.ReturnRequestedOrders.Should().Be(1);
            result.ReturnRejectedOrders.Should().Be(1);
            result.DeliveryFailedOrders.Should().Be(1);
            result.ReturnedOrders.Should().Be(1);
            result.CancelledOrders.Should().Be(1);
        }

        [Fact]
        public async Task GetSellerSalesStatisticsAsync_ShouldClampPeriodDays_WhenPeriodDaysIsBelowMinimum()
        {
            // Act
            var result = await _service.GetSellerSalesStatisticsAsync("seller_1", 5);

            // Assert
            result.PeriodDays.Should().Be(7); // Clamped to minimum of 7
        }

        [Fact]
        public async Task GetSellerSalesStatisticsAsync_ShouldClampPeriodDays_WhenPeriodDaysIsAboveMaximum()
        {
            // Act
            var result = await _service.GetSellerSalesStatisticsAsync("seller_1", 400);

            // Assert
            result.PeriodDays.Should().Be(365); // Clamped to maximum of 365
        }

        [Fact]
        public async Task GetSellerSalesStatisticsAsync_ShouldBuildRevenueTrendCorrectly_WhenOrdersAreDistributedAcrossDays()
        {
            // Arrange
            var sellerId = "seller_1";
            var todayLocal = ToBusinessLocal(DateTime.UtcNow).Date;
            
            // Generate orders on different days
            var o1 = new Order
            {
                OrderId = "o1",
                SellerId = sellerId,
                Status = nameof(OrderStatusEnum.Completed),
                CreatedAt = FromBusinessLocal(todayLocal.AddDays(-2)), // 2 days ago
                FinalAmount = 100
            };

            var o2 = new Order
            {
                OrderId = "o2",
                SellerId = sellerId,
                Status = nameof(OrderStatusEnum.Delivered),
                CreatedAt = FromBusinessLocal(todayLocal.AddDays(-5)), // 5 days ago
                FinalAmount = 200
            };

            _orders.Add(o1);
            _orders.Add(o2);
            _orderRepository.Setup(x => x.Query()).Returns(_orders.AsMockDbSet().Object);

            // Act
            var result = await _service.GetSellerSalesStatisticsAsync(sellerId, 30);

            // Assert
            result.Should().NotBeNull();
            result.RevenueTrend.Should().HaveCount(7); // Built into 7 buckets
            result.RevenueTrend.Sum(t => t.Revenue).Should().Be(300);
        }

        #endregion

        #region Abnormal Tests (A)

        [Fact]
        public async Task GetSellerSalesStatisticsAsync_ShouldIgnoreOrdersForOtherSellers()
        {
            // Arrange
            var sellerId = "seller_1";
            var otherSellerId = "seller_other";
            var baseTimeUtc = DateTime.UtcNow;

            var orderSelf = new Order
            {
                OrderId = "o1",
                SellerId = sellerId,
                Status = nameof(OrderStatusEnum.Completed),
                CreatedAt = baseTimeUtc.AddHours(-1),
                FinalAmount = 100
            };

            var orderOther = new Order
            {
                OrderId = "o2",
                SellerId = otherSellerId,
                Status = nameof(OrderStatusEnum.Completed),
                CreatedAt = baseTimeUtc.AddHours(-1),
                FinalAmount = 200
            };

            _orders.Add(orderSelf);
            _orders.Add(orderOther);
            _orderRepository.Setup(x => x.Query()).Returns(_orders.AsMockDbSet().Object);

            // Act
            var result = await _service.GetSellerSalesStatisticsAsync(sellerId, 30);

            // Assert
            result.Should().NotBeNull();
            result.TotalOrders.Should().Be(1);
            result.NetSales.Should().Be(100); // 200 from other seller is ignored
        }

        [Fact]
        public async Task GetSellerSalesStatisticsAsync_ShouldIgnoreOrdersOutsidePeriodRange()
        {
            // Arrange
            var sellerId = "seller_1";
            var todayLocal = ToBusinessLocal(DateTime.UtcNow).Date;

            // Inside period (within 7 days)
            var orderInside = new Order
            {
                OrderId = "o_inside",
                SellerId = sellerId,
                Status = nameof(OrderStatusEnum.Completed),
                CreatedAt = FromBusinessLocal(todayLocal.AddDays(-2)),
                FinalAmount = 100
            };

            // Outside period (more than 7 days ago)
            var orderOutside = new Order
            {
                OrderId = "o_outside",
                SellerId = sellerId,
                Status = nameof(OrderStatusEnum.Completed),
                CreatedAt = FromBusinessLocal(todayLocal.AddDays(-10)),
                FinalAmount = 200
            };

            _orders.Add(orderInside);
            _orders.Add(orderOutside);
            _orderRepository.Setup(x => x.Query()).Returns(_orders.AsMockDbSet().Object);

            // Act
            var result = await _service.GetSellerSalesStatisticsAsync(sellerId, 7);

            // Assert
            result.Should().NotBeNull();
            result.TotalOrders.Should().Be(1);
            result.NetSales.Should().Be(100); // 200 outside period is ignored
        }

        #endregion

        #region Boundary Tests (B)

        [Fact]
        public async Task GetSellerSalesStatisticsAsync_ShouldBuildSingleDayBuckets_WhenPeriodDaysIsExactlySeven()
        {
            // Arrange
            var sellerId = "seller_1";
            var todayLocal = ToBusinessLocal(DateTime.UtcNow).Date;

            var order = new Order
            {
                OrderId = "o1",
                SellerId = sellerId,
                Status = nameof(OrderStatusEnum.Completed),
                CreatedAt = FromBusinessLocal(todayLocal), // today
                FinalAmount = 150
            };

            _orders.Add(order);
            _orderRepository.Setup(x => x.Query()).Returns(_orders.AsMockDbSet().Object);

            // Act
            var result = await _service.GetSellerSalesStatisticsAsync(sellerId, 7); // Exactly 7 days

            // Assert
            result.Should().NotBeNull();
            result.RevenueTrend.Should().HaveCount(7);
            
            // Labels for 7-day period should represent single days (dd/MM)
            foreach (var trendPoint in result.RevenueTrend)
            {
                trendPoint.Label.Should().NotContain("-"); // No dash represents single-day labels
            }

            result.RevenueTrend.First(t => t.FromDate.Date == todayLocal).Revenue.Should().Be(150);
        }

        #endregion
    }
}
