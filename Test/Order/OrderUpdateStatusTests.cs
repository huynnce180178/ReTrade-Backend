using System;
using System.Collections.Generic;
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
    public class OrderUpdateStatusTests
    {
        private readonly Mock<IOrderRepository> _orderRepository;
        private readonly Mock<IHubContext<OrderHub>> _orderHub;
        private readonly Mock<IHubClients> _hubClients;
        private readonly Mock<IClientProxy> _clientProxy;
        private readonly Mock<INotificationService> _notificationService;
        private readonly OrderService _service;

        public OrderUpdateStatusTests()
        {
            _orderRepository = new Mock<IOrderRepository>();
            _orderHub = new Mock<IHubContext<OrderHub>>();
            _hubClients = new Mock<IHubClients>();
            _clientProxy = new Mock<IClientProxy>();
            _notificationService = new Mock<INotificationService>();

            _hubClients.Setup(c => c.Group(It.IsAny<string>())).Returns(_clientProxy.Object);
            _orderHub.Setup(h => h.Clients).Returns(_hubClients.Object);

            _service = new OrderService(
                _orderRepository.Object,
                _orderHub.Object,
                _notificationService.Object
            );
        }

        #region Normal Tests (N)
        [Fact]
        public async Task UpdateStatusAsync_ShouldUpdateStatusSuccessfully_WhenTransitionIsValid()
        {
            // Arrange
            var orderId = "order_201";
            var sellerId = "seller_202";

            var order = new Order
            {
                OrderId = orderId,
                SellerId = sellerId,
                BuyerId = "buyer_303",
                Status = "Confirmed",
                CreatedAt = DateTime.UtcNow.AddHours(-1),
                Payment = new List<Payment>()
            };

            var dto = new OrderStatusUpdateDto
            {
                Status = "Shipping",
                TrackingCode = "  GHN_TRK999  ",
                ShippingProvider = "  GHN Express  "
            };

            _orderRepository.Setup(r => r.GetForUpdateAsync(orderId)).ReturnsAsync(order);
            _orderRepository.Setup(r => r.UpdateAsync(It.IsAny<Order>())).Returns(Task.CompletedTask);
            _notificationService.Setup(n => n.CreateAndSendAsync(It.IsAny<CreateNotificationDto>())).ReturnsAsync(new NotificationDto());

            // Act
            var result = await _service.UpdateStatusAsync(sellerId, orderId, dto);

            // Assert
            result.Should().NotBeNull();
            result!.Status.Should().Be("Shipping");
            result.TrackingCode.Should().Be("GHN_TRK999");
            result.ShippingProvider.Should().Be("GHN Express");
            result.ExpectedDeliveryTime.Should().NotBeNull();
            order.Status.Should().Be("Shipping");

            _orderRepository.Verify(r => r.UpdateAsync(It.IsAny<Order>()), Times.Once);
        }
        #endregion

        #region Abnormal Tests (A)
        [Fact]
        public async Task UpdateStatusAsync_ShouldReturnNull_WhenOrderNotFoundOrSellerIdMismatch()
        {
            // Scenario 1: Order null
            _orderRepository.Setup(r => r.GetForUpdateAsync("non_existent")).ReturnsAsync((Order?)null);
            var result1 = await _service.UpdateStatusAsync("seller_202", "non_existent", new OrderStatusUpdateDto { Status = "Confirmed" });
            result1.Should().BeNull();

            // Scenario 2: SellerId mismatch
            var order = new Order { OrderId = "order_203", SellerId = "seller_202", Status = "Pending" };
            _orderRepository.Setup(r => r.GetForUpdateAsync("order_203")).ReturnsAsync(order);
            var result2 = await _service.UpdateStatusAsync("other_seller", "order_203", new OrderStatusUpdateDto { Status = "Confirmed" });
            result2.Should().BeNull();

            _orderRepository.Verify(r => r.UpdateAsync(It.IsAny<Order>()), Times.Never);
        }

        [Fact]
        public async Task UpdateStatusAsync_ShouldThrowInvalidOperationException_WhenStatusOrTransitionIsInvalid()
        {
            var orderId = "order_204";
            var sellerId = "seller_202";

            // Scenario 1: Invalid status string
            var order1 = new Order { OrderId = orderId, SellerId = sellerId, Status = "Pending" };
            _orderRepository.Setup(r => r.GetForUpdateAsync(orderId)).ReturnsAsync(order1);
            Func<Task> act1 = async () => await _service.UpdateStatusAsync(sellerId, orderId, new OrderStatusUpdateDto { Status = "InvalidStatusString" });
            await act1.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Invalid order status.*");

            // Scenario 2: Disallowed status transition (Shipping -> Confirmed)
            var order2 = new Order { OrderId = orderId, SellerId = sellerId, Status = "Shipping" };
            _orderRepository.Setup(r => r.GetForUpdateAsync(orderId)).ReturnsAsync(order2);
            Func<Task> act2 = async () => await _service.UpdateStatusAsync(sellerId, orderId, new OrderStatusUpdateDto { Status = "Confirmed" });
            await act2.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Cannot update order status from Shipping to Confirmed.*");

            _orderRepository.Verify(r => r.UpdateAsync(It.IsAny<Order>()), Times.Never);
        }
        #endregion

        #region Boundary Tests (B)
        [Fact]
        public async Task UpdateStatusAsync_ShouldHandleAwaitingPaymentExpiry_Correctly()
        {
            var sellerId = "seller_202";
            var dto = new OrderStatusUpdateDto { Status = "Cancelled" };

            // Scenario 1: Expired (> 15 mins) -> Success
            var expiredOrder = new Order
            {
                OrderId = "order_expired",
                SellerId = sellerId,
                BuyerId = "buyer_303",
                Status = "AwaitingPayment",
                CreatedAt = DateTime.UtcNow.AddMinutes(-20),
                Payment = new List<Payment>()
            };
            _orderRepository.Setup(r => r.GetForUpdateAsync("order_expired")).ReturnsAsync(expiredOrder);
            _orderRepository.Setup(r => r.UpdateAsync(It.IsAny<Order>())).Returns(Task.CompletedTask);

            var result = await _service.UpdateStatusAsync(sellerId, "order_expired", dto);
            result.Should().NotBeNull();
            result!.Status.Should().Be("Cancelled");

            // Scenario 2: Not expired (< 15 mins) -> Throws exception
            var validOrder = new Order
            {
                OrderId = "order_valid",
                SellerId = sellerId,
                Status = "AwaitingPayment",
                CreatedAt = DateTime.UtcNow.AddMinutes(-5)
            };
            _orderRepository.Setup(r => r.GetForUpdateAsync("order_valid")).ReturnsAsync(validOrder);
            Func<Task> act = async () => await _service.UpdateStatusAsync(sellerId, "order_valid", dto);
            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Cannot update order status from AwaitingPayment to Cancelled*");
        }
        #endregion
    }
}
