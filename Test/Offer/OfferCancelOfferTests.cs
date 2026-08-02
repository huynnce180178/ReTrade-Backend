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
    public class OfferCancelOfferTests
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

        public OfferCancelOfferTests()
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
        public async Task CancelOfferAsync_ShouldCancelOfferAndReturnDto_WhenValidBuyerAndPendingOffer()
        {
            // Arrange
            string buyerUserId = "user_buyer_1";
            string offerId = "offer_123";

            var offer = new Offer
            {
                OfferId = offerId,
                BuyerId = buyerUserId,
                OfferPrice = 500000,
                Status = "Pending"
            };

            _repo.Setup(x => x.GetByIdAsync(offerId)).ReturnsAsync(offer);
            _repo.Setup(x => x.UpdateAsync(offer)).Returns(Task.CompletedTask);

            // Act
            var result = await _service.CancelOfferAsync(buyerUserId, offerId);

            // Assert
            result.Should().NotBeNull();
            result.Status.Should().Be("Cancelled");

            _repo.Verify(x => x.UpdateAsync(It.Is<Offer>(o =>
                o.OfferId == offerId &&
                o.Status == "Cancelled"
            )), Times.Once);
        }

        [Fact]
        public async Task CancelOfferAsync_ShouldMapBuyerAndProductDetailsCorrectly_WhenOfferCancelled()
        {
            // Arrange
            string buyerUserId = "user_buyer_1";
            string offerId = "offer_123";

            var buyer = new User { UserId = buyerUserId, FirstName = "Sarah", LastName = "Connor" };
            var product = new Product
            {
                ProductId = "prod_1",
                Name = "Vintage Camera",
                Price = 2000000,
                ProductImage = new List<ProductImage>
                {
                    new ProductImage { SortOrder = 1, Image = new Image { ImageUrl = "https://example.com/cam.jpg" } }
                }
            };

            var offer = new Offer
            {
                OfferId = offerId,
                BuyerId = buyerUserId,
                Buyer = buyer,
                ProductId = "prod_1",
                Product = product,
                OfferPrice = 1800000,
                Message = "Testing cancel",
                Status = "Pending"
            };

            _repo.Setup(x => x.GetByIdAsync(offerId)).ReturnsAsync(offer);
            _repo.Setup(x => x.UpdateAsync(offer)).Returns(Task.CompletedTask);

            // Act
            var result = await _service.CancelOfferAsync(buyerUserId, offerId);

            // Assert
            result.Should().NotBeNull();
            result.BuyerName.Should().Be("Sarah Connor");
            result.ProductName.Should().Be("Vintage Camera");
            result.OriginalPrice.Should().Be(2000000);
            result.OfferPrice.Should().Be(1800000);
            result.ProductImageUrl.Should().Be("https://example.com/cam.jpg");
            result.Status.Should().Be("Cancelled");
        }

        #endregion

        #region Abnormal Tests (A)

        [Fact]
        public async Task CancelOfferAsync_ShouldThrowException_WhenOfferNotFound()
        {
            // Arrange
            string buyerUserId = "user_buyer_1";
            string offerId = "invalid_offer_id";

            _repo.Setup(x => x.GetByIdAsync(offerId)).ReturnsAsync((Offer?)null);

            // Act
            Func<Task> act = async () => await _service.CancelOfferAsync(buyerUserId, offerId);

            // Assert
            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Offer not found.");
        }

        [Fact]
        public async Task CancelOfferAsync_ShouldThrowException_WhenUserIsNotTheBuyerOfOffer()
        {
            // Arrange
            string buyerUserId = "user_buyer_1";
            string offerId = "offer_123";

            var offer = new Offer
            {
                OfferId = offerId,
                BuyerId = "other_buyer_id", // Different buyer
                Status = "Pending"
            };

            _repo.Setup(x => x.GetByIdAsync(offerId)).ReturnsAsync(offer);

            // Act
            Func<Task> act = async () => await _service.CancelOfferAsync(buyerUserId, offerId);

            // Assert
            await act.Should().ThrowAsync<Exception>()
                .WithMessage("You are not authorized to cancel this offer.");
        }

        [Theory]
        [InlineData("Accepted")]
        [InlineData("Rejected")]
        [InlineData("Cancelled")]
        [InlineData("Completed")]
        [InlineData("CounterOffer")]
        public async Task CancelOfferAsync_ShouldThrowException_WhenOfferStatusIsNotPending(string status)
        {
            // Arrange
            string buyerUserId = "user_buyer_1";
            string offerId = "offer_123";

            var offer = new Offer
            {
                OfferId = offerId,
                BuyerId = buyerUserId,
                Status = status
            };

            _repo.Setup(x => x.GetByIdAsync(offerId)).ReturnsAsync(offer);

            // Act
            Func<Task> act = async () => await _service.CancelOfferAsync(buyerUserId, offerId);

            // Assert
            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Only pending offers can be cancelled.");
        }

        #endregion

        #region Boundary Tests (B)

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task CancelOfferAsync_ShouldThrowException_WhenBuyerUserIdIsNullOrWhitespace(string? buyerUserId)
        {
            // Arrange
            string offerId = "offer_123";
            var offer = new Offer { OfferId = offerId, BuyerId = "actual_buyer", Status = "Pending" };

            _repo.Setup(x => x.GetByIdAsync(offerId)).ReturnsAsync(offer);

            // Act
            Func<Task> act = async () => await _service.CancelOfferAsync(buyerUserId!, offerId);

            // Assert
            await act.Should().ThrowAsync<Exception>()
                .WithMessage("You are not authorized to cancel this offer.");
        }

        [Fact]
        public async Task CancelOfferAsync_ShouldHandleProductWithNullImages_WithoutThrowingException()
        {
            // Arrange
            string buyerUserId = "user_buyer_1";
            string offerId = "offer_123";

            var product = new Product
            {
                ProductId = "prod_1",
                Name = "Desk",
                ProductImage = new List<ProductImage>()
            };

            var offer = new Offer
            {
                OfferId = offerId,
                BuyerId = buyerUserId,
                Product = product,
                Status = "Pending"
            };

            _repo.Setup(x => x.GetByIdAsync(offerId)).ReturnsAsync(offer);
            _repo.Setup(x => x.UpdateAsync(offer)).Returns(Task.CompletedTask);

            // Act
            var result = await _service.CancelOfferAsync(buyerUserId, offerId);

            // Assert
            result.Should().NotBeNull();
            result.Status.Should().Be("Cancelled");
            result.ProductImageUrl.Should().BeNull();
        }

        [Fact]
        public async Task CancelOfferAsync_ShouldHandleOfferWithNullProduct_WithoutThrowingException()
        {
            // Arrange
            string buyerUserId = "user_buyer_1";
            string offerId = "offer_123";

            var offer = new Offer
            {
                OfferId = offerId,
                BuyerId = buyerUserId,
                Product = null, // Product is null
                Status = "Pending"
            };

            _repo.Setup(x => x.GetByIdAsync(offerId)).ReturnsAsync(offer);
            _repo.Setup(x => x.UpdateAsync(offer)).Returns(Task.CompletedTask);

            // Act
            var result = await _service.CancelOfferAsync(buyerUserId, offerId);

            // Assert
            result.Should().NotBeNull();
            result.Status.Should().Be("Cancelled");
            result.ProductName.Should().BeNull();
            result.ProductImageUrl.Should().BeNull();
        }

        [Fact]
        public async Task CancelOfferAsync_ShouldHandleOfferWithNullBuyer_WithoutThrowingException()
        {
            // Arrange
            string buyerUserId = "user_buyer_1";
            string offerId = "offer_123";

            var offer = new Offer
            {
                OfferId = offerId,
                BuyerId = buyerUserId,
                Buyer = null, // Buyer is null
                Status = "Pending"
            };

            _repo.Setup(x => x.GetByIdAsync(offerId)).ReturnsAsync(offer);
            _repo.Setup(x => x.UpdateAsync(offer)).Returns(Task.CompletedTask);

            // Act
            var result = await _service.CancelOfferAsync(buyerUserId, offerId);

            // Assert
            result.Should().NotBeNull();
            result.Status.Should().Be("Cancelled");
            result.BuyerName.Should().BeNull();
        }

        #endregion
    }
}
