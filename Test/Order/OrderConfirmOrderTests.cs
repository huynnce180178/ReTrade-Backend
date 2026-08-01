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
    public class OrderConfirmOrderTests
    {
        private readonly Mock<IOrderRepository> _orderRepository;
        private readonly Mock<IHubContext<OrderHub>> _orderHub;
        private readonly Mock<IHubClients> _hubClients;
        private readonly Mock<IClientProxy> _clientProxy;
        private readonly Mock<INotificationService> _notificationService;
        private readonly OrderService _service;

        public OrderConfirmOrderTests()
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
        public async Task ConfirmOrderAsync_ShouldUpdateOrderStatusToConfirmedAndSendNotifications_WhenOrderIsPendingAndSellerIdMatches()
        {
            // Arrange
            var orderId = "order_101";
            var sellerId = "seller_202";
            var buyerId = "buyer_303";

            var order = new Order
            {
                OrderId = orderId,
                OrderCode = "ORD-101",
                SellerId = sellerId,
                BuyerId = buyerId,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow.AddHours(-1),
                Payment = new List<Payment>()
            };

            _orderRepository.Setup(r => r.GetForUpdateAsync(orderId)).ReturnsAsync(order);
            _orderRepository.Setup(r => r.UpdateAsync(It.IsAny<Order>())).Returns(Task.CompletedTask);
            _notificationService.Setup(n => n.CreateAndSendAsync(It.IsAny<CreateNotificationDto>())).ReturnsAsync(new NotificationDto());

            // Act
            var result = await _service.ConfirmOrderAsync(sellerId, orderId);

            // Assert
            result.Should().NotBeNull();
            result!.Status.Should().Be("Confirmed");
            order.Status.Should().Be("Confirmed");
            order.UpdatedAt.Should().NotBeNull();

            _orderRepository.Verify(r => r.GetForUpdateAsync(orderId), Times.Once);
            _orderRepository.Verify(r => r.UpdateAsync(It.Is<Order>(o => o.OrderId == orderId && o.Status == "Confirmed")), Times.Once);
            _notificationService.Verify(n => n.CreateAndSendAsync(It.IsAny<CreateNotificationDto>()), Times.Exactly(2));
            _clientProxy.Verify(p => p.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), default), Times.Exactly(2));
        }
        #endregion

        #region Abnormal Tests (A)
        [Theory]
        [InlineData("non_existent_order", "seller_202", false)]
        [InlineData("order_101", "other_seller", true)]
        public async Task ConfirmOrderAsync_ShouldReturnNull_WhenOrderNotFoundOrSellerIdMismatch(string orderId, string sellerId, bool orderExists)
        {
            // Arrange
            var order = orderExists ? new Order { OrderId = "order_101", SellerId = "seller_202", Status = "Pending" } : null;
            _orderRepository.Setup(r => r.GetForUpdateAsync("order_101")).ReturnsAsync(order);
            _orderRepository.Setup(r => r.GetForUpdateAsync("non_existent_order")).ReturnsAsync((Order?)null);

            // Act
            var result = await _service.ConfirmOrderAsync(sellerId, orderId);

            // Assert
            result.Should().BeNull();
            _orderRepository.Verify(r => r.UpdateAsync(It.IsAny<Order>()), Times.Never);
        }

        [Fact]
        public async Task ConfirmOrderAsync_ShouldThrowInvalidOperationException_WhenCurrentStatusCannotMoveToConfirmed()
        {
            // Arrange
            var orderId = "order_102";
            var sellerId = "seller_202";

            var order = new Order
            {
                OrderId = orderId,
                SellerId = sellerId,
                Status = "Delivered"
            };

            _orderRepository.Setup(r => r.GetForUpdateAsync(orderId)).ReturnsAsync(order);

            // Act
            Func<Task> act = async () => await _service.ConfirmOrderAsync(sellerId, orderId);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Cannot update order status from Delivered to Confirmed*");

            _orderRepository.Verify(r => r.UpdateAsync(It.IsAny<Order>()), Times.Never);
        }
        #endregion

        #region Boundary Tests (B)
        [Fact]
        public async Task ConfirmOrderAsync_ShouldConfirmOrderSuccessfully_EvenIfSendNotificationsFails()
        {
            // Arrange
            var orderId = "order_103";
            var sellerId = "seller_202";

            var order = new Order
            {
                OrderId = orderId,
                SellerId = sellerId,
                BuyerId = "buyer_303",
                Status = "Pending",
                Payment = new List<Payment>()
            };

            _orderRepository.Setup(r => r.GetForUpdateAsync(orderId)).ReturnsAsync(order);
            _orderRepository.Setup(r => r.UpdateAsync(It.IsAny<Order>())).Returns(Task.CompletedTask);
            _notificationService.Setup(n => n.CreateAndSendAsync(It.IsAny<CreateNotificationDto>()))
                .ThrowsAsync(new Exception("Notification service error"));

            // Act
            var result = await _service.ConfirmOrderAsync(sellerId, orderId);

            // Assert
            result.Should().NotBeNull();
            result!.Status.Should().Be("Confirmed");
            order.Status.Should().Be("Confirmed");

            _orderRepository.Verify(r => r.UpdateAsync(It.IsAny<Order>()), Times.Once);
        }

        [Fact]
        public async Task ConfirmOrderAsync_ShouldAllowReConfirmingOrder_WhenCurrentStatusIsAlreadyConfirmed()
        {
            // Arrange
            var orderId = "order_104";
            var sellerId = "seller_202";

            var order = new Order
            {
                OrderId = orderId,
                SellerId = sellerId,
                Status = "Confirmed",
                Payment = new List<Payment>()
            };

            _orderRepository.Setup(r => r.GetForUpdateAsync(orderId)).ReturnsAsync(order);
            _orderRepository.Setup(r => r.UpdateAsync(It.IsAny<Order>())).Returns(Task.CompletedTask);

            // Act
            var result = await _service.ConfirmOrderAsync(sellerId, orderId);

            // Assert
            result.Should().NotBeNull();
            result!.Status.Should().Be("Confirmed");

            _orderRepository.Verify(r => r.UpdateAsync(It.IsAny<Order>()), Times.Once);
        }
        #endregion
    }
}
