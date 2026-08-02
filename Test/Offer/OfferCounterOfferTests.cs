using System;
using System.Collections.Generic;
using System.Linq;
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
using RetradeBE.Services.Checkout;
using RetradeBE.Services.Offer;
using Xunit;

namespace Test.OfferTests
{
    public class OfferCounterOfferTests
    {
        private readonly Mock<ICheckoutService> _checkoutService;
        private readonly Mock<IHubContext<OrderHub>> _orderHub;
        private readonly Mock<IOfferRepository> _repo;
        private readonly Mock<IAccountRepository> _accountRepo;
        private readonly Mock<IProductRepository> _productRepo;
        private readonly Mock<IUserRepository> _userRepo;
        private readonly Mock<IAddressRepository> _addressRepo;
        private readonly Mock<IOrderRepository> _orderRepo;
        private readonly Mock<IWishlistRepository> _wishlistRepo;
        private readonly Mock<INotificationService> _notificationService;
        private readonly IMapper _mapper;
        private readonly OfferService _service;

        public OfferCounterOfferTests()
        {
            _checkoutService = new Mock<ICheckoutService>();
            _orderHub = new Mock<IHubContext<OrderHub>>();
            _repo = new Mock<IOfferRepository>();
            _accountRepo = new Mock<IAccountRepository>();
            _productRepo = new Mock<IProductRepository>();
            _userRepo = new Mock<IUserRepository>();
            _addressRepo = new Mock<IAddressRepository>();
            _orderRepo = new Mock<IOrderRepository>();
            _wishlistRepo = new Mock<IWishlistRepository>();
            _notificationService = new Mock<INotificationService>();

            var configuration = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<AutoMapperProfile>();
            }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
            _mapper = configuration.CreateMapper();

            _service = new OfferService(
                _checkoutService.Object,
                _orderHub.Object,
                _repo.Object,
                _accountRepo.Object,
                _productRepo.Object,
                _userRepo.Object,
                _addressRepo.Object,
                _orderRepo.Object,
                _wishlistRepo.Object,
                _notificationService.Object
            );
        }

        #region Normal Tests (N)

        [Fact]
        public async Task CounterOfferAsync_ShouldUpdateOfferStatusToCounterOfferAndSendNotification_WhenValidRequestFromPending()
        {
            // Arrange
            string sellerId = "user_seller_1";
            string buyerId = "user_buyer_1";
            string offerId = "offer_123";

            var product = new Product
            {
                ProductId = "prod_1",
                Name = "Gaming Chair",
                Price = 1000000,
                SellerId = sellerId
            };

            var offer = new Offer
            {
                OfferId = offerId,
                BuyerId = buyerId,
                ProductId = "prod_1",
                Product = product,
                OfferPrice = 500000,
                Status = "Pending"
            };

            var request = new CounterOfferDto
            {
                OfferId = offerId,
                CounterPrice = 750000
            };

            _repo.Setup(x => x.GetByIdAsync(offerId)).ReturnsAsync(offer);
            _repo.Setup(x => x.UpdateAsync(offer)).Returns(Task.CompletedTask);

            // Act
            var result = await _service.CounterOfferAsync(sellerId, request);

            // Assert
            result.Should().NotBeNull();
            result.Status.Should().Be("CounterOffer");
            result.OfferPrice.Should().Be(750000);

            _repo.Verify(x => x.UpdateAsync(It.Is<Offer>(o =>
                o.Status == "CounterOffer" &&
                o.OfferPrice == 750000
            )), Times.Once);

            _notificationService.Verify(x => x.CreateAndSendAsync(It.Is<CreateNotificationDto>(n =>
                n.UserId == buyerId &&
                n.Title == "Counter Offer Received" &&
                n.Type == "Offer" &&
                n.ReferenceId == offerId
            )), Times.Once);
        }

