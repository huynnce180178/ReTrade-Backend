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
    public class OrderGetMyOrdersTests
    {
        private readonly Mock<IOrderRepository> _orderRepository;
        private readonly Mock<IHubContext<OrderHub>> _orderHub;
        private readonly Mock<INotificationService> _notificationService;
        private readonly OrderService _service;

        public OrderGetMyOrdersTests()
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
        public async Task GetMyOrdersAsync_ShouldReturnPagedOrdersWithFiltersAndPagination_WhenUserIdAndQueryAreValid()
        {
            // Arrange
            var userId = "user_1";
            var query = new OrderSearchQueryDto
            {
                Page = 1,
                PageSize = 2,
                Status = "Pending"
            };

            var orders = new List<Order>
            {
                new Order { OrderId = "o1", BuyerId = userId, Status = "Pending", CreatedAt = DateTime.UtcNow.AddHours(-3) },
                new Order { OrderId = "o2", BuyerId = userId, Status = "Pending", CreatedAt = DateTime.UtcNow.AddHours(-1) },
                new Order { OrderId = "o3", BuyerId = userId, Status = "Completed", CreatedAt = DateTime.UtcNow },
                new Order { OrderId = "o4", BuyerId = "other_user", Status = "Pending", CreatedAt = DateTime.UtcNow }
            };

            _orderRepository.Setup(r => r.Query()).Returns(orders.AsAsyncQueryable());

            // Act
            var result = await _service.GetMyOrdersAsync(userId, query);

            // Assert
            result.Should().NotBeNull();
            result.TotalItems.Should().Be(2);
            result.TotalPages.Should().Be(1);
            result.Page.Should().Be(1);
            result.PageSize.Should().Be(2);
            result.Items.Should().HaveCount(2);
            result.Items[0].OrderId.Should().Be("o2"); // Sắp xếp CreatedAt giảm dần
            result.Items[1].OrderId.Should().Be("o1");

            _orderRepository.Verify(r => r.Query(), Times.Once);
        }

        [Fact]
        public async Task GetMyOrdersAsync_ShouldFilterBySearchTermAndDateRange_Correctly()
        {
            // Arrange
            var userId = "user_1";
            var now = DateTime.UtcNow;
            var query = new OrderSearchQueryDto
            {
                SearchTerm = "Laptop",
                FromDate = now.AddDays(-5),
                ToDate = now
            };

            var product1 = new Product { ProductId = "p1", Name = "Gaming Laptop" };
            var product2 = new Product { ProductId = "p2", Name = "Smartphone" };

            var orders = new List<Order>
            {
                new Order
                {
                    OrderId = "o1",
                    BuyerId = userId,
                    OrderCode = "ORD-001",
                    Product = product1,
                    CreatedAt = now.AddDays(-2)
                },
                new Order
                {
                    OrderId = "o2",
                    BuyerId = userId,
                    OrderCode = "ORD-002",
                    Product = product2,
                    CreatedAt = now.AddDays(-2)
                }
            };

            _orderRepository.Setup(r => r.Query()).Returns(orders.AsAsyncQueryable());

            // Act
            var result = await _service.GetMyOrdersAsync(userId, query);

            // Assert
            result.TotalItems.Should().Be(1);
            result.Items.First().OrderId.Should().Be("o1");
            result.Items.First().ProductName.Should().Be("Gaming Laptop");
        }
        #endregion

        #region Abnormal Tests (A)
        [Fact]
        public async Task GetMyOrdersAsync_ShouldReturnEmptyPagedResult_WhenUserIdIsInvalid()
        {
            // Arrange
            var invalidUserIds = new string?[] { null, "", "   " };
            var query = new OrderSearchQueryDto { Page = 1, PageSize = 10 };

            foreach (var invalidUserId in invalidUserIds)
            {
                // Act
                var result = await _service.GetMyOrdersAsync(invalidUserId!, query);

                // Assert
                result.Should().NotBeNull();
                result.TotalItems.Should().Be(0);
                result.Items.Should().BeEmpty();
                result.Page.Should().Be(1);
                result.PageSize.Should().Be(10);
            }

            _orderRepository.Verify(r => r.Query(), Times.Never);
        }

        [Fact]
        public async Task GetMyOrdersAsync_ShouldReturnEmptyPagedResult_WhenNoOrdersMatchFilters()
        {
            // Arrange
            var userId = "user_1";
            var query = new OrderSearchQueryDto { Status = "NonExistentStatus" };
            var orders = new List<Order>
            {
                new Order { OrderId = "o1", BuyerId = userId, Status = "Completed", CreatedAt = DateTime.UtcNow }
            };

            _orderRepository.Setup(r => r.Query()).Returns(orders.AsAsyncQueryable());

            // Act
            var result = await _service.GetMyOrdersAsync(userId, query);

            // Assert
            result.TotalItems.Should().Be(0);
            result.Items.Should().BeEmpty();
            result.TotalPages.Should().Be(1);
        }
        #endregion

        #region Boundary Tests (B)
        [Fact]
        public async Task GetMyOrdersAsync_ShouldNormalizeInvalidPageAndPageSize_Correctly()
        {
            // Arrange
            var userId = "user_1";
            var query = new OrderSearchQueryDto
            {
                Page = -5,       // Âm -> Quy về 1
                PageSize = 500   // Quá lớn -> Quy về max 100
            };

            var orders = new List<Order>
            {
                new Order { OrderId = "o1", BuyerId = userId, CreatedAt = DateTime.UtcNow }
            };

            _orderRepository.Setup(r => r.Query()).Returns(orders.AsAsyncQueryable());

            // Act
            var result = _service.GetMyOrdersAsync(userId, query).Result;

            // Assert
            result.Page.Should().Be(1);
            result.PageSize.Should().Be(100);
            result.TotalItems.Should().Be(1);
        }

        [Fact]
        public async Task GetMyOrdersAsync_ShouldSortByCustomCriteria_WhenSortByIsSpecified()
        {
            // Arrange
            var userId = "user_1";
            var query = new OrderSearchQueryDto
            {
                SortBy = "finalamount desc"
            };

            var orders = new List<Order>
            {
                new Order { OrderId = "o1", BuyerId = userId, FinalAmount = 100, CreatedAt = DateTime.UtcNow.AddMinutes(-10) },
                new Order { OrderId = "o2", BuyerId = userId, FinalAmount = 500, CreatedAt = DateTime.UtcNow.AddMinutes(-5) }
            };

            _orderRepository.Setup(r => r.Query()).Returns(orders.AsAsyncQueryable());

            // Act
            var result = await _service.GetMyOrdersAsync(userId, query);

            // Assert
            result.Items.Should().HaveCount(2);
            result.Items[0].OrderId.Should().Be("o2"); // FinalAmount 500 xếp trước
            result.Items[1].OrderId.Should().Be("o1"); // FinalAmount 100 xếp sau
        }
        #endregion
    }
}
