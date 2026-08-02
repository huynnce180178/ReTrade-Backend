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
    public class AuctionPlaceBidTests
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

        private readonly List<Bid> _bids;
        private readonly List<AuctionDeposit> _deposits;
        private readonly List<Order> _orders;
        private readonly List<Payment> _payments;
        private readonly List<RefundRequest> _refundRequests;
        private readonly List<Address> _addresses;

        public AuctionPlaceBidTests()
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

            // Cấu hình mapper với NullLoggerFactory
            var configuration = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<AutoMapperProfile>();
            }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
            _mapper = configuration.CreateMapper();

            // Khởi tạo các danh sách mock dbset
            _bids = new List<Bid>();
            _deposits = new List<AuctionDeposit>();
            _orders = new List<Order>();
            _payments = new List<Payment>();
            _refundRequests = new List<RefundRequest>();
            _addresses = new List<Address>();

            // Setup default database mock dbset
            _context.Setup(c => c.Bid).Returns(_bids.AsMockDbSet().Object);
            _context.Setup(c => c.AuctionDeposit).Returns(_deposits.AsMockDbSet().Object);
            _context.Setup(c => c.Order).Returns(_orders.AsMockDbSet().Object);
            _context.Setup(c => c.Payment).Returns(_payments.AsMockDbSet().Object);
            _context.Setup(c => c.RefundRequest).Returns(_refundRequests.AsMockDbSet().Object);
            _context.Setup(c => c.Address).Returns(_addresses.AsMockDbSet().Object);

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
        public async Task PlaceBidAsync_ShouldPlaceBidAndNotify_WhenBidIsNormal()
        {
            // Arrange
            var accountId = "acc_1";
            var userId = "bidder_1";
            var auctionId = "auc_123";
            var baseTime = DateTime.UtcNow.AddHours(7);

            var dto = new AuctionBidCreateDto { BidAmount = 150 };

            var account = new Account { AccountId = accountId, UserId = userId };
            var existingAuction = new Auction
            {
                AuctionId = auctionId,
                SellerId = "seller_1",
                StartingPrice = 100,
                CurrentPrice = 100,
                MinIncrement = 10,
                StartTime = baseTime.AddHours(-1),
                EndTime = baseTime.AddHours(2),
                Status = "Ongoing",
                Bid = new List<Bid>
                {
                    new Bid { BidId = "bid_old", UserId = "bidder_old", BidAmount = 110, Status = "Highest" }
                }
            };

            var deposit = new AuctionDeposit
            {
                AuctionId = auctionId,
                UserId = userId,
                DepositAmount = 50000, // available limit: 50000 - 20000 = 30000
                Status = "Paid",
                PolicyAccepted = true,
                CreatedAt = baseTime
            };

            _deposits.Add(deposit);
            _context.Setup(c => c.AuctionDeposit).Returns(_deposits.AsMockDbSet().Object);

            _accountRepository.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);
            _auctionRepository.Setup(x => x.GetByIdAsync(auctionId)).ReturnsAsync(existingAuction);

            // Act
            var result = await _service.PlaceBidAsync(accountId, auctionId, dto);

            // Assert
            result.Should().NotBeNull();
            result.AuctionEnded.Should().BeFalse();
            result.Message.Should().Be("Bid placed successfully.");

            existingAuction.CurrentPrice.Should().Be(150);
            existingAuction.Bid.First(b => b.BidId == "bid_old").Status.Should().Be("Outbid");

            _context.Verify(x => x.SaveChangesAsync(It.IsAny<System.Threading.CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task PlaceBidAsync_ShouldPlaceBuyNowBidAndEndAuction_WhenBidMatchesBuyNowPrice()
        {
            // Arrange
            var accountId = "acc_1";
            var userId = "bidder_1";
            var auctionId = "auc_123";
            var baseTime = DateTime.UtcNow.AddHours(7);

            var dto = new AuctionBidCreateDto { BidAmount = 500 };

            var account = new Account { AccountId = accountId, UserId = userId };
            var existingAuction = new Auction
            {
                AuctionId = auctionId,
                SellerId = "seller_1",
                StartingPrice = 100,
                CurrentPrice = 100,
                BuyNowPrice = 500, // matches BuyNowPrice
                MinIncrement = 10,
                StartTime = baseTime.AddHours(-1),
                EndTime = baseTime.AddHours(2),
                Status = "Ongoing",
                Bid = new List<Bid>(),
                Product = new Product { ProductId = "p1", Name = "Premium Product" }
            };

            var deposit = new AuctionDeposit
            {
                AuctionDepositId = "dep_1",
                AuctionId = auctionId,
                UserId = userId,
                DepositAmount = 1000000, // very large limit
                Status = "Paid",
                PolicyAccepted = true,
                CreatedAt = baseTime
            };

            _deposits.Add(deposit);
            _context.Setup(c => c.AuctionDeposit).Returns(_deposits.AsMockDbSet().Object);

            _accountRepository.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);
            _auctionRepository.Setup(x => x.GetByIdAsync(auctionId)).ReturnsAsync(existingAuction);

            // Act
            var result = await _service.PlaceBidAsync(accountId, auctionId, dto);

            // Assert
            result.Should().NotBeNull();
            result.AuctionEnded.Should().BeTrue();
            result.Message.Should().Be("Bid matched buy now price. Auction ended.");

            existingAuction.Status.Should().Be("EndedByBuyNow");
            existingAuction.WinnerId.Should().Be(userId);
            deposit.Status.Should().Be("AppliedToOrder");
        }

        [Fact]
        public async Task PlaceBidAsync_ShouldPlaceBid_WhenFirstBidOnAuction()
        {
            // Arrange
            var accountId = "acc_1";
            var userId = "bidder_1";
            var auctionId = "auc_123";
            var baseTime = DateTime.UtcNow.AddHours(7);

            var dto = new AuctionBidCreateDto { BidAmount = 110 }; // greater than starting price

            var account = new Account { AccountId = accountId, UserId = userId };
            var existingAuction = new Auction
            {
                AuctionId = auctionId,
                SellerId = "seller_1",
                StartingPrice = 100,
                CurrentPrice = null,
                MinIncrement = 10,
                StartTime = baseTime.AddHours(-1),
                EndTime = baseTime.AddHours(2),
                Status = "Ongoing",
                Bid = new List<Bid>() // first bid
            };

            var deposit = new AuctionDeposit
            {
                AuctionId = auctionId,
                UserId = userId,
                DepositAmount = 50000,
                Status = "Paid",
                PolicyAccepted = true,
                CreatedAt = baseTime
            };

            _deposits.Add(deposit);
            _context.Setup(c => c.AuctionDeposit).Returns(_deposits.AsMockDbSet().Object);

            _accountRepository.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);
            _auctionRepository.Setup(x => x.GetByIdAsync(auctionId)).ReturnsAsync(existingAuction);

            // Act
            var result = await _service.PlaceBidAsync(accountId, auctionId, dto);

            // Assert
            result.Should().NotBeNull();
            result.Message.Should().Be("Bid placed successfully.");
            existingAuction.CurrentPrice.Should().Be(110);
        }

        [Fact]
        public async Task PlaceBidAsync_ShouldPlaceBidAndNotifyOutbidUsers_WhenPreviousHighestBidderExists()
        {
            // Arrange
            var accountId = "acc_1";
            var userId = "bidder_2";
            var auctionId = "auc_123";
            var baseTime = DateTime.UtcNow.AddHours(7);

            var dto = new AuctionBidCreateDto { BidAmount = 200 };

            var account = new Account { AccountId = accountId, UserId = userId };
            var existingAuction = new Auction
            {
                AuctionId = auctionId,
                SellerId = "seller_1",
                StartingPrice = 100,
                CurrentPrice = 150,
                MinIncrement = 10,
                StartTime = baseTime.AddHours(-1),
                EndTime = baseTime.AddHours(2),
                Status = "Ongoing",
                Bid = new List<Bid>
                {
                    new Bid { BidId = "bid_old", UserId = "bidder_1", BidAmount = 150, Status = "Highest" }
                },
                Product = new Product { ProductId = "p1", Name = "Premium Product" }
            };

            var deposit = new AuctionDeposit
            {
                AuctionId = auctionId,
                UserId = userId,
                DepositAmount = 50000,
                Status = "Paid",
                PolicyAccepted = true,
                CreatedAt = baseTime
            };

            var outbidDeposit = new AuctionDeposit
            {
                AuctionId = auctionId,
                UserId = "bidder_1",
                DepositAmount = 50000,
                Status = "Paid",
                CreatedAt = baseTime
            };

            _deposits.Add(deposit);
            _deposits.Add(outbidDeposit);
            _context.Setup(c => c.AuctionDeposit).Returns(_deposits.AsMockDbSet().Object);

            _accountRepository.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);
            _auctionRepository.Setup(x => x.GetByIdAsync(auctionId)).ReturnsAsync(existingAuction);

            // Act
            await _service.PlaceBidAsync(accountId, auctionId, dto);

            // Assert
            _notificationService.Verify(x => x.CreateAndSendAsync(It.Is<CreateNotificationDto>(n => 
                n.UserId == "bidder_1" && n.Title == "You have been outbid!")), Times.Once);
        }

        [Fact]
        public async Task PlaceBidAsync_ShouldPlaceBid_WhenBidAmountIsAtBiddingLimit()
        {
            // Arrange
            var accountId = "acc_1";
            var userId = "bidder_1";
            var auctionId = "auc_123";
            var baseTime = DateTime.UtcNow.AddHours(7);

            var dto = new AuctionBidCreateDto { BidAmount = 30000 }; // Limit is: 50000 - 20000 - 0 = 30000

            var account = new Account { AccountId = accountId, UserId = userId };
            var existingAuction = new Auction
            {
                AuctionId = auctionId,
                SellerId = "seller_1",
                StartingPrice = 100,
                CurrentPrice = 100,
                MinIncrement = 10,
                StartTime = baseTime.AddHours(-1),
                EndTime = baseTime.AddHours(2),
                Status = "Ongoing",
                Bid = new List<Bid>()
            };

            var deposit = new AuctionDeposit
            {
                AuctionId = auctionId,
                UserId = userId,
                DepositAmount = 50000, // 50K
                Status = "Paid",
                PolicyAccepted = true,
                CreatedAt = baseTime
            };

            _deposits.Add(deposit);
            _context.Setup(c => c.AuctionDeposit).Returns(_deposits.AsMockDbSet().Object);

            _accountRepository.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);
            _auctionRepository.Setup(x => x.GetByIdAsync(auctionId)).ReturnsAsync(existingAuction);

            // Act
            var result = await _service.PlaceBidAsync(accountId, auctionId, dto);

            // Assert
            result.Should().NotBeNull();
            result.Message.Should().Be("Bid placed successfully.");
        }

        #endregion

        #region Abnormal Tests (A)

        [Fact]
        public async Task PlaceBidAsync_ShouldThrowException_WhenBidAmountIsZeroOrNegative()
        {
            // Arrange
            var accountId = "acc_1";
            var userId = "bidder_1";
            var dto = new AuctionBidCreateDto { BidAmount = 0 };

            var account = new Account { AccountId = accountId, UserId = userId };
            _accountRepository.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);

            // Act & Assert
            await _service.Invoking(s => s.PlaceBidAsync(accountId, "auc_123", dto))
                .Should().ThrowAsync<Exception>()
                .WithMessage("Bid amount must be greater than 0.");
        }

        [Fact]
        public async Task PlaceBidAsync_ShouldThrowException_WhenAuctionDoesNotExist()
        {
            // Arrange
            var accountId = "acc_1";
            var userId = "bidder_1";
            var dto = new AuctionBidCreateDto { BidAmount = 150 };

            var account = new Account { AccountId = accountId, UserId = userId };
            _accountRepository.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);
            _auctionRepository.Setup(x => x.GetByIdAsync("non_existent")).ReturnsAsync((Auction)null!);

            // Act & Assert
            await _service.Invoking(s => s.PlaceBidAsync(accountId, "non_existent", dto))
                .Should().ThrowAsync<Exception>()
                .WithMessage("Auction not found.");
        }

        #endregion

        #region Boundary Tests (B)

        [Fact]
        public async Task PlaceBidAsync_ShouldThrowException_WhenSellerTriesToBid()
        {
            // Arrange
            var accountId = "acc_1";
            var userId = "seller_1";
            var auctionId = "auc_123";
            var dto = new AuctionBidCreateDto { BidAmount = 150 };

            var account = new Account { AccountId = accountId, UserId = userId };
            var existingAuction = new Auction
            {
                AuctionId = auctionId,
                SellerId = userId, // Seller bids on own auction
                Bid = new List<Bid>()
            };

            _accountRepository.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);
            _auctionRepository.Setup(x => x.GetByIdAsync(auctionId)).ReturnsAsync(existingAuction);

            // Act & Assert
            await _service.Invoking(s => s.PlaceBidAsync(accountId, auctionId, dto))
                .Should().ThrowAsync<Exception>()
                .WithMessage("You cannot bid on your own auction.");
        }

        [Fact]
        public async Task PlaceBidAsync_ShouldThrowException_WhenAuctionIsNotOngoing()
        {
            // Arrange
            var accountId = "acc_1";
            var userId = "bidder_1";
            var auctionId = "auc_123";
            var baseTime = DateTime.UtcNow.AddHours(7);
            var dto = new AuctionBidCreateDto { BidAmount = 150 };

            var account = new Account { AccountId = accountId, UserId = userId };
            var existingAuction = new Auction
            {
                AuctionId = auctionId,
                SellerId = "seller_1",
                StartTime = baseTime.AddHours(1), // upcoming
                EndTime = baseTime.AddHours(4),
                Status = "Upcoming",
                Bid = new List<Bid>()
            };

            _accountRepository.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);
            _auctionRepository.Setup(x => x.GetByIdAsync(auctionId)).ReturnsAsync(existingAuction);

            // Act & Assert
            await _service.Invoking(s => s.PlaceBidAsync(accountId, auctionId, dto))
                .Should().ThrowAsync<Exception>()
                .WithMessage("Bids can only be placed on active auctions.");
        }

        [Fact]
        public async Task PlaceBidAsync_ShouldThrowException_WhenDepositOrPolicyMissingOrAmountExceedsLimit()
        {
            // Arrange
            var accountId = "acc_1";
            var userId = "bidder_1";
            var auctionId = "auc_123";
            var baseTime = DateTime.UtcNow.AddHours(7);

            var account = new Account { AccountId = accountId, UserId = userId };
            var existingAuction = new Auction
            {
                AuctionId = auctionId,
                SellerId = "seller_1",
                StartingPrice = 100,
                CurrentPrice = 100,
                MinIncrement = 10,
                StartTime = baseTime.AddHours(-1),
                EndTime = baseTime.AddHours(2),
                Status = "Ongoing",
                Bid = new List<Bid>()
            };

            _accountRepository.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);
            _auctionRepository.Setup(x => x.GetByIdAsync(auctionId)).ReturnsAsync(existingAuction);

            // Test 1: No deposit found
            var dto = new AuctionBidCreateDto { BidAmount = 150 };
            _context.Setup(c => c.AuctionDeposit).Returns(new List<AuctionDeposit>().AsMockDbSet().Object);

            await _service.Invoking(s => s.PlaceBidAsync(accountId, auctionId, dto))
                .Should().ThrowAsync<Exception>()
                .WithMessage("A paid deposit and accepted policy are required before bidding.");

            // Test 2: Deposit paid but policy not accepted
            var depositUnaccepted = new AuctionDeposit { AuctionId = auctionId, UserId = userId, Status = "Paid", PolicyAccepted = false, CreatedAt = baseTime };
            _context.Setup(c => c.AuctionDeposit).Returns(new List<AuctionDeposit> { depositUnaccepted }.AsMockDbSet().Object);

            await _service.Invoking(s => s.PlaceBidAsync(accountId, auctionId, dto))
                .Should().ThrowAsync<Exception>()
                .WithMessage("A paid deposit and accepted policy are required before bidding.");

            // Test 3: Bid amount exceeds bidding limit (deposit is 30K, limit is 30K - 20K = 10K. Bid 15K exceeds it)
            var depositPaid = new AuctionDeposit { AuctionId = auctionId, UserId = userId, DepositAmount = 30000, Status = "Paid", PolicyAccepted = true, CreatedAt = baseTime };
            _context.Setup(c => c.AuctionDeposit).Returns(new List<AuctionDeposit> { depositPaid }.AsMockDbSet().Object);

            var dtoExceed = new AuctionBidCreateDto { BidAmount = 15000 };
            await _service.Invoking(s => s.PlaceBidAsync(accountId, auctionId, dtoExceed))
                .Should().ThrowAsync<Exception>()
                .WithMessage("Bid amount cannot exceed your bidding limit*");

            // Test 4: Bid amount is less than or equal to current price (CurrentPrice = 100, Bid = 100)
            var depositLarge = new AuctionDeposit { AuctionId = auctionId, UserId = userId, DepositAmount = 500000, Status = "Paid", PolicyAccepted = true, CreatedAt = baseTime };
            _context.Setup(c => c.AuctionDeposit).Returns(new List<AuctionDeposit> { depositLarge }.AsMockDbSet().Object);

            var dtoLow = new AuctionBidCreateDto { BidAmount = 100 };
            await _service.Invoking(s => s.PlaceBidAsync(accountId, auctionId, dtoLow))
                .Should().ThrowAsync<Exception>()
                .WithMessage("Bid amount must be greater than the current bid.");

            // Test 5: Bid amount is less than minimum next bid (Starting = 100, Increment = 10, MinNext = 110. Bid = 105)
            var dtoBelowIncrement = new AuctionBidCreateDto { BidAmount = 105 };
            existingAuction.CurrentPrice = 100;
            existingAuction.Bid = new List<Bid> { new Bid { BidAmount = 100, Status = "Highest" } }; // to establish hasBid
            await _service.Invoking(s => s.PlaceBidAsync(accountId, auctionId, dtoBelowIncrement))
                .Should().ThrowAsync<Exception>()
                .WithMessage("Bid amount must be at least 110 VND.");
        }

        #endregion
    }
}
