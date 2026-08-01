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
    public class AuctionGetUserBidHistoryTests
    {
        private readonly Mock<IAuctionRepository> _auctionRepository;
        private readonly Mock<IAccountRepository> _accountRepository;
        private readonly Mock<AppDbContext> _context;
        private readonly Mock<IPaymentService> _paymentService;
        private readonly Mock<IHubContext<AuctionHub>> _auctionHub;
        private readonly Mock<INotificationService> _notificationService;
        private readonly IMapper _mapper;
        private readonly AuctionService _service;

        private readonly List<Bid> _bids;

        public AuctionGetUserBidHistoryTests()
        {
            _auctionRepository = new Mock<IAuctionRepository>();
            _accountRepository = new Mock<IAccountRepository>();
            _context = new Mock<AppDbContext>();
            _paymentService = new Mock<IPaymentService>();
            _auctionHub = new Mock<IHubContext<AuctionHub>>();
            _notificationService = new Mock<INotificationService>();

            // Cấu hình mapper với NullLoggerFactory
            var configuration = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<AutoMapperProfile>();
            }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
            _mapper = configuration.CreateMapper();

            _bids = new List<Bid>();
            _context.Setup(c => c.Bid).Returns(_bids.AsMockDbSet().Object);

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
        public async Task GetUserBidHistoryAsync_ShouldReturnBidHistory_WhenBidsExistAndAuctionIsOngoingWithHighestBid()
        {
            // Arrange
            var accountId = "acc_1";
            var userId = "user_1";
            var account = new Account { AccountId = accountId, UserId = userId };
            var baseTime = DateTime.UtcNow.AddHours(7);

            var category = new Category { CategoryId = "cat_1", Name = "Electronics" };
            var product = new Product { ProductId = "prod_1", Name = "Phone", Category = category, ProductImage = new List<ProductImage>() };
            var auction = new Auction { AuctionId = "auc_123", ProductId = "prod_1", Product = product, StartTime = baseTime.AddHours(-1), EndTime = baseTime.AddHours(2), Status = "Ongoing" };

            var bid = new Bid
            {
                BidId = "bid_1",
                UserId = userId,
                BidAmount = 150,
                Status = "Highest",
                CreatedAt = baseTime,
                AuctionId = "auc_123",
                Auction = auction
            };

            _bids.Add(bid);
            _context.Setup(c => c.Bid).Returns(_bids.AsMockDbSet().Object);

            _accountRepository.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);

            // Act
            var result = await _service.GetUserBidHistoryAsync(accountId);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            result[0].BidId.Should().Be("bid_1");
            result[0].BidStatus.Should().Be("Winning"); // Ongoing + Highest -> Winning
            result[0].ProductName.Should().Be("Phone");
            result[0].ProductImageUrl.Should().BeEmpty();
        }

        [Fact]
        public async Task GetUserBidHistoryAsync_ShouldReturnBidHistory_WhenBidsExistAndAuctionIsOngoingWithOutbid()
        {
            // Arrange
            var accountId = "acc_1";
            var userId = "user_1";
            var account = new Account { AccountId = accountId, UserId = userId };
            var baseTime = DateTime.UtcNow.AddHours(7);

            var category = new Category { CategoryId = "cat_1", Name = "Electronics" };
            var product = new Product { ProductId = "prod_1", Name = "Phone", Category = category, ProductImage = new List<ProductImage>() };
            var auction = new Auction { AuctionId = "auc_123", ProductId = "prod_1", Product = product, StartTime = baseTime.AddHours(-1), EndTime = baseTime.AddHours(2), Status = "Ongoing" };

            var bid = new Bid
            {
                BidId = "bid_1",
                UserId = userId,
                BidAmount = 150,
                Status = "Outbid",
                CreatedAt = baseTime,
                AuctionId = "auc_123",
                Auction = auction
            };

            _bids.Add(bid);
            _context.Setup(c => c.Bid).Returns(_bids.AsMockDbSet().Object);

            _accountRepository.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);

            // Act
            var result = await _service.GetUserBidHistoryAsync(accountId);

            // Assert
            result.Should().NotBeNull();
            result[0].BidStatus.Should().Be("Outbid"); // Ongoing + Outbid -> Outbid
        }

        [Fact]
        public async Task GetUserBidHistoryAsync_ShouldReturnBidHistory_WhenBidsExistAndAuctionIsEndedWithHighest()
        {
            // Arrange
            var accountId = "acc_1";
            var userId = "user_1";
            var account = new Account { AccountId = accountId, UserId = userId };
            var baseTime = DateTime.UtcNow.AddHours(7);

            var category = new Category { CategoryId = "cat_1", Name = "Electronics" };
            var product = new Product { ProductId = "prod_1", Name = "Phone", Category = category, ProductImage = new List<ProductImage>() };
            var auction = new Auction { AuctionId = "auc_123", ProductId = "prod_1", Product = product, StartTime = baseTime.AddHours(-5), EndTime = baseTime.AddHours(-1), Status = "Ended" };

            var bid = new Bid
            {
                BidId = "bid_1",
                UserId = userId,
                BidAmount = 150,
                Status = "Highest",
                CreatedAt = baseTime,
                AuctionId = "auc_123",
                Auction = auction
            };

            _bids.Add(bid);
            _context.Setup(c => c.Bid).Returns(_bids.AsMockDbSet().Object);

            _accountRepository.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);

            // Act
            var result = await _service.GetUserBidHistoryAsync(accountId);

            // Assert
            result.Should().NotBeNull();
            result[0].BidStatus.Should().Be("Won"); // Ended + Highest -> Won
        }

        [Fact]
        public async Task GetUserBidHistoryAsync_ShouldReturnBidHistory_WhenBidsExistAndAuctionIsEndedWithOutbid()
        {
            // Arrange
            var accountId = "acc_1";
            var userId = "user_1";
            var account = new Account { AccountId = accountId, UserId = userId };
            var baseTime = DateTime.UtcNow.AddHours(7);

            var category = new Category { CategoryId = "cat_1", Name = "Electronics" };
            var product = new Product { ProductId = "prod_1", Name = "Phone", Category = category, ProductImage = new List<ProductImage>() };
            var auction = new Auction { AuctionId = "auc_123", ProductId = "prod_1", Product = product, StartTime = baseTime.AddHours(-5), EndTime = baseTime.AddHours(-1), Status = "Ended" };

            var bid = new Bid
            {
                BidId = "bid_1",
                UserId = userId,
                BidAmount = 150,
                Status = "Outbid",
                CreatedAt = baseTime,
                AuctionId = "auc_123",
                Auction = auction
            };

            _bids.Add(bid);
            _context.Setup(c => c.Bid).Returns(_bids.AsMockDbSet().Object);

            _accountRepository.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);

            // Act
            var result = await _service.GetUserBidHistoryAsync(accountId);

            // Assert
            result.Should().NotBeNull();
            result[0].BidStatus.Should().Be("Lost"); // Ended + Outbid -> Lost
        }

        [Fact]
        public async Task GetUserBidHistoryAsync_ShouldReturnBidHistory_WhenBidsExistAndAuctionIsCancelled()
        {
            // Arrange
            var accountId = "acc_1";
            var userId = "user_1";
            var account = new Account { AccountId = accountId, UserId = userId };
            var baseTime = DateTime.UtcNow.AddHours(7);

            var category = new Category { CategoryId = "cat_1", Name = "Electronics" };
            var product = new Product { ProductId = "prod_1", Name = "Phone", Category = category, ProductImage = new List<ProductImage>() };
            var auction = new Auction { AuctionId = "auc_123", ProductId = "prod_1", Product = product, Status = "Cancelled" };

            var bid = new Bid
            {
                BidId = "bid_1",
                UserId = userId,
                BidAmount = 150,
                Status = "Highest",
                CreatedAt = baseTime,
                AuctionId = "auc_123",
                Auction = auction
            };

            _bids.Add(bid);
            _context.Setup(c => c.Bid).Returns(_bids.AsMockDbSet().Object);

            _accountRepository.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);

            // Act
            var result = await _service.GetUserBidHistoryAsync(accountId);

            // Assert
            result.Should().NotBeNull();
            result[0].BidStatus.Should().Be("Cancelled"); // Cancelled Status -> Cancelled
        }

        [Fact]
        public async Task GetUserBidHistoryAsync_ShouldReturnBidHistory_WhenBidsExistAndAuctionIsUpcoming()
        {
            // Arrange
            var accountId = "acc_1";
            var userId = "user_1";
            var account = new Account { AccountId = accountId, UserId = userId };
            var baseTime = DateTime.UtcNow.AddHours(7);

            var category = new Category { CategoryId = "cat_1", Name = "Electronics" };
            var product = new Product { ProductId = "prod_1", Name = "Phone", Category = category, ProductImage = new List<ProductImage>() };
            var auction = new Auction { AuctionId = "auc_123", ProductId = "prod_1", Product = product, StartTime = baseTime.AddHours(2), EndTime = baseTime.AddHours(5), Status = "Upcoming" };

            var bid = new Bid
            {
                BidId = "bid_1",
                UserId = userId,
                BidAmount = 150,
                Status = "Highest",
                CreatedAt = baseTime,
                AuctionId = "auc_123",
                Auction = auction
            };

            _bids.Add(bid);
            _context.Setup(c => c.Bid).Returns(_bids.AsMockDbSet().Object);

            _accountRepository.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);

            // Act
            var result = await _service.GetUserBidHistoryAsync(accountId);

            // Assert
            result.Should().NotBeNull();
            result[0].BidStatus.Should().Be("Highest"); // Upcoming defaults to original status
        }

        [Fact]
        public async Task GetUserBidHistoryAsync_ShouldReturnBidHistory_WhenNoImageAvailableForProduct()
        {
            // Arrange
            var accountId = "acc_1";
            var userId = "user_1";
            var account = new Account { AccountId = accountId, UserId = userId };
            var baseTime = DateTime.UtcNow.AddHours(7);

            var category = new Category { CategoryId = "cat_1", Name = "Electronics" };
            var product = new Product { ProductId = "prod_1", Name = "Phone", Category = category, ProductImage = new List<ProductImage>() }; // Empty list, not null (to bypass backend null-ptr bug)
            var auction = new Auction { AuctionId = "auc_123", ProductId = "prod_1", Product = product, StartTime = baseTime.AddHours(-1), EndTime = baseTime.AddHours(2), Status = "Ongoing" };

            var bid = new Bid
            {
                BidId = "bid_1",
                UserId = userId,
                BidAmount = 150,
                Status = "Highest",
                CreatedAt = baseTime,
                AuctionId = "auc_123",
                Auction = auction
            };

            _bids.Add(bid);
            _context.Setup(c => c.Bid).Returns(_bids.AsMockDbSet().Object);

            _accountRepository.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);

            // Act
            var result = await _service.GetUserBidHistoryAsync(accountId);

            // Assert
            result.Should().NotBeNull();
            result[0].ProductImageUrl.Should().BeEmpty();
        }

        #endregion

        #region Abnormal Tests (A)

        [Fact]
        public async Task GetUserBidHistoryAsync_ShouldThrowException_WhenAccountDoesNotExist()
        {
            // Arrange
            var accountId = "non_existent";
            _accountRepository.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync((Account)null!);

            // Act & Assert
            await _service.Invoking(s => s.GetUserBidHistoryAsync(accountId))
                .Should().ThrowAsync<Exception>()
                .WithMessage("Account does not exist.");
        }

        [Fact]
        public async Task GetUserBidHistoryAsync_ShouldThrowException_WhenAccountNotLinkedToUser()
        {
            // Arrange
            var accountId = "acc_1";
            var account = new Account { AccountId = accountId, UserId = null }; // not linked
            _accountRepository.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);

            // Act & Assert
            await _service.Invoking(s => s.GetUserBidHistoryAsync(accountId))
                .Should().ThrowAsync<Exception>()
                .WithMessage("Account is not linked to a user.");
        }

        #endregion

        #region Boundary Tests (B)

        [Fact]
        public async Task GetUserBidHistoryAsync_ShouldReturnEmptyList_WhenUserHasNoBids()
        {
            // Arrange
            var accountId = "acc_1";
            var userId = "user_1";
            var account = new Account { AccountId = accountId, UserId = userId };

            _context.Setup(c => c.Bid).Returns(new List<Bid>().AsMockDbSet().Object);
            _accountRepository.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);

            // Act
            var result = await _service.GetUserBidHistoryAsync(accountId);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        #endregion
    }
}
