using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Moq;
using RetradeBE.Hubs;
using RetradeBE.Mappings;
using RetradeBE.Models;
using RetradeBE.Models.DTOs;
using RetradeBE.Repositories;
using RetradeBE.Services;
using Xunit;

namespace Test.PurchaseTests
{
    public class PurchaseCompletePurchaseTests
    {
        private readonly Mock<IOrderRepository> _orderRepository;
        private readonly Mock<IHubContext<OrderHub>> _orderHub;
        private readonly Mock<IHubClients> _hubClients;
        private readonly Mock<IClientProxy> _clientProxy;
        private readonly IMapper _mapper;
        private readonly PurchaseService _service;

        public PurchaseCompletePurchaseTests()
        {
            _orderRepository = new Mock<IOrderRepository>();
            _orderHub = new Mock<IHubContext<OrderHub>>();
            _hubClients = new Mock<IHubClients>();
            _clientProxy = new Mock<IClientProxy>();

            // Setup SignalR Hub Mocks
            _hubClients.Setup(c => c.Group(It.IsAny<string>())).Returns(_clientProxy.Object);
            _orderHub.Setup(h => h.Clients).Returns(_hubClients.Object);

            var configuration = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<AutoMapperProfile>();
            }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
            _mapper = configuration.CreateMapper();

            _service = new PurchaseService(
                _orderRepository.Object,
                _mapper,
                _orderHub.Object
            );
        }

        #region Normal Tests (N)
        [Fact]
        public async Task CompletePurchaseAsync_ShouldUpdateStatusToCompletedAndNotify_WhenOrderIsDeliveredAndOwnedByBuyer()
        {
            // Arrange
            var buyerId = "buyer_123";
            var orderId = "order_001";
            var order = new Order
            {
                OrderId = orderId,
                BuyerId = buyerId,
                SellerId = "seller_456",
                Status = "Delivered",
                CreatedAt = DateTime.UtcNow.AddDays(-2),
                UpdatedAt = DateTime.UtcNow.AddDays(-1)
            };

            _orderRepository.Setup(r => r.GetForUpdateAsync(orderId)).ReturnsAsync(order);
            _orderRepository.Setup(r => r.UpdateAsync(It.IsAny<Order>())).Returns(Task.CompletedTask);

            // Act
            var result = await _service.CompletePurchaseAsync(buyerId, orderId);

            // Assert
            result.Should().NotBeNull();
            result!.Status.Should().Be("Completed");
            order.Status.Should().Be("Completed");

            _orderRepository.Verify(r => r.GetForUpdateAsync(orderId), Times.Once);
            _orderRepository.Verify(r => r.UpdateAsync(It.Is<Order>(o => o.Status == "Completed")), Times.Once);
            _clientProxy.Verify(c => c.SendCoreAsync("SellerOrderStatusChanged", It.IsAny<object[]>(), default), Times.Once);
            _clientProxy.Verify(c => c.SendCoreAsync("BuyerOrderStatusChanged", It.IsAny<object[]>(), default), Times.Once);
        }
        #endregion

        #region Abnormal Tests (A)
        [Fact]
        public async Task CompletePurchaseAsync_ShouldReturnNull_WhenBuyerIdIsInvalid()
        {
            // Arrange
            var invalidBuyerIds = new string?[] { null, "", "   " };

            foreach (var invalidBuyerId in invalidBuyerIds)
            {
                // Act
                var result = await _service.CompletePurchaseAsync(invalidBuyerId!, "order_001");

                // Assert
                result.Should().BeNull();
            }

            _orderRepository.Verify(r => r.GetForUpdateAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task CompletePurchaseAsync_ShouldReturnNull_WhenOrderNotFoundOrNotOwnedByBuyer()
        {
            // Arrange 1: Order not found
            var buyerId = "buyer_123";
            _orderRepository.Setup(r => r.GetForUpdateAsync("non_existent")).ReturnsAsync((Order?)null);

            var resultNotFound = await _service.CompletePurchaseAsync(buyerId, "non_existent");
            resultNotFound.Should().BeNull();

            // Arrange 2: Order belongs to different buyer
            var orderOtherBuyer = new Order { OrderId = "order_other", BuyerId = "other_buyer", Status = "Delivered" };
            _orderRepository.Setup(r => r.GetForUpdateAsync("order_other")).ReturnsAsync(orderOtherBuyer);

            var resultOtherBuyer = await _service.CompletePurchaseAsync(buyerId, "order_other");
            resultOtherBuyer.Should().BeNull();

            _orderRepository.Verify(r => r.UpdateAsync(It.IsAny<Order>()), Times.Never);
        }

        [Fact]
        public async Task CompletePurchaseAsync_ShouldThrowInvalidOperationException_WhenOrderStatusIsNotDelivered()
        {
            // Arrange
            var buyerId = "buyer_123";
            var orderId = "order_001";
            var invalidStatuses = new[] { "Pending", "Confirmed", "Completed", "Cancelled", "AwaitingPayment" };

            foreach (var status in invalidStatuses)
            {
                var order = new Order
                {
                    OrderId = orderId,
                    BuyerId = buyerId,
                    Status = status
                };

                _orderRepository.Setup(r => r.GetForUpdateAsync(orderId)).ReturnsAsync(order);

                // Act
                Func<Task> act = async () => await _service.CompletePurchaseAsync(buyerId, orderId);

                // Assert
                await act.Should().ThrowAsync<InvalidOperationException>()
                    .WithMessage("Purchase can only be completed from Delivered status.");
            }

            _orderRepository.Verify(r => r.UpdateAsync(It.IsAny<Order>()), Times.Never);
        }
        #endregion

        #region Boundary Tests (B)
        [Fact]
        public async Task CompletePurchaseAsync_ShouldHandleCaseInsensitiveStatusCheck_WhenOrderIsDeliveredInDifferentCase()
        {
            // Arrange
            var buyerId = "buyer_123";
            var orderId = "order_lowercase";
            var order = new Order
            {
                OrderId = orderId,
                BuyerId = buyerId,
                Status = "delivered" // chữ thường
            };

            _orderRepository.Setup(r => r.GetForUpdateAsync(orderId)).ReturnsAsync(order);
            _orderRepository.Setup(r => r.UpdateAsync(It.IsAny<Order>())).Returns(Task.CompletedTask);

            // Act
            var result = await _service.CompletePurchaseAsync(buyerId, orderId);

            // Assert
            result.Should().NotBeNull();
            result!.Status.Should().Be("Completed");
            order.Status.Should().Be("Completed");
        }

        [Fact]
        public async Task CompletePurchaseAsync_ShouldUpdateTimestampToUtcNow_WhenCompleted()
        {
            // Arrange
            var buyerId = "buyer_123";
            var orderId = "order_001";
            var beforeTime = DateTime.UtcNow.AddSeconds(-1);
            var order = new Order
            {
                OrderId = orderId,
                BuyerId = buyerId,
                Status = "Delivered",
                UpdatedAt = DateTime.UtcNow.AddDays(-5)
            };

            _orderRepository.Setup(r => r.GetForUpdateAsync(orderId)).ReturnsAsync(order);
            _orderRepository.Setup(r => r.UpdateAsync(It.IsAny<Order>())).Returns(Task.CompletedTask);

            // Act
            await _service.CompletePurchaseAsync(buyerId, orderId);

            // Assert
            order.UpdatedAt.Should().NotBeNull();
            order.UpdatedAt.Value.Should().BeAfter(beforeTime);
            order.UpdatedAt.Value.Should().BeOnOrBefore(DateTime.UtcNow);
        }
        #endregion
    }
}
