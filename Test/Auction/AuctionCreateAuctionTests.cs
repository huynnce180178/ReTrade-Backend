using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Moq;
using RetradeBE.Data;
using RetradeBE.Hubs;
using RetradeBE.Mappings;
using RetradeBE.Models;
using RetradeBE.Models.DTOs;
using RetradeBE.Repositories;
using RetradeBE.Services;
using Xunit;
using Test;

namespace Test.AuctionTests
{
    public class AuctionCreateAuctionTests
    {
        private readonly Mock<IAuctionRepository> _auctionRepository;
        private readonly Mock<IAccountRepository> _accountRepository;
        private readonly Mock<AppDbContext> _context;
        private readonly Mock<IPaymentService> _paymentService;
        private readonly Mock<IHubContext<AuctionHub>> _auctionHub;
        private readonly Mock<INotificationService> _notificationService;
        private readonly Mock<IHubClients> _hubClients;
        private readonly Mock<IClientProxy> _clientProxy;
        private readonly IMapper _mapper;
        private readonly AuctionService _service;

        public AuctionCreateAuctionTests()
        {
            _auctionRepository = new Mock<IAuctionRepository>();
            _accountRepository = new Mock<IAccountRepository>();
            _context = new Mock<AppDbContext>();
            _paymentService = new Mock<IPaymentService>();
            _auctionHub = new Mock<IHubContext<AuctionHub>>();
            _notificationService = new Mock<INotificationService>();

            _hubClients = new Mock<IHubClients>();
            _clientProxy = new Mock<IClientProxy>();
            _hubClients.Setup(x => x.Group(It.IsAny<string>())).Returns(_clientProxy.Object);
            _auctionHub.SetupGet(x => x.Clients).Returns(_hubClients.Object);

            // Default Mock Setup to prevent ArgumentNullException in HasRole check
            _accountRepository.Setup(x => x.GetRolesAsync(It.IsAny<string>()))
                .ReturnsAsync(new List<string> { "User" });

            // Cấu hình mapper với NullLoggerFactory
            var configuration = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<AutoMapperProfile>();
            }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
            _mapper = configuration.CreateMapper();

            _service = new AuctionService(
                _auctionRepository.Object,
                _accountRepository.Object,
                _context.Object,
                _paymentService.Object,
                _auctionHub.Object,
                _notificationService.Object
            );
        }

        #region Normal Tests (N)

        [Fact]
        public async Task CreateAuctionAsync_ShouldCreateAuctionAndNotify_WhenRequestIsValidForSeller()
        {
            // Arrange
            var accountId = "acc_1";
            var userId = "seller_1";
            var baseTime = DateTime.UtcNow.AddHours(7);

            var dto = new AuctionCreateDto
            {
                ProductId = "prod_1",
                StartingPrice = 100,
                MinIncrement = 10,
                BuyNowPrice = 200,
                StartTime = baseTime.AddHours(-1),
                EndTime = baseTime.AddHours(2)
            };

            var account = new Account { AccountId = accountId, UserId = userId };
            var roles = new List<string> { "User" };
            var product = new Product 
            { 
                ProductId = "prod_1", 
                SellerId = userId, 
                Name = "Product 1",
                ProductImage = new List<ProductImage>()
            };

            var createdAuction = new Auction
            {
                AuctionId = "auc_123",
                ProductId = "prod_1",
                SellerId = userId,
                StartingPrice = 100,
                CurrentPrice = 100,
                MinIncrement = 10,
                BuyNowPrice = 200,
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                Status = "Ongoing",
                CreatedAt = baseTime,
                Product = product,
                Bid = new List<Bid>()
            };

            _accountRepository.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);
            _accountRepository.Setup(x => x.GetRolesAsync(accountId)).ReturnsAsync(roles);
            _auctionRepository.Setup(x => x.QueryEligibleProducts()).Returns(new List<Product> { product }.AsAsyncQueryable());
            _auctionRepository.Setup(x => x.HasOpenAuctionForProductAsync("prod_1")).ReturnsAsync(false);
            _auctionRepository.Setup(x => x.GetByIdAsync(It.IsAny<string>())).ReturnsAsync(createdAuction);

