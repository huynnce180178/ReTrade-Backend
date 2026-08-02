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
    public class OrderGetOrderDetailTests
    {
        private readonly Mock<IOrderRepository> _orderRepository;
        private readonly Mock<IHubContext<OrderHub>> _orderHub;
        private readonly Mock<INotificationService> _notificationService;
        private readonly OrderService _service;

        public OrderGetOrderDetailTests()
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
        public async Task GetOrderDetailAsync_ShouldReturnOrderDetailDto_WhenOrderExistsAndSellerIdMatches()
        {
            // Arrange
            var orderId = "ord_1001";
            var sellerId = "seller_1001";
            var buyerId = "buyer_1001";
            var now = DateTime.UtcNow;

            var buyer = new User
            {
                UserId = buyerId,
                FirstName = "Nguyen",
                LastName = "Van A",
                Email = "buyer@test.com",
                Phone = "0901234567"
            };

            var seller = new User
            {
                UserId = sellerId,
                FirstName = "Tran",
                LastName = "Thi B",
                Email = "seller@test.com",
                Phone = "0909876543"
            };

            var mainImage = new Image { ImageUrl = "https://example.com/main.png" };
            var secondaryImage = new Image { ImageUrl = "https://example.com/sec.png" };

            var product = new Product
            {
                ProductId = "prod_100",
                Name = "Laptop Gaming",
                ProductImage = new List<ProductImage>
                {
                    new ProductImage { SortOrder = 2, IsMain = false, Image = secondaryImage },
                    new ProductImage { SortOrder = 1, IsMain = true, Image = mainImage }
                }
            };

            var payments = new List<Payment>
            {
                new Payment
                {
                    PaymentId = "pay_1",
                    Amount = 500,
                    PaymentMethod = "VNPAY",
                    ProviderTransactionId = "TXN_001",
                    Status = "Success",
                    CreatedAt = now.AddMinutes(-30),
                    UpdatedAt = now.AddMinutes(-30)
                },
                new Payment
                {
                    PaymentId = "pay_2",
                    Amount = 1000,
                    PaymentMethod = "VNPAY",
                    ProviderTransactionId = "TXN_002",
                    Status = "Success",
                    CreatedAt = now.AddMinutes(-5),
                    UpdatedAt = now.AddMinutes(-5)
                }
            };

            var order = new Order
            {
                OrderId = orderId,
                OrderCode = "ORD-2026-001",
                ProductId = "prod_100",
                Product = product,
                BuyerId = buyerId,
                Buyer = buyer,
                SellerId = sellerId,
                Seller = seller,
                Quantity = 1,
                UnitPrice = 1500,
                TotalAmount = 1500,
                ShippingFee = 30,
                DiscountAmount = 50,
                FinalAmount = 1480,
                Status = "Confirmed",
                TrackingCode = "TRK123456",
                ShippingProvider = "GHN",
                ExpectedDeliveryTime = now.AddDays(3),
                CreatedAt = now.AddHours(-2),
                UpdatedAt = now.AddHours(-1),
                AddressSnapshot = "Nguyen Van A - 0901234567 - 123 Duong Le Loi",
                Payment = payments
            };

            _orderRepository.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync(order);

            // Act
            var result = await _service.GetOrderDetailAsync(sellerId, orderId);

            // Assert
            result.Should().NotBeNull();
            result!.OrderId.Should().Be(orderId);
            result.OrderCode.Should().Be("ORD-2026-001");
            result.ProductId.Should().Be("prod_100");
            result.ProductName.Should().Be("Laptop Gaming");
            result.ProductImageUrl.Should().Be("https://example.com/main.png");
            result.BuyerId.Should().Be(buyerId);
            result.BuyerName.Should().Be("Nguyen Van A");
            result.BuyerEmail.Should().Be("buyer@test.com");
            result.BuyerPhone.Should().Be("0901234567");
            result.SellerId.Should().Be(sellerId);
            result.SellerName.Should().Be("Tran Thi B");
            result.SellerEmail.Should().Be("seller@test.com");
            result.SellerPhone.Should().Be("0909876543");
            result.Quantity.Should().Be(1);
            result.UnitPrice.Should().Be(1500);
            result.TotalAmount.Should().Be(1500);
            result.ShippingFee.Should().Be(30);
            result.DiscountAmount.Should().Be(50);
            result.FinalAmount.Should().Be(1480);
            result.Status.Should().Be("Confirmed");
            result.TrackingCode.Should().Be("TRK123456");
            result.ShippingProvider.Should().Be("GHN");

            // Payments mapped & ordered descending by CreatedAt
            result.Payments.Should().HaveCount(2);
            result.Payments[0].PaymentId.Should().Be("pay_2");
            result.Payments[1].PaymentId.Should().Be("pay_1");

            _orderRepository.Verify(r => r.GetByIdAsync(orderId), Times.Once);
        }

        [Fact]
        public async Task GetOrderDetailAsync_ShouldFallbackToSortedProductImage_WhenMainImageIsNotSet()
        {
            // Arrange
            var orderId = "ord_1002";
            var sellerId = "seller_1001";

            var imgFirst = new Image { ImageUrl = "https://example.com/first.png" };
            var imgSecond = new Image { ImageUrl = "https://example.com/second.png" };

            var product = new Product
            {
                ProductId = "prod_101",
                Name = "Smartphone",
                ProductImage = new List<ProductImage>
                {
                    new ProductImage { SortOrder = 2, IsMain = false, Image = imgSecond },
                    new ProductImage { SortOrder = 1, IsMain = false, Image = imgFirst }
                }
            };

            var order = new Order
            {
                OrderId = orderId,
                SellerId = sellerId,
                ProductId = "prod_101",
                Product = product,
                Payment = new List<Payment>()
            };

            _orderRepository.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync(order);

            // Act
            var result = await _service.GetOrderDetailAsync(sellerId, orderId);

            // Assert
            result.Should().NotBeNull();
            result!.ProductImageUrl.Should().Be("https://example.com/first.png");
            _orderRepository.Verify(r => r.GetByIdAsync(orderId), Times.Once);
        }
        #endregion

        #region Abnormal Tests (A)
        [Fact]
        public async Task GetOrderDetailAsync_ShouldReturnNull_WhenOrderDoesNotExist()
        {
            // Arrange
            var orderId = "non_existent_order";
            var sellerId = "seller_1001";

            _orderRepository.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync((Order?)null);

            // Act
            var result = await _service.GetOrderDetailAsync(sellerId, orderId);

            // Assert
            result.Should().BeNull();
            _orderRepository.Verify(r => r.GetByIdAsync(orderId), Times.Once);
        }

        [Fact]
        public async Task GetOrderDetailAsync_ShouldReturnNull_WhenSellerIdDoesNotMatch()
        {
            // Arrange
            var orderId = "ord_1003";
            var actualSellerId = "seller_1001";
            var requestedSellerId = "seller_9999";

            var order = new Order
            {
                OrderId = orderId,
                SellerId = actualSellerId
            };

            _orderRepository.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync(order);

            // Act
            var result = await _service.GetOrderDetailAsync(requestedSellerId, orderId);

            // Assert
            result.Should().BeNull();
            _orderRepository.Verify(r => r.GetByIdAsync(orderId), Times.Once);
        }

        [Fact]
        public async Task GetOrderDetailAsync_ShouldReturnNull_WhenSellerIdIsNullOrEmpty()
        {
            // Arrange
            var orderId = "ord_1004";
            var order = new Order
            {
                OrderId = orderId,
                SellerId = "seller_1001"
            };

            _orderRepository.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync(order);

            var invalidSellerIds = new string?[] { null, "", "   " };

            foreach (var invalidSellerId in invalidSellerIds)
            {
                // Act
                var result = await _service.GetOrderDetailAsync(invalidSellerId!, orderId);

                // Assert
                result.Should().BeNull();
            }

            _orderRepository.Verify(r => r.GetByIdAsync(orderId), Times.Exactly(invalidSellerIds.Length));
        }
        #endregion

        #region Boundary Tests (B)
        [Fact]
        public async Task GetOrderDetailAsync_ShouldMapNullRelatedEntitiesGracefully_WhenProductBuyerSellerAndPaymentsAreNullOrEmpty()
        {
            // Arrange
            var orderId = "ord_boundary_01";
            var sellerId = "seller_1001";

            var order = new Order
            {
                OrderId = orderId,
                SellerId = sellerId,
                Product = null,
                Buyer = null,
                Seller = null,
                Payment = new List<Payment>()
            };

            _orderRepository.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync(order);

            // Act
            var result = await _service.GetOrderDetailAsync(sellerId, orderId);

            // Assert
            result.Should().NotBeNull();
            result!.OrderId.Should().Be(orderId);
            result.ProductName.Should().BeNull();
            result.ProductImageUrl.Should().BeNull();
            result.BuyerName.Should().BeNull();
            result.BuyerEmail.Should().BeNull();
            result.BuyerPhone.Should().BeNull();
            result.SellerName.Should().BeNull();
            result.SellerEmail.Should().BeNull();
            result.SellerPhone.Should().BeNull();
            result.Payments.Should().BeEmpty();

            _orderRepository.Verify(r => r.GetByIdAsync(orderId), Times.Once);
        }

        [Fact]
        public async Task GetOrderDetailAsync_ShouldResolveBuyerPhoneFromAddressSnapshot_WhenBuyerPhoneIsNull()
        {
            // Arrange
            var orderId = "ord_boundary_02";
            var sellerId = "seller_1001";

            var buyerWithoutPhone = new User
            {
                UserId = "buyer_no_phone",
                FirstName = "Le",
                LastName = "Van C",
                Phone = null
            };

            var order = new Order
            {
                OrderId = orderId,
                SellerId = sellerId,
                Buyer = buyerWithoutPhone,
                AddressSnapshot = "Le Van C - 0987654321 - 456 Le Duan, Q3",
                Payment = new List<Payment>()
            };

            _orderRepository.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync(order);

            // Act
            var result = await _service.GetOrderDetailAsync(sellerId, orderId);

            // Assert
            result.Should().NotBeNull();
            result!.BuyerPhone.Should().Be("0987654321");
            _orderRepository.Verify(r => r.GetByIdAsync(orderId), Times.Once);
        }
        #endregion
    }
}
