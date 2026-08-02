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
    public class OfferMakeOfferTests
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

        public OfferMakeOfferTests()
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
        public async Task MakeOfferAsync_ShouldCreateOfferAndSendNotification_WhenValidRequestWithDefaultExpiration()
        {
            // Arrange
            string accountId = "acc_buyer_1";
            string buyerUserId = "user_buyer_1";
            string sellerUserId = "user_seller_1";
            string productId = "prod_1";

            var account = new Account { AccountId = accountId, UserId = buyerUserId };
            _accountRepo.Setup(x => x.Query()).Returns(new List<Account> { account }.AsAsyncQueryable());

            var product = new Product
            {
                ProductId = productId,
                Name = "Used Laptop",
                Price = 1000000,
                Status = "Accepted",
                SellerId = sellerUserId,
                ProductImage = new List<ProductImage>
                {
                    new ProductImage
                    {
                        SortOrder = 1,
                        Image = new Image { ImageUrl = "https://example.com/laptop.jpg" }
                    }
                }
            };
            _productRepo.Setup(x => x.Query()).Returns(new List<Product> { product }.AsAsyncQueryable());

            _repo.Setup(x => x.Query()).Returns(new List<Offer>().AsAsyncQueryable());
            _repo.Setup(x => x.AddAsync(It.IsAny<Offer>())).Returns(Task.CompletedTask);

            var buyerUser = new User { UserId = buyerUserId, FirstName = "John", LastName = "Doe" };
            _userRepo.Setup(x => x.GetByIdAsync(buyerUserId)).ReturnsAsync(buyerUser);

            var request = new MakeOfferRequestDto
            {
                ProductId = productId,
                OfferPrice = 800000,
                Message = "Can I get a discount?",
                ExpiresInHours = 0 // Should default to 48 hours
            };

            // Act
            var result = await _service.MakeOfferAsync(accountId, request);

            // Assert
            result.Should().NotBeNull();
            result.ProductId.Should().Be(productId);
            result.ProductName.Should().Be("Used Laptop");
            result.OfferPrice.Should().Be(800000);
            result.OriginalPrice.Should().Be(1000000);
            result.BuyerId.Should().Be(buyerUserId);
            result.BuyerName.Should().Be("John Doe");
            result.ProductImageUrl.Should().Be("https://example.com/laptop.jpg");
            result.Status.Should().Be("Pending");
            result.ExpiresAt.Should().BeAfter(DateTime.UtcNow.AddHours(47));

            _repo.Verify(x => x.AddAsync(It.Is<Offer>(o =>
                o.BuyerId == buyerUserId &&
                o.ProductId == productId &&
                o.OfferPrice == 800000 &&
                o.Status == "Pending"
            )), Times.Once);

            _notificationService.Verify(x => x.CreateAndSendAsync(It.Is<CreateNotificationDto>(n =>
                n.UserId == sellerUserId &&
                n.Title == "New Offer Received" &&
                n.Type == "Offer"
            )), Times.Once);
        }

        [Fact]
        public async Task MakeOfferAsync_ShouldCreateOffer_WhenValidRequestWithCustomExpiration()
        {
            // Arrange
            string accountId = "acc_buyer_1";
            string buyerUserId = "user_buyer_1";
            string productId = "prod_1";

            var account = new Account { AccountId = accountId, UserId = buyerUserId };
            _accountRepo.Setup(x => x.Query()).Returns(new List<Account> { account }.AsAsyncQueryable());

            var product = new Product
            {
                ProductId = productId,
                Name = "Smartphone",
                Price = 5000000,
                Status = "Accepted",
                SellerId = "seller_1",
                ProductImage = new List<ProductImage>()
            };
            _productRepo.Setup(x => x.Query()).Returns(new List<Product> { product }.AsAsyncQueryable());
            _repo.Setup(x => x.Query()).Returns(new List<Offer>().AsAsyncQueryable());
            _repo.Setup(x => x.AddAsync(It.IsAny<Offer>())).Returns(Task.CompletedTask);

            var request = new MakeOfferRequestDto
            {
                ProductId = productId,
                OfferPrice = 4500000,
                Message = "Fast deal today",
                ExpiresInHours = 24
            };

            // Act
            var result = await _service.MakeOfferAsync(accountId, request);

            // Assert
            result.Should().NotBeNull();
            result.ExpiresAt.Should().BeAfter(DateTime.UtcNow.AddHours(23));
            result.ExpiresAt.Should().BeBefore(DateTime.UtcNow.AddHours(25));
        }

        [Fact]
        public async Task MakeOfferAsync_ShouldCreateOffer_WhenProductHasNoPriceSet()
        {
            // Arrange
            string accountId = "acc_buyer_1";
            string buyerUserId = "user_buyer_1";
            string productId = "prod_1";

            var account = new Account { AccountId = accountId, UserId = buyerUserId };
            _accountRepo.Setup(x => x.Query()).Returns(new List<Account> { account }.AsAsyncQueryable());

            var product = new Product
            {
                ProductId = productId,
                Name = "Unpriced Item",
                Price = null,
                Status = "Accepted",
                SellerId = "seller_1",
                ProductImage = new List<ProductImage>()
            };
            _productRepo.Setup(x => x.Query()).Returns(new List<Product> { product }.AsAsyncQueryable());
            _repo.Setup(x => x.Query()).Returns(new List<Offer>().AsAsyncQueryable());
            _repo.Setup(x => x.AddAsync(It.IsAny<Offer>())).Returns(Task.CompletedTask);

            var request = new MakeOfferRequestDto
            {
                ProductId = productId,
                OfferPrice = 200000,
                ExpiresInHours = 12
            };

            // Act
            var result = await _service.MakeOfferAsync(accountId, request);

            // Assert
            result.Should().NotBeNull();
            result.OfferPrice.Should().Be(200000);
            result.OriginalPrice.Should().BeNull();
        }

        [Fact]
        public async Task MakeOfferAsync_ShouldMapBuyerNameAsNull_WhenUserNotFoundInUserRepo()
        {
            // Arrange
            string accountId = "acc_buyer_1";
            string buyerUserId = "user_buyer_1";
            string productId = "prod_1";

            var account = new Account { AccountId = accountId, UserId = buyerUserId };
            _accountRepo.Setup(x => x.Query()).Returns(new List<Account> { account }.AsAsyncQueryable());

            var product = new Product
            {
                ProductId = productId,
                Name = "Keyboard",
                Price = 500000,
                Status = "Accepted",
                SellerId = "seller_1",
                ProductImage = new List<ProductImage>()
            };
            _productRepo.Setup(x => x.Query()).Returns(new List<Product> { product }.AsAsyncQueryable());
            _repo.Setup(x => x.Query()).Returns(new List<Offer>().AsAsyncQueryable());
            _repo.Setup(x => x.AddAsync(It.IsAny<Offer>())).Returns(Task.CompletedTask);
            _userRepo.Setup(x => x.GetByIdAsync(buyerUserId)).ReturnsAsync((User?)null);

            var request = new MakeOfferRequestDto
            {
                ProductId = productId,
                OfferPrice = 400000
            };

            // Act
            var result = await _service.MakeOfferAsync(accountId, request);

            // Assert
            result.Should().NotBeNull();
            result.BuyerName.Should().BeNull();
        }

        #endregion

        #region Abnormal Tests (A)

        [Fact]
        public async Task MakeOfferAsync_ShouldThrowException_WhenAccountNotFound()
        {
            // Arrange
            string accountId = "non_existing_acc";
            _accountRepo.Setup(x => x.Query()).Returns(new List<Account>().AsAsyncQueryable());

            var request = new MakeOfferRequestDto { ProductId = "prod_1", OfferPrice = 10000 };

            // Act
            Func<Task> act = async () => await _service.MakeOfferAsync(accountId, request);

            // Assert
            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Account not found or not linked to a user.");
        }

        [Fact]
        public async Task MakeOfferAsync_ShouldThrowException_WhenAccountNotLinkedToUser()
        {
            // Arrange
            string accountId = "acc_unlinked";
            var account = new Account { AccountId = accountId, UserId = null };
            _accountRepo.Setup(x => x.Query()).Returns(new List<Account> { account }.AsAsyncQueryable());

            var request = new MakeOfferRequestDto { ProductId = "prod_1", OfferPrice = 10000 };

            // Act
            Func<Task> act = async () => await _service.MakeOfferAsync(accountId, request);

            // Assert
            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Account not found or not linked to a user.");
        }

        [Fact]
        public async Task MakeOfferAsync_ShouldThrowException_WhenProductNotFound()
        {
            // Arrange
            string accountId = "acc_buyer_1";
            var account = new Account { AccountId = accountId, UserId = "user_buyer_1" };
            _accountRepo.Setup(x => x.Query()).Returns(new List<Account> { account }.AsAsyncQueryable());

            _productRepo.Setup(x => x.Query()).Returns(new List<Product>().AsAsyncQueryable());

            var request = new MakeOfferRequestDto { ProductId = "non_existing_prod", OfferPrice = 10000 };

            // Act
            Func<Task> act = async () => await _service.MakeOfferAsync(accountId, request);

            // Assert
            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Product not found.");
        }

        [Theory]
        [InlineData("Pending")]
        [InlineData("Rejected")]
        [InlineData("Draft")]
        [InlineData("Sold")]
        public async Task MakeOfferAsync_ShouldThrowException_WhenProductStatusIsNotAccepted(string status)
        {
            // Arrange
            string accountId = "acc_buyer_1";
            string productId = "prod_1";
            var account = new Account { AccountId = accountId, UserId = "user_buyer_1" };
            _accountRepo.Setup(x => x.Query()).Returns(new List<Account> { account }.AsAsyncQueryable());

            var product = new Product
            {
                ProductId = productId,
                Status = status,
                SellerId = "seller_1",
                ProductImage = new List<ProductImage>()
            };
            _productRepo.Setup(x => x.Query()).Returns(new List<Product> { product }.AsAsyncQueryable());

            var request = new MakeOfferRequestDto { ProductId = productId, OfferPrice = 10000 };

            // Act
            Func<Task> act = async () => await _service.MakeOfferAsync(accountId, request);

            // Assert
            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Product is not available for offers.");
        }

        [Fact]
        public async Task MakeOfferAsync_ShouldThrowException_WhenBuyerIsSeller()
        {
            // Arrange
            string accountId = "acc_same_user";
            string userId = "user_same";
            string productId = "prod_1";

            var account = new Account { AccountId = accountId, UserId = userId };
            _accountRepo.Setup(x => x.Query()).Returns(new List<Account> { account }.AsAsyncQueryable());

            var product = new Product
            {
                ProductId = productId,
                Status = "Accepted",
                SellerId = userId, // Same as buyer
                ProductImage = new List<ProductImage>()
            };
            _productRepo.Setup(x => x.Query()).Returns(new List<Product> { product }.AsAsyncQueryable());

            var request = new MakeOfferRequestDto { ProductId = productId, OfferPrice = 10000 };

            // Act
            Func<Task> act = async () => await _service.MakeOfferAsync(accountId, request);

            // Assert
            await act.Should().ThrowAsync<Exception>()
                .WithMessage("You cannot make an offer on your own product.");
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-5000)]
        public async Task MakeOfferAsync_ShouldThrowException_WhenOfferPriceIsZeroOrNegative(decimal offerPrice)
        {
            // Arrange
            string accountId = "acc_buyer_1";
            string buyerUserId = "user_buyer_1";
            string productId = "prod_1";

            var account = new Account { AccountId = accountId, UserId = buyerUserId };
            _accountRepo.Setup(x => x.Query()).Returns(new List<Account> { account }.AsAsyncQueryable());

            var product = new Product
            {
                ProductId = productId,
                Price = 500000,
                Status = "Accepted",
                SellerId = "seller_1",
                ProductImage = new List<ProductImage>()
            };
            _productRepo.Setup(x => x.Query()).Returns(new List<Product> { product }.AsAsyncQueryable());

            var request = new MakeOfferRequestDto { ProductId = productId, OfferPrice = offerPrice };

            // Act
            Func<Task> act = async () => await _service.MakeOfferAsync(accountId, request);

            // Assert
            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Offer price must be greater than 0.");
        }

        [Fact]
        public async Task MakeOfferAsync_ShouldThrowException_WhenOfferPriceIsEqualToListedPrice()
        {
            // Arrange
            string accountId = "acc_buyer_1";
            string buyerUserId = "user_buyer_1";
            string productId = "prod_1";

            var account = new Account { AccountId = accountId, UserId = buyerUserId };
            _accountRepo.Setup(x => x.Query()).Returns(new List<Account> { account }.AsAsyncQueryable());

            var product = new Product
            {
                ProductId = productId,
                Price = 500000,
                Status = "Accepted",
                SellerId = "seller_1",
                ProductImage = new List<ProductImage>()
            };
            _productRepo.Setup(x => x.Query()).Returns(new List<Product> { product }.AsAsyncQueryable());

            var request = new MakeOfferRequestDto { ProductId = productId, OfferPrice = 500000 };

            // Act
            Func<Task> act = async () => await _service.MakeOfferAsync(accountId, request);

            // Assert
            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Your offer must be lower than the listed price (500,000 VND). Offers are for bargaining only.");
        }

        [Fact]
        public async Task MakeOfferAsync_ShouldThrowException_WhenOfferPriceIsGreaterThanListedPrice()
        {
            // Arrange
            string accountId = "acc_buyer_1";
            string buyerUserId = "user_buyer_1";
            string productId = "prod_1";

            var account = new Account { AccountId = accountId, UserId = buyerUserId };
            _accountRepo.Setup(x => x.Query()).Returns(new List<Account> { account }.AsAsyncQueryable());

            var product = new Product
            {
                ProductId = productId,
                Price = 500000,
                Status = "Accepted",
                SellerId = "seller_1",
                ProductImage = new List<ProductImage>()
            };
            _productRepo.Setup(x => x.Query()).Returns(new List<Product> { product }.AsAsyncQueryable());

            var request = new MakeOfferRequestDto { ProductId = productId, OfferPrice = 600000 };

            // Act
            Func<Task> act = async () => await _service.MakeOfferAsync(accountId, request);

            // Assert
            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Your offer must be lower than the listed price (500,000 VND). Offers are for bargaining only.");
        }

        [Fact]
        public async Task MakeOfferAsync_ShouldThrowException_WhenPendingOfferAlreadyExists()
        {
            // Arrange
            string accountId = "acc_buyer_1";
            string buyerUserId = "user_buyer_1";
            string productId = "prod_1";

            var account = new Account { AccountId = accountId, UserId = buyerUserId };
            _accountRepo.Setup(x => x.Query()).Returns(new List<Account> { account }.AsAsyncQueryable());

            var product = new Product
            {
                ProductId = productId,
                Price = 500000,
                Status = "Accepted",
                SellerId = "seller_1",
                ProductImage = new List<ProductImage>()
            };
            _productRepo.Setup(x => x.Query()).Returns(new List<Product> { product }.AsAsyncQueryable());

            var existingOffer = new Offer
            {
                OfferId = "existing_offer_1",
                BuyerId = buyerUserId,
                ProductId = productId,
                Status = "Pending"
            };
            _repo.Setup(x => x.Query()).Returns(new List<Offer> { existingOffer }.AsAsyncQueryable());

            var request = new MakeOfferRequestDto { ProductId = productId, OfferPrice = 400000 };

            // Act
            Func<Task> act = async () => await _service.MakeOfferAsync(accountId, request);

            // Assert
            await act.Should().ThrowAsync<Exception>()
                .WithMessage("You already have a pending offer for this product. Cancel it before making a new one.");
        }

        #endregion

        #region Boundary Tests (B)

        [Fact]
        public async Task MakeOfferAsync_ShouldCreateOffer_WhenOfferPriceIsJustBelowListedPrice()
        {
            // Arrange
            string accountId = "acc_buyer_1";
            string buyerUserId = "user_buyer_1";
            string productId = "prod_1";

            var account = new Account { AccountId = accountId, UserId = buyerUserId };
            _accountRepo.Setup(x => x.Query()).Returns(new List<Account> { account }.AsAsyncQueryable());

            var product = new Product
            {
                ProductId = productId,
                Price = 100000,
                Status = "Accepted",
                SellerId = "seller_1",
                ProductImage = new List<ProductImage>()
            };
            _productRepo.Setup(x => x.Query()).Returns(new List<Product> { product }.AsAsyncQueryable());
            _repo.Setup(x => x.Query()).Returns(new List<Offer>().AsAsyncQueryable());
            _repo.Setup(x => x.AddAsync(It.IsAny<Offer>())).Returns(Task.CompletedTask);

            var request = new MakeOfferRequestDto
            {
                ProductId = productId,
                OfferPrice = 99999
            };

            // Act
            var result = await _service.MakeOfferAsync(accountId, request);

            // Assert
            result.Should().NotBeNull();
            result.OfferPrice.Should().Be(99999);
        }

        [Fact]
        public async Task MakeOfferAsync_ShouldCreateOffer_WhenOfferPriceIsMinimumValidAmount()
        {
            // Arrange
            string accountId = "acc_buyer_1";
            string buyerUserId = "user_buyer_1";
            string productId = "prod_1";

            var account = new Account { AccountId = accountId, UserId = buyerUserId };
            _accountRepo.Setup(x => x.Query()).Returns(new List<Account> { account }.AsAsyncQueryable());

            var product = new Product
            {
                ProductId = productId,
                Price = 100000,
                Status = "Accepted",
                SellerId = "seller_1",
                ProductImage = new List<ProductImage>()
            };
            _productRepo.Setup(x => x.Query()).Returns(new List<Product> { product }.AsAsyncQueryable());
            _repo.Setup(x => x.Query()).Returns(new List<Offer>().AsAsyncQueryable());
            _repo.Setup(x => x.AddAsync(It.IsAny<Offer>())).Returns(Task.CompletedTask);

            var request = new MakeOfferRequestDto
            {
                ProductId = productId,
                OfferPrice = 1
            };

            // Act
            var result = await _service.MakeOfferAsync(accountId, request);

            // Assert
            result.Should().NotBeNull();
            result.OfferPrice.Should().Be(1);
        }

        [Theory]
        [InlineData("Rejected")]
        [InlineData("Cancelled")]
        [InlineData("Accepted")]
        [InlineData("Completed")]
        public async Task MakeOfferAsync_ShouldCreateOffer_WhenPreviousOfferExistsButNotPending(string previousStatus)
        {
            // Arrange
            string accountId = "acc_buyer_1";
            string buyerUserId = "user_buyer_1";
            string productId = "prod_1";

            var account = new Account { AccountId = accountId, UserId = buyerUserId };
            _accountRepo.Setup(x => x.Query()).Returns(new List<Account> { account }.AsAsyncQueryable());

            var product = new Product
            {
                ProductId = productId,
                Price = 500000,
                Status = "Accepted",
                SellerId = "seller_1",
                ProductImage = new List<ProductImage>()
            };
            _productRepo.Setup(x => x.Query()).Returns(new List<Product> { product }.AsAsyncQueryable());

            var oldOffer = new Offer
            {
                OfferId = "old_offer",
                BuyerId = buyerUserId,
                ProductId = productId,
                Status = previousStatus
            };
            _repo.Setup(x => x.Query()).Returns(new List<Offer> { oldOffer }.AsAsyncQueryable());
            _repo.Setup(x => x.AddAsync(It.IsAny<Offer>())).Returns(Task.CompletedTask);

            var request = new MakeOfferRequestDto
            {
                ProductId = productId,
                OfferPrice = 300000
            };

            // Act
            var result = await _service.MakeOfferAsync(accountId, request);

            // Assert
            result.Should().NotBeNull();
            result.OfferPrice.Should().Be(300000);
            result.Status.Should().Be("Pending");
        }

        #endregion
    }
}
