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
    public class PurchaseGetBuyerPurchaseDetailTests
    {
        private readonly Mock<IOrderRepository> _orderRepository;
        private readonly Mock<IHubContext<OrderHub>> _orderHub;
        private readonly IMapper _mapper;
        private readonly PurchaseService _service;

        public PurchaseGetBuyerPurchaseDetailTests()
        {
            _orderRepository = new Mock<IOrderRepository>();
            _orderHub = new Mock<IHubContext<OrderHub>>();

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
        public async Task GetByIdAsync_ShouldReturnMappedPurchaseDetailDto_WhenBuyerIdAndOrderIdAreValidAndOwnedByBuyer()
        {
            // Arrange
            var buyerId = "buyer_123";
            var orderId = "order_001";
            var buyer = new User { UserId = buyerId, FirstName = "Nguyen", LastName = "Văn B", Email = "b@test.com", Phone = "0912345678" };
            var seller = new User { UserId = "seller_456", FirstName = "Tran", LastName = "Văn C", Email = "c@test.com", Phone = "0987654321" };
            var product = new Product { ProductId = "prod_789", Name = "iPhone 15 Pro" };

            var order = new Order
            {
                OrderId = orderId,
                BuyerId = buyerId,
                Buyer = buyer,
                SellerId = "seller_456",
                Seller = seller,
                Product = product,
                Status = "Delivered",
                CreatedAt = DateTime.UtcNow.AddDays(-3),
                Review = new List<Review> { new Review { ReviewId = "rev_1", OrderId = orderId } }
            };

            _orderRepository.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync(order);

            // Act
            var result = await _service.GetByIdAsync(buyerId, orderId);

            // Assert
            result.Should().NotBeNull();
            result!.OrderId.Should().Be(orderId);
            result.BuyerId.Should().Be(buyerId);
            result.BuyerName.Should().Be("Nguyen Văn B");
            result.BuyerEmail.Should().Be("b@test.com");
            result.SellerName.Should().Be("Tran Văn C");
            result.ProductName.Should().Be("iPhone 15 Pro");
            result.Status.Should().Be("Delivered");
            result.HasReview.Should().BeTrue();

            _orderRepository.Verify(r => r.GetByIdAsync(orderId), Times.Once);
        }
        #endregion

        #region Abnormal Tests (A)
        [Fact]
        public async Task GetByIdAsync_ShouldThrowArgumentException_WhenBuyerIdIsInvalid()
        {
            // Arrange
            var invalidBuyerIds = new string?[] { null, "", "   " };

            foreach (var invalidBuyerId in invalidBuyerIds)
            {
                // Act
                Func<Task> act = async () => await _service.GetByIdAsync(invalidBuyerId!, "order_001");

                // Assert
                await act.Should().ThrowAsync<ArgumentException>()
                    .WithMessage("*Buyer ID is required*");
            }

            _orderRepository.Verify(r => r.GetByIdAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldThrowKeyNotFoundException_WhenOrderNotFound()
        {
            // Arrange
            var buyerId = "buyer_123";
            var orderId = "non_existent_order";

            _orderRepository.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync((Order?)null);

            // Act
            Func<Task> act = async () => await _service.GetByIdAsync(buyerId, orderId);

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage("*Purchase order not found*");

            _orderRepository.Verify(r => r.GetByIdAsync(orderId), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldThrowUnauthorizedAccessException_WhenOrderBelongsToDifferentBuyer()
        {
            // Arrange
            var buyerId = "buyer_123";
            var orderId = "order_001";

            var orderOfOtherBuyer = new Order
            {
                OrderId = orderId,
                BuyerId = "other_buyer_999",
                Status = "Completed"
            };

            _orderRepository.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync(orderOfOtherBuyer);

            // Act
            Func<Task> act = async () => await _service.GetByIdAsync(buyerId, orderId);

            // Assert
            await act.Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("*You do not have permission to view this order*");

            _orderRepository.Verify(r => r.GetByIdAsync(orderId), Times.Once);
        }
        #endregion

        #region Boundary Tests (B)
        [Fact]
        public async Task GetByIdAsync_ShouldThrowArgumentException_WhenOrderIdIsInvalid()
        {
            // Arrange
            var buyerId = "buyer_123";
            var invalidOrderIds = new string?[] { null, "", "   " };

            foreach (var invalidOrderId in invalidOrderIds)
            {
                // Act
                Func<Task> act = async () => await _service.GetByIdAsync(buyerId, invalidOrderId!);

                // Assert
                await act.Should().ThrowAsync<ArgumentException>()
                    .WithMessage("*Order ID is required*");
            }
        }
        #endregion
    }
}