        [Fact]
        public async Task CounterOfferAsync_ShouldUpdateCounterOffer_WhenCurrentStatusIsAlreadyCounterOffer()
        {
            // Arrange
            string sellerId = "user_seller_1";
            string buyerId = "user_buyer_1";
            string offerId = "offer_123";

            var product = new Product
            {
                ProductId = "prod_1",
                Name = "Gaming Chair",
                Price = 1000000,
                SellerId = sellerId
            };

            var offer = new Offer
            {
                OfferId = offerId,
                BuyerId = buyerId,
                ProductId = "prod_1",
                Product = product,
                OfferPrice = 600000,
                Status = "CounterOffer" // Already CounterOffer
            };

            var request = new CounterOfferDto
            {
                OfferId = offerId,
                CounterPrice = 800000
            };

            _repo.Setup(x => x.GetByIdAsync(offerId)).ReturnsAsync(offer);
            _repo.Setup(x => x.UpdateAsync(offer)).Returns(Task.CompletedTask);

            // Act
            var result = await _service.CounterOfferAsync(sellerId, request);

            // Assert
            result.Should().NotBeNull();
            result.Status.Should().Be("CounterOffer");
            result.OfferPrice.Should().Be(800000);
        }

        [Fact]
        public async Task CounterOfferAsync_ShouldMapBuyerAndProductDetailsCorrectly_WhenCounterOfferCreated()
        {
            // Arrange
            string sellerId = "user_seller_1";
            string buyerId = "user_buyer_1";
            string offerId = "offer_123";

            var buyer = new User { UserId = buyerId, FirstName = "Michael", LastName = "Jordan" };
            var product = new Product
            {
                ProductId = "prod_1",
                Name = "Sneakers",
                Price = 3000000,
                SellerId = sellerId,
                ProductImage = new List<ProductImage>
                {
                    new ProductImage { SortOrder = 1, Image = new Image { ImageUrl = "https://example.com/shoes.jpg" } }
                }
            };

            var offer = new Offer
            {
                OfferId = offerId,
                BuyerId = buyerId,
                Buyer = buyer,
                ProductId = "prod_1",
                Product = product,
                OfferPrice = 2000000,
                Status = "Pending"
            };

            var request = new CounterOfferDto
            {
                OfferId = offerId,
                CounterPrice = 2500000
            };

            _repo.Setup(x => x.GetByIdAsync(offerId)).ReturnsAsync(offer);
            _repo.Setup(x => x.UpdateAsync(offer)).Returns(Task.CompletedTask);

            // Act
            var result = await _service.CounterOfferAsync(sellerId, request);

            // Assert
            result.Should().NotBeNull();
            result.BuyerName.Should().Be("Michael Jordan");
            result.ProductName.Should().Be("Sneakers");
            result.OriginalPrice.Should().Be(3000000);
            result.OfferPrice.Should().Be(2500000);
            result.ProductImageUrl.Should().Be("https://example.com/shoes.jpg");
        }

        #endregion

        #region Abnormal Tests (A)

        [Fact]
        public async Task CounterOfferAsync_ShouldThrowException_WhenOfferNotFound()
        {
            // Arrange
            string sellerId = "user_seller_1";
            var request = new CounterOfferDto { OfferId = "invalid_offer", CounterPrice = 500000 };

            _repo.Setup(x => x.GetByIdAsync("invalid_offer")).ReturnsAsync((Offer?)null);

            // Act
            Func<Task> act = async () => await _service.CounterOfferAsync(sellerId, request);

            // Assert
            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Offer not found.");
        }

        [Fact]
        public async Task CounterOfferAsync_ShouldThrowException_WhenProductIsNull()
        {
            // Arrange
            string sellerId = "user_seller_1";
            string offerId = "offer_123";

            var offer = new Offer
            {
                OfferId = offerId,
                Product = null, // Product is null
                OfferPrice = 500000,
                Status = "Pending"
            };

            var request = new CounterOfferDto { OfferId = offerId, CounterPrice = 700000 };

            _repo.Setup(x => x.GetByIdAsync(offerId)).ReturnsAsync(offer);

            // Act
            Func<Task> act = async () => await _service.CounterOfferAsync(sellerId, request);

            // Assert
            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Product not found.");
        }

