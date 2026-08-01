using System;
using System.Collections.Generic;
using System.Linq;
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
    public class PurchaseGetBuyerPurchasesTests
    {
        private readonly Mock<IOrderRepository> _orderRepository;
        private readonly Mock<IHubContext<OrderHub>> _orderHub;
        private readonly IMapper _mapper;
        private readonly PurchaseService _service;

        public PurchaseGetBuyerPurchasesTests()
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
        public void QueryByBuyerId_ShouldReturnFilteredOrdersInDescendingOrder_WhenBuyerIdAndStatusAreValid()
        {
            // Arrange
            var buyerId = "buyer_1";
            var orders = new List<Order>
            {
                new Order { OrderId = "order_1", BuyerId = buyerId, Status = "Pending", CreatedAt = DateTime.UtcNow.AddHours(-2) },
                new Order { OrderId = "order_2", BuyerId = buyerId, Status = "Pending", CreatedAt = DateTime.UtcNow.AddHours(-1) },
                new Order { OrderId = "order_3", BuyerId = buyerId, Status = "Completed", CreatedAt = DateTime.UtcNow },
                new Order { OrderId = "order_4", BuyerId = "other_buyer", Status = "Pending", CreatedAt = DateTime.UtcNow }
            };

            _orderRepository.Setup(r => r.Query()).Returns(orders.AsQueryable());

            // Act
            var result = _service.QueryByBuyerId(buyerId, "Pending").ToList();

            // Assert
            result.Should().HaveCount(2);
            result[0].OrderId.Should().Be("order_2"); // Mới hơn lên trước
            result[1].OrderId.Should().Be("order_1");
            result.All(x => x.Status == "Pending").Should().BeTrue();
            _orderRepository.Verify(r => r.Query(), Times.Once);
        }

        [Fact]
        public void QueryByBuyerId_ShouldMapOrderDetailsAndRelationsToPurchaseListDto_Correctly()
        {
            // Arrange
            var buyerId = "buyer_1";
            var seller = new User { UserId = "s1", FirstName = "Nguyen", LastName = "Văn A", Email = "a@test.com", Phone = "0987654321" };
            var mainImg = new Image { ImageId = "img1", ImageUrl = "http://img.com/main.png" };
            var product = new Product
            {
                ProductId = "p1",
                Name = "Laptop Pro",
                ProductImage = new List<ProductImage>
                {
                    new ProductImage { ProductId = "p1", ImageId = "img1", Image = mainImg, IsMain = true }
                }
            };
            var orders = new List<Order>
            {
                new Order
                {
                    OrderId = "order_1",
                    BuyerId = buyerId,
                    SellerId = "s1",
                    Seller = seller,
                    Product = product,
                    Status = "Completed",
                    CreatedAt = DateTime.UtcNow,
                    Review = new List<Review> { new Review { ReviewId = "r1", OrderId = "order_1" } }
                }
            };

            _orderRepository.Setup(r => r.Query()).Returns(orders.AsQueryable());

            // Act
            var result = _service.QueryByBuyerId(buyerId).ToList();

            // Assert
            result.Should().HaveCount(1);
            var dto = result.First();
            dto.OrderId.Should().Be("order_1");
            dto.ProductName.Should().Be("Laptop Pro");
            dto.ProductImageUrl.Should().Be("http://img.com/main.png");
            dto.SellerName.Should().Be("Nguyen Văn A");
            dto.SellerEmail.Should().Be("a@test.com");
            dto.SellerPhone.Should().Be("0987654321");
            dto.HasReview.Should().BeTrue();
        }

        [Fact]
        public void QueryByBuyerId_ShouldReturnEmpty_WhenBuyerHasNoOrdersOrStatusNotMatched()
        {
            // Arrange
            var buyerId = "buyer_1";
            var orders = new List<Order>
            {
                new Order { OrderId = "order_1", BuyerId = buyerId, Status = "Completed", CreatedAt = DateTime.UtcNow }
            };

            _orderRepository.Setup(r => r.Query()).Returns(orders.AsQueryable());

            // Act & Assert 1: Không đúng status
            var resultStatusUnmatched = _service.QueryByBuyerId(buyerId, "Cancelled").ToList();
            resultStatusUnmatched.Should().BeEmpty();

            // Act & Assert 2: BuyerId không tồn tại đơn
            var resultBuyerUnmatched = _service.QueryByBuyerId("unknown_buyer").ToList();
            resultBuyerUnmatched.Should().BeEmpty();
        }
        #endregion

        #region Abnormal Tests (A)
        [Fact]
        public void QueryByBuyerId_ShouldReturnEmptyQueryable_WhenBuyerIdIsInvalid()
        {
            // Arrange
            var invalidBuyerIds = new string?[] { null, "", "   " };

            foreach (var invalidBuyerId in invalidBuyerIds)
            {
                // Act
                var result = _service.QueryByBuyerId(invalidBuyerId!).ToList();

                // Assert
                result.Should().BeEmpty();
            }

            _orderRepository.Verify(r => r.Query(), Times.Never);
        }
        #endregion

        #region Boundary Tests (B)
        [Fact]
        public void QueryByBuyerId_ShouldIgnoreStatusFilter_WhenStatusIsNullOrWhitespace()
        {
            // Arrange
            var buyerId = "buyer_1";
            var emptyStatuses = new string?[] { null, "", "   " };
            var orders = new List<Order>
            {
                new Order { OrderId = "o1", BuyerId = buyerId, Status = "Completed", CreatedAt = DateTime.UtcNow.AddMinutes(-5) },
                new Order { OrderId = "o2", BuyerId = buyerId, Status = "Pending", CreatedAt = DateTime.UtcNow }
            };

            _orderRepository.Setup(r => r.Query()).Returns(orders.AsQueryable());

            foreach (var emptyStatus in emptyStatuses)
            {
                // Act
                var result = _service.QueryByBuyerId(buyerId, emptyStatus).ToList();

                // Assert
                result.Should().HaveCount(2);
                result[0].OrderId.Should().Be("o2");
                result[1].OrderId.Should().Be("o1");
            }
        }

        [Fact]
        public void QueryByBuyerId_ShouldFallbackToFirstSortedImage_WhenNoMainImageIsMarked()
        {
            // Arrange
            var buyerId = "buyer_1";
            var fallbackImg = new Image { ImageId = "img_fallback", ImageUrl = "http://img.com/fallback.png" };
            var product = new Product
            {
                ProductId = "p1",
                Name = "Product Secondary Image",
                ProductImage = new List<ProductImage>
                {
                    new ProductImage { ProductId = "p1", ImageId = "img_fallback", Image = fallbackImg, IsMain = false, SortOrder = 1 }
                }
            };
            var orders = new List<Order>
            {
                new Order { OrderId = "o1", BuyerId = buyerId, Product = product, CreatedAt = DateTime.UtcNow }
            };

            _orderRepository.Setup(r => r.Query()).Returns(orders.AsQueryable());

            // Act
            var result = _service.QueryByBuyerId(buyerId).ToList();

            // Assert
            result.Should().HaveCount(1);
            result.First().ProductImageUrl.Should().Be("http://img.com/fallback.png");
        }
        #endregion
    }
}
