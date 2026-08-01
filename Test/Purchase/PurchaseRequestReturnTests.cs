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
    public class PurchaseRequestReturnTests
    {
        private readonly Mock<IOrderRepository> _orderRepository;
        private readonly Mock<IHubContext<OrderHub>> _orderHub;
        private readonly Mock<IHubClients> _hubClients;
        private readonly Mock<IClientProxy> _clientProxy;
        private readonly IMapper _mapper;
        private readonly PurchaseService _service;

        public PurchaseRequestReturnTests()
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
        public async Task RequestReturnAsync_ShouldUpdateOrderStatusToReturnRequestedAndSendNotification_WhenRequestIsValid()
        {
            // Arrange
            var buyerId = "buyer_100";
            var orderId = "order_200";
            var sellerId = "seller_300";

            var order = new Order
            {
                OrderId = orderId,
                OrderCode = "ORD-200",
                BuyerId = buyerId,
                SellerId = sellerId,
                Status = "Completed",
                UpdatedAt = DateTime.UtcNow.AddDays(-2), // Received 2 days ago (within 7 days)
                Payment = new List<Payment>()
            };

            var dto = new ReturnPurchaseRequestDto
            {
                Reason = "Item damaged during delivery"
            };

            _orderRepository.Setup(r => r.GetForUpdateAsync(orderId)).ReturnsAsync(order);
            _orderRepository.Setup(r => r.UpdateAsync(It.IsAny<Order>())).Returns(Task.CompletedTask);

            // Act
            var result = await _service.RequestReturnAsync(buyerId, orderId, dto);

            // Assert
            result.Should().NotBeNull();
            result!.OrderId.Should().Be(orderId);
            result.Status.Should().Be("ReturnRequested");
            result.ReturnReason.Should().Be("Item damaged during delivery");
            order.Status.Should().Be("ReturnRequested");
            order.ReturnReason.Should().Be("Item damaged during delivery");

            _orderRepository.Verify(r => r.GetForUpdateAsync(orderId), Times.Once);
            _orderRepository.Verify(r => r.UpdateAsync(It.Is<Order>(o => o.OrderId == orderId && o.Status == "ReturnRequested")), Times.Once);
            _clientProxy.Verify(p => p.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), default), Times.Exactly(2));
        }
        #endregion

        #region Abnormal Tests (A)
        [Fact]
        public async Task RequestReturnAsync_ShouldReturnNull_WhenBuyerIdIsNullOrEmptyOrOrderNotFoundOrNotBelongingToBuyer()
        {
            var dto = new ReturnPurchaseRequestDto { Reason = "Defective item" };

            // Scenario 1: buyerId is null or empty
            var resultNullBuyer = await _service.RequestReturnAsync(null!, "order_200", dto);
            resultNullBuyer.Should().BeNull();

            var resultEmptyBuyer = await _service.RequestReturnAsync("   ", "order_200", dto);
            resultEmptyBuyer.Should().BeNull();

            // Scenario 2: Order not found
            _orderRepository.Setup(r => r.GetForUpdateAsync("non_existent")).ReturnsAsync((Order?)null);
            var resultNullOrder = await _service.RequestReturnAsync("buyer_100", "non_existent", dto);
            resultNullOrder.Should().BeNull();

            // Scenario 3: Order belongs to another buyer
            var otherBuyerOrder = new Order { OrderId = "order_201", BuyerId = "other_buyer", Status = "Completed" };
            _orderRepository.Setup(r => r.GetForUpdateAsync("order_201")).ReturnsAsync(otherBuyerOrder);
            var resultOtherBuyer = await _service.RequestReturnAsync("buyer_100", "order_201", dto);
            resultOtherBuyer.Should().BeNull();

            _orderRepository.Verify(r => r.UpdateAsync(It.IsAny<Order>()), Times.Never);
        }

        [Fact]
        public async Task RequestReturnAsync_ShouldThrowInvalidOperationException_WhenReasonIsEmptyOrStatusNotCompleted()
        {
            var buyerId = "buyer_100";
            var orderId = "order_202";

            // Scenario 1: Reason is null or empty
            Func<Task> actEmptyReason = async () => await _service.RequestReturnAsync(buyerId, orderId, new ReturnPurchaseRequestDto { Reason = "   " });
            await actEmptyReason.Should().ThrowAsync<InvalidOperationException>().WithMessage("Return reason is required.");

            // Scenario 2: Status is not Completed (e.g. Delivered or Pending)
            var pendingOrder = new Order { OrderId = orderId, BuyerId = buyerId, Status = "Delivered" };
            _orderRepository.Setup(r => r.GetForUpdateAsync(orderId)).ReturnsAsync(pendingOrder);

            Func<Task> actNotCompleted = async () => await _service.RequestReturnAsync(buyerId, orderId, new ReturnPurchaseRequestDto { Reason = "Item damaged" });
            await actNotCompleted.Should().ThrowAsync<InvalidOperationException>().WithMessage("Purchase can only request return from Completed status.");

            _orderRepository.Verify(r => r.UpdateAsync(It.IsAny<Order>()), Times.Never);
        }
        #endregion

        #region Boundary Tests (B)
        [Fact]
        public async Task RequestReturnAsync_ShouldThrowInvalidOperationException_WhenReturnRequestWindowIsExpired()
        {
            // Arrange
            var buyerId = "buyer_100";
            var orderId = "order_203";

            // Completed 10 days ago (> 7 days return window)
            var expiredOrder = new Order
            {
                OrderId = orderId,
                BuyerId = buyerId,
                Status = "Completed",
                UpdatedAt = DateTime.UtcNow.AddDays(-10)
            };

            var dto = new ReturnPurchaseRequestDto { Reason = "Changed my mind" };

            _orderRepository.Setup(r => r.GetForUpdateAsync(orderId)).ReturnsAsync(expiredOrder);

            // Act
            Func<Task> act = async () => await _service.RequestReturnAsync(buyerId, orderId, dto);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Purchase can only request return within 7 days after receiving the order.");

            _orderRepository.Verify(r => r.UpdateAsync(It.IsAny<Order>()), Times.Never);
        }
        #endregion
    }
}