        [Fact]
        public async Task CounterOfferAsync_ShouldThrowException_WhenSellerIsNotAuthorizedToManageOffer()
        {
            // Arrange
            string sellerId = "user_seller_1";
            string offerId = "offer_123";

            var product = new Product
            {
                ProductId = "prod_1",
                SellerId = "other_seller_id" // Different seller
            };

            var offer = new Offer
            {
                OfferId = offerId,
                Product = product,
                OfferPrice = 500000,
                Status = "Pending"
            };

            var request = new CounterOfferDto { OfferId = offerId, CounterPrice = 700000 };

            _repo.Setup(x => x.GetByIdAsync(offerId)).ReturnsAsync(offer);

            // Act
            Func<Task> act = async () => await _service.CounterOfferAsync(sellerId, request);

            // Assert
            await act.Should().ThrowAsync<Exception>()
                .WithMessage("You are not authorized to manage this offer.");
        }

        [Theory]
        [InlineData("Accepted")]
        [InlineData("Rejected")]
        [InlineData("Cancelled")]
        [InlineData("Completed")]
        public async Task CounterOfferAsync_ShouldThrowException_WhenOfferStatusIsNotPendingOrCounterOffer(string status)
        {
            // Arrange
            string sellerId = "user_seller_1";
            string offerId = "offer_123";

            var product = new Product { ProductId = "prod_1", SellerId = sellerId, Price = 1000000 };
            var offer = new Offer { OfferId = offerId, Product = product, OfferPrice = 500000, Status = status };
            var request = new CounterOfferDto { OfferId = offerId, CounterPrice = 700000 };

            _repo.Setup(x => x.GetByIdAsync(offerId)).ReturnsAsync(offer);

            // Act
            Func<Task> act = async () => await _service.CounterOfferAsync(sellerId, request);

            // Assert
            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Only pending offers can be countered.");
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-50000)]
        public async Task CounterOfferAsync_ShouldThrowException_WhenCounterPriceIsZeroOrNegative(decimal counterPrice)
        {
            // Arrange
            string sellerId = "user_seller_1";
            string offerId = "offer_123";

            var product = new Product { ProductId = "prod_1", SellerId = sellerId, Price = 1000000 };
            var offer = new Offer { OfferId = offerId, Product = product, OfferPrice = 500000, Status = "Pending" };
            var request = new CounterOfferDto { OfferId = offerId, CounterPrice = counterPrice };

            _repo.Setup(x => x.GetByIdAsync(offerId)).ReturnsAsync(offer);

            // Act
            Func<Task> act = async () => await _service.CounterOfferAsync(sellerId, request);

            // Assert
            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Counter price must be greater than 0.");
        }

        [Theory]
        [InlineData(500000)]
        [InlineData(400000)]
        public async Task CounterOfferAsync_ShouldThrowException_WhenCounterPriceIsLessOrEqualToBuyerOfferPrice(decimal counterPrice)
        {
            // Arrange
            string sellerId = "user_seller_1";
            string offerId = "offer_123";

            var product = new Product { ProductId = "prod_1", SellerId = sellerId, Price = 1000000 };
            var offer = new Offer { OfferId = offerId, Product = product, OfferPrice = 500000, Status = "Pending" };
            var request = new CounterOfferDto { OfferId = offerId, CounterPrice = counterPrice };

            _repo.Setup(x => x.GetByIdAsync(offerId)).ReturnsAsync(offer);

            // Act
            Func<Task> act = async () => await _service.CounterOfferAsync(sellerId, request);

            // Assert
            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Counter offer price must be greater than the buyer's offer.");
        }

        [Theory]
        [InlineData(1000000)]
        [InlineData(1200000)]
        public async Task CounterOfferAsync_ShouldThrowException_WhenCounterPriceIsGreaterOrEqualToProductPrice(decimal counterPrice)
        {
            // Arrange
            string sellerId = "user_seller_1";
            string offerId = "offer_123";

            var product = new Product { ProductId = "prod_1", SellerId = sellerId, Price = 1000000 };
            var offer = new Offer { OfferId = offerId, Product = product, OfferPrice = 500000, Status = "Pending" };
            var request = new CounterOfferDto { OfferId = offerId, CounterPrice = counterPrice };

            _repo.Setup(x => x.GetByIdAsync(offerId)).ReturnsAsync(offer);

            // Act
            Func<Task> act = async () => await _service.CounterOfferAsync(sellerId, request);

            // Assert
            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Counter offer price must be lower than the product price.");
        }

        #endregion

        #region Boundary Tests (B)

        [Fact]
        public async Task CounterOfferAsync_ShouldCreateCounterOffer_WhenCounterPriceIsJustAboveBuyerOfferPrice()
        {
            // Arrange
            string sellerId = "user_seller_1";
            string offerId = "offer_123";

            var product = new Product { ProductId = "prod_1", SellerId = sellerId, Price = 1000000 };
            var offer = new Offer { OfferId = offerId, Product = product, OfferPrice = 500000, Status = "Pending" };
            var request = new CounterOfferDto { OfferId = offerId, CounterPrice = 500001 };

            _repo.Setup(x => x.GetByIdAsync(offerId)).ReturnsAsync(offer);
            _repo.Setup(x => x.UpdateAsync(offer)).Returns(Task.CompletedTask);

            // Act
            var result = await _service.CounterOfferAsync(sellerId, request);

            // Assert
            result.Should().NotBeNull();
            result.OfferPrice.Should().Be(500001);
            result.Status.Should().Be("CounterOffer");
        }

        [Fact]
        public async Task CounterOfferAsync_ShouldCreateCounterOffer_WhenCounterPriceIsJustBelowProductPrice()
        {
            // Arrange
            string sellerId = "user_seller_1";
            string offerId = "offer_123";

            var product = new Product { ProductId = "prod_1", SellerId = sellerId, Price = 1000000 };
            var offer = new Offer { OfferId = offerId, Product = product, OfferPrice = 500000, Status = "Pending" };
            var request = new CounterOfferDto { OfferId = offerId, CounterPrice = 999999 };

            _repo.Setup(x => x.GetByIdAsync(offerId)).ReturnsAsync(offer);
            _repo.Setup(x => x.UpdateAsync(offer)).Returns(Task.CompletedTask);

            // Act
            var result = await _service.CounterOfferAsync(sellerId, request);

            // Assert
            result.Should().NotBeNull();
            result.OfferPrice.Should().Be(999999);
            result.Status.Should().Be("CounterOffer");
        }

        [Fact]
        public async Task CounterOfferAsync_ShouldHandleProductWithNullImages_WithoutThrowingException()
        {
            // Arrange
            string sellerId = "user_seller_1";
            string offerId = "offer_123";

            var product = new Product
            {
                ProductId = "prod_1",
                Price = 1000000,
                SellerId = sellerId,
                ProductImage = new List<ProductImage>()
            };
            var offer = new Offer { OfferId = offerId, Product = product, OfferPrice = 500000, Status = "Pending" };
            var request = new CounterOfferDto { OfferId = offerId, CounterPrice = 750000 };

            _repo.Setup(x => x.GetByIdAsync(offerId)).ReturnsAsync(offer);
            _repo.Setup(x => x.UpdateAsync(offer)).Returns(Task.CompletedTask);

            // Act
            var result = await _service.CounterOfferAsync(sellerId, request);

            // Assert
            result.Should().NotBeNull();
            result.ProductImageUrl.Should().BeNull();
        }

        #endregion
    }
}