            // Act
            var result = await _service.CreateAuctionAsync(accountId, dto);

            // Assert
            result.Should().NotBeNull();
            result.AuctionId.Should().Be("auc_123");
            result.Status.Should().Be("Ongoing");

            _auctionRepository.Verify(x => x.AddAsync(It.Is<Auction>(a => a.ProductId == "prod_1" && a.SellerId == userId)), Times.Once);
            _notificationService.Verify(x => x.NotifyAdminsAsync("New Auction Created", It.IsAny<string>(), "System", It.Is<string>(id => id.StartsWith("auc"))), Times.Once);
        }

        [Fact]
        public async Task CreateAuctionAsync_ShouldCreateAuctionAndNotify_WhenRequestIsValidForAdmin()
        {
            // Arrange
            var accountId = "admin_acc";
            var adminUserId = "admin_1";
            var sellerUserId = "seller_1";
            var baseTime = DateTime.UtcNow.AddHours(7);

            var dto = new AuctionCreateDto
            {
                ProductId = "prod_1",
                StartingPrice = 100,
                MinIncrement = 10,
                BuyNowPrice = 200,
                StartTime = baseTime.AddHours(-1),
                EndTime = baseTime.AddHours(2)
            };

            var account = new Account { AccountId = accountId, UserId = adminUserId };
            var roles = new List<string> { "Admin" };
            var product = new Product 
            { 
                ProductId = "prod_1", 
                SellerId = sellerUserId, // Owned by another seller
                Name = "Product 1",
                ProductImage = new List<ProductImage>()
            };

            var createdAuction = new Auction
            {
                AuctionId = "auc_123",
                ProductId = "prod_1",
                SellerId = sellerUserId,
                StartingPrice = 100,
                CurrentPrice = 100,
                MinIncrement = 10,
                BuyNowPrice = 200,
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                Status = "Ongoing",
                CreatedAt = baseTime,
                Product = product,
                Bid = new List<Bid>()
            };

            _accountRepository.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);
            _accountRepository.Setup(x => x.GetRolesAsync(accountId)).ReturnsAsync(roles);
            _auctionRepository.Setup(x => x.QueryEligibleProducts()).Returns(new List<Product> { product }.AsAsyncQueryable());
            _auctionRepository.Setup(x => x.HasOpenAuctionForProductAsync("prod_1")).ReturnsAsync(false);
            _auctionRepository.Setup(x => x.GetByIdAsync(It.IsAny<string>())).ReturnsAsync(createdAuction);

            // Act
            var result = await _service.CreateAuctionAsync(accountId, dto);

            // Assert
            result.Should().NotBeNull();
            result.AuctionId.Should().Be("auc_123");

            _auctionRepository.Verify(x => x.AddAsync(It.Is<Auction>(a => a.ProductId == "prod_1" && a.SellerId == sellerUserId)), Times.Once);
            _notificationService.Verify(x => x.NotifyAdminsAsync("New Auction Created", It.IsAny<string>(), "System", It.Is<string>(id => id.StartsWith("auc"))), Times.Once);
        }

        [Fact]
        public async Task CreateAuctionAsync_ShouldSetStatusToUpcoming_WhenStartTimeIsAfterNow()
        {
            // Arrange
            var accountId = "acc_1";
            var userId = "seller_1";
            var baseTime = DateTime.UtcNow.AddHours(7);

            var dto = new AuctionCreateDto
            {
                ProductId = "prod_1",
                StartingPrice = 100,
                MinIncrement = 10,
                BuyNowPrice = 200,
                StartTime = baseTime.AddHours(2), // StartTime is in future
                EndTime = baseTime.AddHours(5)
            };

            var account = new Account { AccountId = accountId, UserId = userId };
            var roles = new List<string> { "User" };
            var product = new Product { ProductId = "prod_1", SellerId = userId, ProductImage = new List<ProductImage>() };

            var createdAuction = new Auction
            {
                AuctionId = "auc_123",
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                Status = "Upcoming",
                Product = product,
                Bid = new List<Bid>()
            };

            _accountRepository.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);
            _accountRepository.Setup(x => x.GetRolesAsync(accountId)).ReturnsAsync(roles);
            _auctionRepository.Setup(x => x.QueryEligibleProducts()).Returns(new List<Product> { product }.AsAsyncQueryable());
            _auctionRepository.Setup(x => x.GetByIdAsync(It.IsAny<string>())).ReturnsAsync(createdAuction);

            // Act
            var result = await _service.CreateAuctionAsync(accountId, dto);

            // Assert
            result.Should().NotBeNull();
            result.Status.Should().Be("Upcoming");
        }

        #endregion

        #region Abnormal Tests (A)

        [Fact]
        public async Task CreateAuctionAsync_ShouldThrowException_WhenProductIdIsEmpty()
        {
            // Arrange
            var dto = new AuctionCreateDto { ProductId = "" };
            var account = new Account { AccountId = "acc_1", UserId = "user_1" };
            _accountRepository.Setup(x => x.GetByIdAsync("acc_1")).ReturnsAsync(account);

            // Act & Assert
            await _service.Invoking(s => s.CreateAuctionAsync("acc_1", dto))
                .Should().ThrowAsync<Exception>()
                .WithMessage("ProductId is required.");
        }

        [Fact]
        public async Task CreateAuctionAsync_ShouldThrowException_WhenUserNotLinkedToAccount()
        {
            // Arrange
            var dto = new AuctionCreateDto { ProductId = "prod_1" };
            var account = new Account { AccountId = "acc_1", UserId = null };
            _accountRepository.Setup(x => x.GetByIdAsync("acc_1")).ReturnsAsync(account);

            // Act & Assert
            await _service.Invoking(s => s.CreateAuctionAsync("acc_1", dto))
                .Should().ThrowAsync<Exception>()
                .WithMessage("Account is not linked to a user.");
        }

        #endregion

        #region Boundary Tests (B)

        [Fact]
        public async Task CreateAuctionAsync_ShouldThrowException_WhenStartingPriceIsZeroOrNegative()
        {
            // Arrange
            var dto = new AuctionCreateDto { ProductId = "prod_1", StartingPrice = 0 };
            var account = new Account { AccountId = "acc_1", UserId = "user_1" };
            _accountRepository.Setup(x => x.GetByIdAsync("acc_1")).ReturnsAsync(account);

            // Act & Assert
            await _service.Invoking(s => s.CreateAuctionAsync("acc_1", dto))
                .Should().ThrowAsync<Exception>()
                .WithMessage("Starting bid must be greater than 0.");
        }

        [Fact]
        public async Task CreateAuctionAsync_ShouldThrowException_WhenMinIncrementIsZeroOrNegative()
        {
            // Arrange
            var dto = new AuctionCreateDto { ProductId = "prod_1", StartingPrice = 100, MinIncrement = -5 };
            var account = new Account { AccountId = "acc_1", UserId = "user_1" };
            _accountRepository.Setup(x => x.GetByIdAsync("acc_1")).ReturnsAsync(account);

            // Act & Assert
            await _service.Invoking(s => s.CreateAuctionAsync("acc_1", dto))
                .Should().ThrowAsync<Exception>()
                .WithMessage("Bid step must be greater than 0.");
        }

        [Fact]
        public async Task CreateAuctionAsync_ShouldThrowException_WhenEndTimeIsBeforeOrEqualStartTime()
        {
            // Arrange
            var now = DateTime.UtcNow;
            var dto = new AuctionCreateDto 
            { 
                ProductId = "prod_1", 
                StartingPrice = 100, 
                MinIncrement = 10,
                StartTime = now.AddHours(2),
                EndTime = now.AddHours(1) // EndTime before StartTime
            };
            var account = new Account { AccountId = "acc_1", UserId = "user_1" };
            _accountRepository.Setup(x => x.GetByIdAsync("acc_1")).ReturnsAsync(account);

            // Act & Assert
            await _service.Invoking(s => s.CreateAuctionAsync("acc_1", dto))
                .Should().ThrowAsync<Exception>()
                .WithMessage("Auction end time must be after start time.");
        }

        [Fact]
        public async Task CreateAuctionAsync_ShouldThrowException_WhenBuyNowPriceIsMissingOrNotGreaterThanStartingPrice()
        {
            // Arrange
            var now = DateTime.UtcNow;
            var account = new Account { AccountId = "acc_1", UserId = "user_1" };
            _accountRepository.Setup(x => x.GetByIdAsync("acc_1")).ReturnsAsync(account);

            // BuyNowPrice is Null
            var dtoNull = new AuctionCreateDto 
            { 
                ProductId = "prod_1", StartingPrice = 100, MinIncrement = 10, BuyNowPrice = null,
                StartTime = now, EndTime = now.AddHours(2)
            };
            await _service.Invoking(s => s.CreateAuctionAsync("acc_1", dtoNull))
                .Should().ThrowAsync<Exception>()
                .WithMessage("Buy now price is required.");

            // BuyNowPrice <= StartingPrice
            var dtoLower = new AuctionCreateDto 
            { 
                ProductId = "prod_1", StartingPrice = 100, MinIncrement = 10, BuyNowPrice = 90,
                StartTime = now, EndTime = now.AddHours(2)
            };
            await _service.Invoking(s => s.CreateAuctionAsync("acc_1", dtoLower))
                .Should().ThrowAsync<Exception>()
                .WithMessage("Buy now price must be greater than starting bid.");
        }

        [Fact]
        public async Task CreateAuctionAsync_ShouldThrowException_WhenProductNotReadyOrNotOwnedOrAlreadyHasAuction()
        {
            // Arrange
            var accountId = "acc_1";
            var userId = "seller_1";
            var now = DateTime.UtcNow;

            var account = new Account { AccountId = accountId, UserId = userId };
            var roles = new List<string> { "User" };

            _accountRepository.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);
            _accountRepository.Setup(x => x.GetRolesAsync(accountId)).ReturnsAsync(roles);

            // Scenario 1: Product is null/not eligible
            var dto1 = new AuctionCreateDto { ProductId = "prod_not_found", StartingPrice = 100, MinIncrement = 10, BuyNowPrice = 200, StartTime = now, EndTime = now.AddHours(2) };
            _auctionRepository.Setup(x => x.QueryEligibleProducts()).Returns(new List<Product>().AsAsyncQueryable());

            await _service.Invoking(s => s.CreateAuctionAsync(accountId, dto1))
                .Should().ThrowAsync<Exception>()
                .WithMessage("This product is not ready for auction or already has an open auction.");

            // Scenario 2: Product owned by someone else
            var productOwnedByOther = new Product { ProductId = "prod_other", SellerId = "other_user" };
            var dto2 = new AuctionCreateDto { ProductId = "prod_other", StartingPrice = 100, MinIncrement = 10, BuyNowPrice = 200, StartTime = now, EndTime = now.AddHours(2) };
            _auctionRepository.Setup(x => x.QueryEligibleProducts()).Returns(new List<Product> { productOwnedByOther }.AsAsyncQueryable());

            await _service.Invoking(s => s.CreateAuctionAsync(accountId, dto2))
                .Should().ThrowAsync<Exception>()
                .WithMessage("You can only create auctions for your own products.");

            // Scenario 3: Product already has an open auction
            var productOwnedByMe = new Product { ProductId = "prod_owned", SellerId = userId };
            var dto3 = new AuctionCreateDto { ProductId = "prod_owned", StartingPrice = 100, MinIncrement = 10, BuyNowPrice = 200, StartTime = now, EndTime = now.AddHours(2) };
            _auctionRepository.Setup(x => x.QueryEligibleProducts()).Returns(new List<Product> { productOwnedByMe }.AsAsyncQueryable());
            _auctionRepository.Setup(x => x.HasOpenAuctionForProductAsync("prod_owned")).ReturnsAsync(true);

            await _service.Invoking(s => s.CreateAuctionAsync(accountId, dto3))
                .Should().ThrowAsync<Exception>()
                .WithMessage("This product already has an open auction.");
        }

        #endregion
    }
}
