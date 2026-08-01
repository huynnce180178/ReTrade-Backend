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
    public class PurchaseCancelPurchaseTests
    {
        private readonly Mock<IOrderRepository> _orderRepository;
        private readonly Mock<IHubContext<OrderHub>> _orderHub;
        private readonly Mock<IHubClients> _hubClients;
        private readonly Mock<IClientProxy> _clientProxy;
        private readonly IMapper _mapper;
        private readonly PurchaseService _service;

        public PurchaseCancelPurchaseTests()
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
        public async Task CancelPurchaseAsync_ShouldUpdateStatusToCancelledAndNotify_WhenOrderCanBeCancelledAndOwnedByBuyer()
        {
            // Arrange
            var buyerId = "buyer_123";
            var orderId = "order_001";
            var validCancelStatuses = new[] { "Pending", "Confirmed", "AwaitingPayment" };

            foreach (var initialStatus in validCancelStatuses)
            {
                var order = new Order
                {
                    OrderId = orderId,
                    BuyerId = buyerId,
                    SellerId = "seller_456",
                    Status = initialStatus,
                    CreatedAt = DateTime.UtcNow.AddDays(-1)
                };

                _orderRepository.Setup(r => r.GetForUpdateAsync(orderId)).ReturnsAsync(order);
                _orderRepository.Setup(r => r.UpdateAsync(It.IsAny<Order>())).Returns(Task.CompletedTask);

                // Act
                var result = await _service.CancelPurchaseAsync(buyerId, orderId);

                // Assert
                result.Should().NotBeNull();
                result!.Status.Should().Be("Cancelled");
                order.Status.Should().Be("Cancelled");
            }

            _orderRepository.Verify(r => r.UpdateAsync(It.Is<Order>(o => o.Status == "Cancelled")), Times.Exactly(validCancelStatuses.Length));
            _clientProxy.Verify(c => c.SendCoreAsync("SellerOrderStatusChanged", It.IsAny<object[]>(), default), Times.Exactly(validCancelStatuses.Length));
            _clientProxy.Verify(c => c.SendCoreAsync("BuyerOrderStatusChanged", It.IsAny<object[]>(), default), Times.Exactly(validCancelStatuses.Length));
        }
        #endregion

        #region Abnormal Tests (A)
        [Fact]
        public async Task CancelPurchaseAsync_ShouldReturnNull_WhenBuyerIdIsInvalid()
        {
            // Arrange
            var invalidBuyerIds = new string?[] { null, "", "   " };

            foreach (var invalidBuyerId in invalidBuyerIds)
            {
                // Act
                var result = await _service.CancelPurchaseAsync(invalidBuyerId!, "order_001");

                // Assert
                result.Should().BeNull();
            }

            _orderRepository.Verify(r => r.GetForUpdateAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task CancelPurchaseAsync_ShouldReturnNull_WhenOrderNotFoundOrNotOwnedByBuyer()
        {
            // Arrange 1: Order not found
            var buyerId = "buyer_123";
            _orderRepository.Setup(r => r.GetForUpdateAsync("non_existent")).ReturnsAsync((Order?)null);

            var resultNotFound = await _service.CancelPurchaseAsync(buyerId, "non_existent");
            resultNotFound.Should().BeNull();

            // Arrange 2: Order belongs to another buyer
            var orderOtherBuyer = new Order { OrderId = "order_other", BuyerId = "other_buyer", Status = "Pending" };
            _orderRepository.Setup(r => r.GetForUpdateAsync("order_other")).ReturnsAsync(orderOtherBuyer);

            var resultOtherBuyer = await _service.CancelPurchaseAsync(buyerId, "order_other");
            resultOtherBuyer.Should().BeNull();

            _orderRepository.Verify(r => r.UpdateAsync(It.IsAny<Order>()), Times.Never);
        }

        [Fact]
        public async Task CancelPurchaseAsync_ShouldThrowInvalidOperationException_WhenOrderStatusCannotBeCancelled()
        {
            // Arrange
            var buyerId = "buyer_123";
            var orderId = "order_001";
            var unCancellableStatuses = new[] { "Delivered", "Completed", "Cancelled", "ReturnRequested" };

            foreach (var status in unCancellableStatuses)
            {
                var order = new Order
                {
                    OrderId = orderId,
                    BuyerId = buyerId,
                    Status = status
                };

                _orderRepository.Setup(r => r.GetForUpdateAsync(orderId)).ReturnsAsync(order);

                // Act
                Func<Task> act = async () => await _service.CancelPurchaseAsync(buyerId, orderId);

                // Assert
                await act.Should().ThrowAsync<InvalidOperationException>()
                    .WithMessage("Purchase can only be cancelled from AwaitingPayment, Pending, or Confirmed status.");
            }

            _orderRepository.Verify(r => r.UpdateAsync(It.IsAny<Order>()), Times.Never);
        }
        #endregion

        #region Boundary Tests (B)
        [Fact]
        public async Task CancelPurchaseAsync_ShouldHandleCaseInsensitiveStatusCheck_WhenStatusIsAwaitingPaymentOrConfirmedInDifferentCase()
        {
            // Arrange
            var buyerId = "buyer_123";
            var orderId = "order_001";
            var lowercaseStatuses = new[] { "awaitingpayment", "pending", "confirmed" };

            foreach (var status in lowercaseStatuses)
            {
                var order = new Order
                {
                    OrderId = orderId,
                    BuyerId = buyerId,
                    Status = status
                };

                _orderRepository.Setup(r => r.GetForUpdateAsync(orderId)).ReturnsAsync(order);
                _orderRepository.Setup(r => r.UpdateAsync(It.IsAny<Order>())).Returns(Task.CompletedTask);

                // Act
                var result = await _service.CancelPurchaseAsync(buyerId, orderId);

                // Assert
                result.Should().NotBeNull();
                result!.Status.Should().Be("Cancelled");
                order.Status.Should().Be("Cancelled");
            }
        }

        [Fact]
        public async Task CancelPurchaseAsync_ShouldUpdateTimestampToUtcNow_WhenCancelled()
        {
            // Arrange
            var buyerId = "buyer_123";
            var orderId = "order_001";
            var beforeTime = DateTime.UtcNow.AddSeconds(-1);
            var order = new Order
            {
                OrderId = orderId,
                BuyerId = buyerId,
                Status = "Pending",
                UpdatedAt = DateTime.UtcNow.AddDays(-3)
            };

            _orderRepository.Setup(r => r.GetForUpdateAsync(orderId)).ReturnsAsync(order);
            _orderRepository.Setup(r => r.UpdateAsync(It.IsAny<Order>())).Returns(Task.CompletedTask);

            // Act
            await _service.CancelPurchaseAsync(buyerId, orderId);

            // Assert
            order.UpdatedAt.Should().NotBeNull();
            order.UpdatedAt.Value.Should().BeAfter(beforeTime);
            order.UpdatedAt.Value.Should().BeOnOrBefore(DateTime.UtcNow);
        }
        #endregion
    }
}
