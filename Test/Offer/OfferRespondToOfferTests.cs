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
    public class OfferRespondToOfferTests
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

        public OfferRespondToOfferTests()
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
        public async Task RespondToOfferAsync_ShouldAcceptOfferAndSendNotification_WhenAcceptIsTrue()
        {
            // Arrange
            string sellerId = "user_seller_1";
            string buyerId = "user_buyer_1";
            string offerId = "offer_123";

            var product = new Product
            {
                ProductId = "prod_1",
                Name = "Mechanical Keyboard",
                SellerId = sellerId
            };

            var offer = new Offer
            {
                OfferId = offerId,
                BuyerId = buyerId,
                ProductId = "prod_1",
                Product = product,
                OfferPrice = 800000,
                Status = "Pending"
            };

            _repo.Setup(x => x.GetByIdAsync(offerId)).ReturnsAsync(offer);
            _repo.Setup(x => x.UpdateAsync(offer)).Returns(Task.CompletedTask);

            // Act
            var result = await _service.RespondToOfferAsync(sellerId, offerId, accept: true);

            // Assert
            result.Should().NotBeNull();
            result.Status.Should().Be("Accepted");

            _repo.Verify(x => x.UpdateAsync(It.Is<Offer>(o => o.Status == "Accepted")), Times.Once);
            _notificationService.Verify(x => x.CreateAndSendAsync(It.Is<CreateNotificationDto>(n =>
                n.UserId == buyerId &&
                n.Title == "Offer Accepted" &&
                n.Type == "Offer" &&
                n.ReferenceId == offerId
            )), Times.Once);
        }

        [Fact]
        public async Task RespondToOfferAsync_ShouldRejectOfferAndSendNotification_WhenAcceptIsFalse()
        {
            // Arrange
            string sellerId = "user_seller_1";
            string buyerId = "user_buyer_1";
            string offerId = "offer_123";

            var product = new Product
            {
                ProductId = "prod_1",
                Name = "Mechanical Keyboard",
                SellerId = sellerId
            };

            var offer = new Offer
            {
                OfferId = offerId,
                BuyerId = buyerId,
                ProductId = "prod_1",
                Product = product,
                OfferPrice = 800000,
                Status = "Pending"
            };

            _repo.Setup(x => x.GetByIdAsync(offerId)).ReturnsAsync(offer);
            _repo.Setup(x => x.UpdateAsync(offer)).Returns(Task.CompletedTask);

            // Act
            var result = await _service.RespondToOfferAsync(sellerId, offerId, accept: false);

            // Assert
            result.Should().NotBeNull();
            result.Status.Should().Be("Rejected");

            _repo.Verify(x => x.UpdateAsync(It.Is<Offer>(o => o.Status == "Rejected")), Times.Once);
            _notificationService.Verify(x => x.CreateAndSendAsync(It.Is<CreateNotificationDto>(n =>
                n.UserId == buyerId &&
                n.Title == "Offer Rejected" &&
                n.Type == "Offer" &&
                n.ReferenceId == offerId
            )), Times.Once);
        }

        [Fact]
        public async Task RespondToOfferAsync_ShouldMapBuyerAndProductDetailsCorrectly_WhenOfferAccepted()
        {
            // Arrange
            string sellerId = "user_seller_1";
            string buyerId = "user_buyer_1";
            string offerId = "offer_123";

            var buyer = new User { UserId = buyerId, FirstName = "Emma", LastName = "Watson" };
            var product = new Product
            {
                ProductId = "prod_1",
                Name = "Headphones",
                Price = 2000000,
                SellerId = sellerId,
                ProductImage = new List<ProductImage>
                {
                    new ProductImage { SortOrder = 1, Image = new Image { ImageUrl = "https://example.com/hp.jpg" } }
                }
            };

            var offer = new Offer
            {
                OfferId = offerId,
                BuyerId = buyerId,
                Buyer = buyer,
                ProductId = "prod_1",
                Product = product,
                OfferPrice = 1500000,
                Message = "Best price",
                Status = "Pending"
            };

            _repo.Setup(x => x.GetByIdAsync(offerId)).ReturnsAsync(offer);
            _repo.Setup(x => x.UpdateAsync(offer)).Returns(Task.CompletedTask);

            // Act
            var result = await _service.RespondToOfferAsync(sellerId, offerId, accept: true);

            // Assert
            result.Should().NotBeNull();
            result.BuyerName.Should().Be("Emma Watson");
            result.ProductName.Should().Be("Headphones");
            result.OriginalPrice.Should().Be(2000000);
            result.OfferPrice.Should().Be(1500000);
            result.ProductImageUrl.Should().Be("https://example.com/hp.jpg");
        }

        #endregion

        #region Abnormal Tests (A)

        [Fact]
        public async Task RespondToOfferAsync_ShouldThrowException_WhenOfferNotFound()
        {
            // Arrange
            string sellerId = "user_seller_1";
            string offerId = "invalid_offer_id";

            _repo.Setup(x => x.GetByIdAsync(offerId)).ReturnsAsync((Offer?)null);

            // Act
            Func<Task> act = async () => await _service.RespondToOfferAsync(sellerId, offerId, accept: true);

            // Assert
            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Offer not found.");
        }

        [Fact]
        public async Task RespondToOfferAsync_ShouldThrowException_WhenSellerIsNotAuthorizedToManageOffer()
        {
            // Arrange
            string sellerId = "user_seller_1";
            string offerId = "offer_123";

            var product = new Product
            {
                ProductId = "prod_1",
                SellerId = "other_seller_id" // Not sellerId
            };

            var offer = new Offer
            {
                OfferId = offerId,
                Product = product,
                Status = "Pending"
            };

            _repo.Setup(x => x.GetByIdAsync(offerId)).ReturnsAsync(offer);

            // Act
            Func<Task> act = async () => await _service.RespondToOfferAsync(sellerId, offerId, accept: true);

            // Assert
            await act.Should().ThrowAsync<Exception>()
                .WithMessage("You are not authorized to manage this offer.");
        }

        [Fact]
        public async Task RespondToOfferAsync_ShouldThrowException_WhenOfferProductIsNull()
        {
            // Arrange
            string sellerId = "user_seller_1";
            string offerId = "offer_123";

            var offer = new Offer
            {
                OfferId = offerId,
                Product = null, // Product is null
                Status = "Pending"
            };

            _repo.Setup(x => x.GetByIdAsync(offerId)).ReturnsAsync(offer);

            // Act
            Func<Task> act = async () => await _service.RespondToOfferAsync(sellerId, offerId, accept: true);

            // Assert
            await act.Should().ThrowAsync<Exception>()
                .WithMessage("You are not authorized to manage this offer.");
        }

        [Theory]
        [InlineData("Accepted")]
        [InlineData("Rejected")]
        [InlineData("Cancelled")]
        [InlineData("Completed")]
        public async Task RespondToOfferAsync_ShouldThrowException_WhenOfferStatusIsNotPending(string status)
        {
            // Arrange
            string sellerId = "user_seller_1";
            string offerId = "offer_123";

            var product = new Product
            {
                ProductId = "prod_1",
                SellerId = sellerId
            };

            var offer = new Offer
            {
                OfferId = offerId,
                Product = product,
                Status = status
            };

            _repo.Setup(x => x.GetByIdAsync(offerId)).ReturnsAsync(offer);

            // Act
            Func<Task> act = async () => await _service.RespondToOfferAsync(sellerId, offerId, accept: true);

            // Assert
            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Only pending offers can be accepted or rejected.");
        }

        #endregion

        #region Boundary Tests (B)

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task RespondToOfferAsync_ShouldThrowException_WhenSellerIdIsNullOrWhitespace(string? sellerId)
        {
            // Arrange
            string offerId = "offer_123";
            var product = new Product { ProductId = "prod_1", SellerId = "actual_seller" };
            var offer = new Offer { OfferId = offerId, Product = product, Status = "Pending" };

            _repo.Setup(x => x.GetByIdAsync(offerId)).ReturnsAsync(offer);

            // Act
            Func<Task> act = async () => await _service.RespondToOfferAsync(sellerId!, offerId, accept: true);

            // Assert
            await act.Should().ThrowAsync<Exception>()
                .WithMessage("You are not authorized to manage this offer.");
        }

        [Fact]
        public async Task RespondToOfferAsync_ShouldHandleProductWithNullImages_WithoutThrowingException()
        {
            // Arrange
            string sellerId = "user_seller_1";
            string offerId = "offer_123";

            var product = new Product
            {
                ProductId = "prod_1",
                SellerId = sellerId,
                ProductImage = new List<ProductImage>()
            };

            var offer = new Offer
            {
                OfferId = offerId,
                Product = product,
                Status = "Pending"
            };

            _repo.Setup(x => x.GetByIdAsync(offerId)).ReturnsAsync(offer);
            _repo.Setup(x => x.UpdateAsync(offer)).Returns(Task.CompletedTask);

            // Act
            var result = await _service.RespondToOfferAsync(sellerId, offerId, accept: true);

            // Assert
            result.Should().NotBeNull();
            result.ProductImageUrl.Should().BeNull();
        }

        [Fact]
        public async Task RespondToOfferAsync_ShouldHandleBuyerWithNullUser_WithoutThrowingException()
        {
            // Arrange
            string sellerId = "user_seller_1";
            string offerId = "offer_123";

            var product = new Product
            {
                ProductId = "prod_1",
                SellerId = sellerId
            };

            var offer = new Offer
            {
                OfferId = offerId,
                Buyer = null, // Null Buyer
                Product = product,
                Status = "Pending"
            };

            _repo.Setup(x => x.GetByIdAsync(offerId)).ReturnsAsync(offer);
            _repo.Setup(x => x.UpdateAsync(offer)).Returns(Task.CompletedTask);

            // Act
            var result = await _service.RespondToOfferAsync(sellerId, offerId, accept: true);

            // Assert
            result.Should().NotBeNull();
            result.BuyerName.Should().BeNull();
        }

        #endregion
    }
}
