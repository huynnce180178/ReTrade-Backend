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
    public class OfferGetMyOffersTests
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

        public OfferGetMyOffersTests()
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
        public async Task GetMyOffersAsync_ShouldReturnOffersOrderedByCreatedAtDescending_WhenAccountHasOffers()
        {
            // Arrange
            string accountId = "acc_buyer_1";
            string buyerUserId = "user_buyer_1";
            var now = DateTime.UtcNow;

            var account = new Account { AccountId = accountId, UserId = buyerUserId };
            _accountRepo.Setup(x => x.Query()).Returns(new List<Account> { account }.AsAsyncQueryable());

            var offerOlder = new Offer
            {
                OfferId = "offer_1",
                BuyerId = buyerUserId,
                ProductId = "prod_1",
                OfferPrice = 100000,
                CreatedAt = now.AddHours(-5)
            };
            var offerNewer = new Offer
            {
                OfferId = "offer_2",
                BuyerId = buyerUserId,
                ProductId = "prod_2",
                OfferPrice = 200000,
                CreatedAt = now.AddHours(-1)
            };

            _repo.Setup(x => x.Query()).Returns(new List<Offer> { offerOlder, offerNewer }.AsAsyncQueryable());

            // Act
            var result = await _service.GetMyOffersAsync(accountId);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result[0].OfferId.Should().Be("offer_2"); // Ordered by CreatedAt desc
            result[1].OfferId.Should().Be("offer_1");
        }

        [Fact]
        public async Task GetMyOffersAsync_ShouldFilterByProductId_WhenProductIdIsProvided()
        {
            // Arrange
            string accountId = "acc_buyer_1";
            string buyerUserId = "user_buyer_1";
            string targetProductId = "prod_target";

            var account = new Account { AccountId = accountId, UserId = buyerUserId };
            _accountRepo.Setup(x => x.Query()).Returns(new List<Account> { account }.AsAsyncQueryable());

            var offer1 = new Offer { OfferId = "offer_1", BuyerId = buyerUserId, ProductId = targetProductId, CreatedAt = DateTime.UtcNow };
            var offer2 = new Offer { OfferId = "offer_2", BuyerId = buyerUserId, ProductId = "prod_other", CreatedAt = DateTime.UtcNow };

            _repo.Setup(x => x.Query()).Returns(new List<Offer> { offer1, offer2 }.AsAsyncQueryable());

            // Act
            var result = await _service.GetMyOffersAsync(accountId, targetProductId);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            result.First().OfferId.Should().Be("offer_1");
            result.First().ProductId.Should().Be(targetProductId);
        }

        [Fact]
        public async Task GetMyOffersAsync_ShouldMapBuyerNameAndProductNameAndMainImage_WhenOffersExist()
        {
            // Arrange
            string accountId = "acc_buyer_1";
            string buyerUserId = "user_buyer_1";

            var account = new Account { AccountId = accountId, UserId = buyerUserId };
            _accountRepo.Setup(x => x.Query()).Returns(new List<Account> { account }.AsAsyncQueryable());

            var buyerUser = new User { UserId = buyerUserId, FirstName = "Alice", LastName = "Wonderland" };
            var product = new Product
            {
                ProductId = "prod_1",
                Name = "Smart Watch",
                Price = 1500000,
                ProductImage = new List<ProductImage>
                {
                    new ProductImage { SortOrder = 1, Image = new Image { ImageUrl = "https://example.com/watch.jpg" } }
                }
            };

            var offer = new Offer
            {
                OfferId = "offer_1",
                BuyerId = buyerUserId,
                Buyer = buyerUser,
                ProductId = "prod_1",
                Product = product,
                OfferPrice = 1200000,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            _repo.Setup(x => x.Query()).Returns(new List<Offer> { offer }.AsAsyncQueryable());

            // Act
            var result = await _service.GetMyOffersAsync(accountId);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            var item = result.First();
            item.BuyerName.Should().Be("Alice Wonderland");
            item.ProductName.Should().Be("Smart Watch");
            item.OriginalPrice.Should().Be(1500000);
            item.OfferPrice.Should().Be(1200000);
            item.ProductImageUrl.Should().Be("https://example.com/watch.jpg");
        }

        [Fact]
        public async Task GetMyOffersAsync_ShouldReturnEmptyList_WhenAccountHasNoOffers()
        {
            // Arrange
            string accountId = "acc_buyer_1";
            string buyerUserId = "user_buyer_1";

            var account = new Account { AccountId = accountId, UserId = buyerUserId };
            _accountRepo.Setup(x => x.Query()).Returns(new List<Account> { account }.AsAsyncQueryable());
            _repo.Setup(x => x.Query()).Returns(new List<Offer>().AsAsyncQueryable());

            // Act
            var result = await _service.GetMyOffersAsync(accountId);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetMyOffersAsync_ShouldMapOffersWithVariousStatuses_WhenBuyerHasAcceptedAndPendingAndRejectedOffers()
        {
            // Arrange
            string accountId = "acc_buyer_1";
            string buyerUserId = "user_buyer_1";

            var account = new Account { AccountId = accountId, UserId = buyerUserId };
            _accountRepo.Setup(x => x.Query()).Returns(new List<Account> { account }.AsAsyncQueryable());

            var offer1 = new Offer { OfferId = "o1", BuyerId = buyerUserId, Status = "Pending", CreatedAt = DateTime.UtcNow.AddHours(-3) };
            var offer2 = new Offer { OfferId = "o2", BuyerId = buyerUserId, Status = "Accepted", CreatedAt = DateTime.UtcNow.AddHours(-2) };
            var offer3 = new Offer { OfferId = "o3", BuyerId = buyerUserId, Status = "Rejected", CreatedAt = DateTime.UtcNow.AddHours(-1) };

            _repo.Setup(x => x.Query()).Returns(new List<Offer> { offer1, offer2, offer3 }.AsAsyncQueryable());

            // Act
            var result = await _service.GetMyOffersAsync(accountId);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(3);
            result[0].Status.Should().Be("Rejected");
            result[1].Status.Should().Be("Accepted");
            result[2].Status.Should().Be("Pending");
        }

        #endregion

        #region Abnormal Tests (A)

        [Fact]
        public async Task GetMyOffersAsync_ShouldReturnEmptyList_WhenAccountNotFound()
        {
            // Arrange
            string accountId = "invalid_acc";
            _accountRepo.Setup(x => x.Query()).Returns(new List<Account>().AsAsyncQueryable());

            // Act
            var result = await _service.GetMyOffersAsync(accountId);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetMyOffersAsync_ShouldReturnEmptyList_WhenAccountUserIdIsNull()
        {
            // Arrange
            string accountId = "acc_unlinked";
            var account = new Account { AccountId = accountId, UserId = null };
            _accountRepo.Setup(x => x.Query()).Returns(new List<Account> { account }.AsAsyncQueryable());

            // Act
            var result = await _service.GetMyOffersAsync(accountId);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetMyOffersAsync_ShouldReturnEmptyList_WhenAccountUserIdIsEmpty()
        {
            // Arrange
            string accountId = "acc_empty_user";
            var account = new Account { AccountId = accountId, UserId = "" };
            _accountRepo.Setup(x => x.Query()).Returns(new List<Account> { account }.AsAsyncQueryable());

            // Act
            var result = await _service.GetMyOffersAsync(accountId);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetMyOffersAsync_ShouldHandleOfferWithNullProduct_WithoutThrowingException()
        {
            // Arrange
            string accountId = "acc_buyer_1";
            string buyerUserId = "user_buyer_1";

            var account = new Account { AccountId = accountId, UserId = buyerUserId };
            _accountRepo.Setup(x => x.Query()).Returns(new List<Account> { account }.AsAsyncQueryable());

            var offerWithNullProduct = new Offer
            {
                OfferId = "offer_null_prod",
                BuyerId = buyerUserId,
                Product = null,
                OfferPrice = 100000,
                CreatedAt = DateTime.UtcNow
            };

            _repo.Setup(x => x.Query()).Returns(new List<Offer> { offerWithNullProduct }.AsAsyncQueryable());

            // Act
            var result = await _service.GetMyOffersAsync(accountId);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            result.First().ProductName.Should().BeNull();
            result.First().ProductImageUrl.Should().BeNull();
        }

        [Fact]
        public async Task GetMyOffersAsync_ShouldHandleOfferProductWithNullImages_WithoutThrowingException()
        {
            // Arrange
            string accountId = "acc_buyer_1";
            string buyerUserId = "user_buyer_1";

            var account = new Account { AccountId = accountId, UserId = buyerUserId };
            _accountRepo.Setup(x => x.Query()).Returns(new List<Account> { account }.AsAsyncQueryable());

            var product = new Product
            {
                ProductId = "prod_1",
                Name = "Desk",
                ProductImage = new List<ProductImage>()
            };

            var offer = new Offer
            {
                OfferId = "offer_1",
                BuyerId = buyerUserId,
                Product = product,
                CreatedAt = DateTime.UtcNow
            };

            _repo.Setup(x => x.Query()).Returns(new List<Offer> { offer }.AsAsyncQueryable());

            // Act
            var result = await _service.GetMyOffersAsync(accountId);

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
        public async Task GetMyOffersAsync_ShouldReturnAllOffers_WhenProductIdIsNullOrWhitespace(string? productId)
        {
            // Arrange
            string accountId = "acc_buyer_1";
            string buyerUserId = "user_buyer_1";

            var account = new Account { AccountId = accountId, UserId = buyerUserId };
            _accountRepo.Setup(x => x.Query()).Returns(new List<Account> { account }.AsAsyncQueryable());

            var offer1 = new Offer { OfferId = "offer_1", BuyerId = buyerUserId, ProductId = "prod_1", CreatedAt = DateTime.UtcNow };
            var offer2 = new Offer { OfferId = "offer_2", BuyerId = buyerUserId, ProductId = "prod_2", CreatedAt = DateTime.UtcNow };

            _repo.Setup(x => x.Query()).Returns(new List<Offer> { offer1, offer2 }.AsAsyncQueryable());

            // Act
            var result = await _service.GetMyOffersAsync(accountId, productId);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetMyOffersAsync_ShouldMapMainImageWithLowestSortOrder_WhenProductHasMultipleSortedImages()
        {
            // Arrange
            string accountId = "acc_buyer_1";
            string buyerUserId = "user_buyer_1";

            var account = new Account { AccountId = accountId, UserId = buyerUserId };
            _accountRepo.Setup(x => x.Query()).Returns(new List<Account> { account }.AsAsyncQueryable());

            var product = new Product
            {
                ProductId = "prod_1",
                Name = "Sorted Item",
                ProductImage = new List<ProductImage>
                {
                    new ProductImage { SortOrder = 5, Image = new Image { ImageUrl = "https://example.com/image5.jpg" } },
                    new ProductImage { SortOrder = 1, Image = new Image { ImageUrl = "https://example.com/image1.jpg" } }
                }
            };

            var offer = new Offer
            {
                OfferId = "offer_1",
                BuyerId = buyerUserId,
                Product = product,
                CreatedAt = DateTime.UtcNow
            };

            _repo.Setup(x => x.Query()).Returns(new List<Offer> { offer }.AsAsyncQueryable());

            // Act
            var result = await _service.GetMyOffersAsync(accountId);

            // Assert
            result.Should().NotBeNull();
            result.First().ProductImageUrl.Should().Be("https://example.com/image1.jpg");
        }

        #endregion
    }
}
