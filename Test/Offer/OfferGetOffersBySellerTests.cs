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
    public class OfferGetOffersBySellerTests
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

        public OfferGetOffersBySellerTests()
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
        public async Task GetOffersBySellerAsync_ShouldReturnOffersSortedByCreatedAtDescending_WhenSellerHasMultipleOffers()
        {
            // Arrange
            string sellerUserId = "user_seller_1";
            var now = DateTime.UtcNow;

            var offerOlder = new Offer
            {
                OfferId = "offer_1",
                BuyerId = "user_buyer_1",
                ProductId = "prod_1",
                OfferPrice = 100000,
                CreatedAt = now.AddHours(-5)
            };
            var offerNewer = new Offer
            {
                OfferId = "offer_2",
                BuyerId = "user_buyer_2",
                ProductId = "prod_2",
                OfferPrice = 200000,
                CreatedAt = now.AddHours(-1)
            };

            _repo.Setup(x => x.GetOffersBySellerAsync(sellerUserId))
                .ReturnsAsync(new List<Offer> { offerOlder, offerNewer });

            // Act
            var result = await _service.GetOffersBySellerAsync(sellerUserId);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result[0].OfferId.Should().Be("offer_2");
            result[1].OfferId.Should().Be("offer_1");
        }

        [Fact]
        public async Task GetOffersBySellerAsync_ShouldMapMainImageWithLowestSortOrder_WhenProductHasMultipleImages()
        {
            // Arrange
            string sellerUserId = "user_seller_1";
            var product = new Product
            {
                ProductId = "prod_1",
                Name = "Laptop",
                ProductImage = new List<ProductImage>
                {
                    new ProductImage { SortOrder = 10, Image = new Image { ImageUrl = "https://example.com/img2.jpg" } },
                    new ProductImage { SortOrder = 2, Image = new Image { ImageUrl = "https://example.com/main.jpg" } }
                }
            };
            var offer = new Offer
            {
                OfferId = "offer_1",
                Product = product,
                CreatedAt = DateTime.UtcNow
            };

            _repo.Setup(x => x.GetOffersBySellerAsync(sellerUserId))
                .ReturnsAsync(new List<Offer> { offer });

            // Act
            var result = await _service.GetOffersBySellerAsync(sellerUserId);

            // Assert
            result.Should().NotBeNull();
            result.First().ProductImageUrl.Should().Be("https://example.com/main.jpg");
        }

        [Fact]
        public async Task GetOffersBySellerAsync_ShouldMapBuyerName_WhenBuyerHasFirstNameAndLastName()
        {
            // Arrange
            string sellerUserId = "user_seller_1";
            var buyer = new User { UserId = "user_buyer_1", FirstName = "John", LastName = "Wick" };
            var offer = new Offer
            {
                OfferId = "offer_1",
                BuyerId = "user_buyer_1",
                Buyer = buyer,
                CreatedAt = DateTime.UtcNow
            };

            _repo.Setup(x => x.GetOffersBySellerAsync(sellerUserId))
                .ReturnsAsync(new List<Offer> { offer });

            // Act
            var result = await _service.GetOffersBySellerAsync(sellerUserId);

            // Assert
            result.Should().NotBeNull();
            result.First().BuyerName.Should().Be("John Wick");
        }

        [Fact]
        public async Task GetOffersBySellerAsync_ShouldMapBuyerNameAsNull_WhenBuyerUserIsNull()
        {
            // Arrange
            string sellerUserId = "user_seller_1";
            var offer = new Offer
            {
                OfferId = "offer_1",
                BuyerId = "user_buyer_1",
                Buyer = null,
                CreatedAt = DateTime.UtcNow
            };

            _repo.Setup(x => x.GetOffersBySellerAsync(sellerUserId))
                .ReturnsAsync(new List<Offer> { offer });

            // Act
            var result = await _service.GetOffersBySellerAsync(sellerUserId);

            // Assert
            result.Should().NotBeNull();
            result.First().BuyerName.Should().BeNull();
        }

        [Fact]
        public async Task GetOffersBySellerAsync_ShouldReturnEmptyList_WhenSellerHasNoOffers()
        {
            // Arrange
            string sellerUserId = "user_seller_1";
            _repo.Setup(x => x.GetOffersBySellerAsync(sellerUserId))
                .ReturnsAsync(new List<Offer>());

            // Act
            var result = await _service.GetOffersBySellerAsync(sellerUserId);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetOffersBySellerAsync_ShouldMapOfferPropertiesCorrectly_WhenValidOfferProvided()
        {
            // Arrange
            string sellerUserId = "user_seller_1";
            var createdTime = DateTime.UtcNow;
            var expireTime = createdTime.AddDays(2);
            var product = new Product { ProductId = "prod_100", Name = "Gaming Console", Price = 5000000 };
            var buyer = new User { UserId = "buyer_100", FirstName = "David", LastName = "Beckham" };

            var offer = new Offer
            {
                OfferId = "offer_100",
                BuyerId = "buyer_100",
                Buyer = buyer,
                ProductId = "prod_100",
                Product = product,
                OfferPrice = 4500000,
                Message = "Negotiable?",
                ExpiresAt = expireTime,
                Status = "Pending",
                CreatedAt = createdTime
            };

            _repo.Setup(x => x.GetOffersBySellerAsync(sellerUserId))
                .ReturnsAsync(new List<Offer> { offer });

            // Act
            var result = await _service.GetOffersBySellerAsync(sellerUserId);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(1);

            var item = result.First();
            item.OfferId.Should().Be("offer_100");
            item.BuyerId.Should().Be("buyer_100");
            item.BuyerName.Should().Be("David Beckham");
            item.ProductId.Should().Be("prod_100");
            item.ProductName.Should().Be("Gaming Console");
            item.OriginalPrice.Should().Be(5000000);
            item.OfferPrice.Should().Be(4500000);
            item.Message.Should().Be("Negotiable?");
            item.ExpiresAt.Should().Be(expireTime);
            item.Status.Should().Be("Pending");
            item.CreatedAt.Should().Be(createdTime);
        }

        #endregion

        #region Abnormal Tests (A)

        [Fact]
        public async Task GetOffersBySellerAsync_ShouldReturnEmptyList_WhenSellerUserIdDoesNotExist()
        {
            // Arrange
            string sellerUserId = "non_existing_seller_id";
            _repo.Setup(x => x.GetOffersBySellerAsync(sellerUserId))
                .ReturnsAsync(new List<Offer>());

            // Act
            var result = await _service.GetOffersBySellerAsync(sellerUserId);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetOffersBySellerAsync_ShouldHandleOfferWithNullProduct_WithoutThrowingException()
        {
            // Arrange
            string sellerUserId = "user_seller_1";
            var offer = new Offer
            {
                OfferId = "offer_1",
                BuyerId = "user_buyer_1",
                Product = null,
                OfferPrice = 300000,
                CreatedAt = DateTime.UtcNow
            };

            _repo.Setup(x => x.GetOffersBySellerAsync(sellerUserId))
                .ReturnsAsync(new List<Offer> { offer });

            // Act
            var result = await _service.GetOffersBySellerAsync(sellerUserId);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            result.First().ProductName.Should().BeNull();
            result.First().OriginalPrice.Should().BeNull();
            result.First().ProductImageUrl.Should().BeNull();
        }

        [Fact]
        public async Task GetOffersBySellerAsync_ShouldHandleProductWithNullImages_WithoutThrowingException()
        {
            // Arrange
            string sellerUserId = "user_seller_1";
            var product = new Product
            {
                ProductId = "prod_1",
                Name = "Desk",
                ProductImage = new List<ProductImage>()
            };
            var offer = new Offer
            {
                OfferId = "offer_1",
                Product = product,
                CreatedAt = DateTime.UtcNow
            };

            _repo.Setup(x => x.GetOffersBySellerAsync(sellerUserId))
                .ReturnsAsync(new List<Offer> { offer });

            // Act
            var result = await _service.GetOffersBySellerAsync(sellerUserId);

            // Assert
            result.Should().NotBeNull();
            result.First().ProductImageUrl.Should().BeNull();
        }

        #endregion

        #region Boundary Tests (B)

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task GetOffersBySellerAsync_ShouldReturnEmptyList_WhenSellerUserIdIsNullOrWhitespace(string? sellerUserId)
        {
            // Arrange
            _repo.Setup(x => x.GetOffersBySellerAsync(It.IsAny<string>()))
                .ReturnsAsync(new List<Offer>());

            // Act
            var result = await _service.GetOffersBySellerAsync(sellerUserId!);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetOffersBySellerAsync_ShouldHandleProductImageWithNullImageReference_WithoutThrowingException()
        {
            // Arrange
            string sellerUserId = "user_seller_1";
            var product = new Product
            {
                ProductId = "prod_1",
                Name = "Chair",
                ProductImage = new List<ProductImage>
                {
                    new ProductImage { SortOrder = 1, Image = null }
                }
            };
            var offer = new Offer
            {
                OfferId = "offer_1",
                Product = product,
                CreatedAt = DateTime.UtcNow
            };

            _repo.Setup(x => x.GetOffersBySellerAsync(sellerUserId))
                .ReturnsAsync(new List<Offer> { offer });

            // Act
            var result = await _service.GetOffersBySellerAsync(sellerUserId);

            // Assert
            result.Should().NotBeNull();
            result.First().ProductImageUrl.Should().BeNull();
        }

        #endregion
    }
}
