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
    public class AuctionUpdateAuctionTests
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

        public AuctionUpdateAuctionTests()
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

            // Default Mock Setup to prevent ArgumentNullException in roles check
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
        public async Task UpdateAuctionAsync_ShouldUpdateAuctionAndNotify_WhenRequestIsValidForSeller()
        {
            // Arrange
            var accountId = "acc_1";
            var userId = "seller_1";
            var auctionId = "auc_123";
            var baseTime = DateTime.UtcNow.AddHours(7);

            var dto = new AuctionUpdateDto
            {
                StartingPrice = 150,
                MinIncrement = 15,
                BuyNowPrice = 300,
                StartTime = baseTime.AddHours(2),
                EndTime = baseTime.AddHours(5)
            };

            var account = new Account { AccountId = accountId, UserId = userId };
            var roles = new List<string> { "User" };
            var existingAuction = new Auction
            {
                AuctionId = auctionId,
                SellerId = userId,
                StartingPrice = 100,
                CurrentPrice = 100,
                MinIncrement = 10,
                BuyNowPrice = 200,
                StartTime = baseTime.AddHours(1), // in future
                EndTime = baseTime.AddHours(4),
                Status = "Upcoming",
                Bid = new List<Bid>()
            };

            var updatedAuctionFromDb = new Auction
            {
                AuctionId = auctionId,
                SellerId = userId,
                StartingPrice = 150,
                CurrentPrice = 150,
                MinIncrement = 15,
                BuyNowPrice = 300,
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                Status = "Upcoming",
                Bid = new List<Bid>()
            };

            _accountRepository.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);
            _accountRepository.Setup(x => x.GetRolesAsync(accountId)).ReturnsAsync(roles);
            _auctionRepository.Setup(x => x.GetByIdAsync(auctionId)).ReturnsAsync(existingAuction);
            // Setup second call of GetByIdAsync to return the updated record
            _auctionRepository.Setup(x => x.GetByIdAsync(auctionId)).ReturnsAsync(updatedAuctionFromDb);

            // Act
            var result = await _service.UpdateAuctionAsync(accountId, auctionId, dto);

            // Assert
            result.Should().NotBeNull();
            result.AuctionId.Should().Be(auctionId);
            result.StartingPrice.Should().Be(150);
            result.BuyNowPrice.Should().Be(300);

            _auctionRepository.Verify(x => x.UpdateAsync(It.Is<Auction>(a => a.AuctionId == auctionId && a.StartingPrice == 150)), Times.Once);
        }

        [Fact]
        public async Task UpdateAuctionAsync_ShouldUpdateAuctionAndNotify_WhenRequestIsValidForAdmin()
        {
            // Arrange
            var accountId = "admin_acc";
            var adminUserId = "admin_1";
            var sellerUserId = "seller_1";
            var auctionId = "auc_123";
            var baseTime = DateTime.UtcNow.AddHours(7);

            var dto = new AuctionUpdateDto
            {
                StartingPrice = 150,
                MinIncrement = 15,
                BuyNowPrice = 300,
                StartTime = baseTime.AddHours(2),
                EndTime = baseTime.AddHours(5)
            };

            var account = new Account { AccountId = accountId, UserId = adminUserId };
            var roles = new List<string> { "Admin" };
            var existingAuction = new Auction
            {
                AuctionId = auctionId,
                SellerId = sellerUserId, // Owned by another user
                StartTime = baseTime.AddHours(1),
                EndTime = baseTime.AddHours(4),
                Status = "Upcoming",
                Bid = new List<Bid>()
            };

            var updatedAuctionFromDb = new Auction
            {
                AuctionId = auctionId,
                SellerId = sellerUserId,
                StartingPrice = 150,
                CurrentPrice = 150,
                MinIncrement = 15,
                BuyNowPrice = 300,
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                Status = "Upcoming",
                Bid = new List<Bid>()
            };

            _accountRepository.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);
            _accountRepository.Setup(x => x.GetRolesAsync(accountId)).ReturnsAsync(roles);
            _auctionRepository.Setup(x => x.GetByIdAsync(auctionId)).ReturnsAsync(existingAuction);
            _auctionRepository.Setup(x => x.GetByIdAsync(auctionId)).ReturnsAsync(updatedAuctionFromDb);

            // Act
            var result = await _service.UpdateAuctionAsync(accountId, auctionId, dto);

            // Assert
            result.Should().NotBeNull();
            result.AuctionId.Should().Be(auctionId);
            _auctionRepository.Verify(x => x.UpdateAsync(It.Is<Auction>(a => a.AuctionId == auctionId && a.SellerId == sellerUserId)), Times.Once);
        }

        [Fact]
        public async Task UpdateAuctionAsync_ShouldResetStatusToUpcoming_WhenUpdateSucceeds()
        {
            // Arrange
            var accountId = "acc_1";
            var userId = "seller_1";
            var auctionId = "auc_123";
            var baseTime = DateTime.UtcNow.AddHours(7);

            var dto = new AuctionUpdateDto
            {
                StartingPrice = 150,
                MinIncrement = 15,
                BuyNowPrice = 300,
                StartTime = baseTime.AddHours(2),
                EndTime = baseTime.AddHours(5)
            };

            var account = new Account { AccountId = accountId, UserId = userId };
            var existingAuction = new Auction
            {
                AuctionId = auctionId,
                SellerId = userId,
                StartTime = baseTime.AddHours(1),
                EndTime = baseTime.AddHours(4),
                Status = "Upcoming",
                Bid = new List<Bid>()
            };

            _accountRepository.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);
            _auctionRepository.Setup(x => x.GetByIdAsync(auctionId)).ReturnsAsync(existingAuction);

            // Act
            await _service.UpdateAuctionAsync(accountId, auctionId, dto);

            // Assert
            existingAuction.Status.Should().Be("Upcoming");
        }

        #endregion

        #region Abnormal Tests (A)

        [Fact]
        public async Task UpdateAuctionAsync_ShouldThrowException_WhenAuctionDoesNotExist()
        {
            // Arrange
            var accountId = "acc_1";
            var userId = "seller_1";
            var baseTime = DateTime.UtcNow.AddHours(7);

            var dto = new AuctionUpdateDto
            {
                StartingPrice = 150,
                MinIncrement = 15,
                BuyNowPrice = 300,
                StartTime = baseTime.AddHours(2),
                EndTime = baseTime.AddHours(5)
            };

            var account = new Account { AccountId = accountId, UserId = userId };
            _accountRepository.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);
            _auctionRepository.Setup(x => x.GetByIdAsync("non_existent")).ReturnsAsync((Auction)null!);

            // Act & Assert
            await _service.Invoking(s => s.UpdateAuctionAsync(accountId, "non_existent", dto))
                .Should().ThrowAsync<Exception>()
                .WithMessage("Auction not found.");
        }

        [Fact]
        public async Task UpdateAuctionAsync_ShouldThrowException_WhenSellerNotAuthorised()
        {
            // Arrange
            var accountId = "acc_1";
            var userId = "seller_1";
            var auctionId = "auc_123";
            var baseTime = DateTime.UtcNow.AddHours(7);

            var dto = new AuctionUpdateDto
            {
                StartingPrice = 150,
                MinIncrement = 15,
                BuyNowPrice = 300,
                StartTime = baseTime.AddHours(2),
                EndTime = baseTime.AddHours(5)
            };

            var account = new Account { AccountId = accountId, UserId = userId };
            var roles = new List<string> { "User" };
            var existingAuction = new Auction
            {
                AuctionId = auctionId,
                SellerId = "different_seller", // Not owned by seller_1
                StartTime = baseTime.AddHours(1),
                EndTime = baseTime.AddHours(4),
                Status = "Upcoming",
                Bid = new List<Bid>()
            };

            _accountRepository.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);
            _accountRepository.Setup(x => x.GetRolesAsync(accountId)).ReturnsAsync(roles);
            _auctionRepository.Setup(x => x.GetByIdAsync(auctionId)).ReturnsAsync(existingAuction);

            // Act & Assert
            await _service.Invoking(s => s.UpdateAuctionAsync(accountId, auctionId, dto))
                .Should().ThrowAsync<Exception>()
                .WithMessage("You can only update your own auctions.");
        }

        #endregion

        #region Boundary Tests (B)

        [Fact]
        public async Task UpdateAuctionAsync_ShouldThrowException_WhenAuctionIsAlreadyActive()
        {
            // Arrange
            var accountId = "acc_1";
            var userId = "seller_1";
            var auctionId = "auc_123";
            var baseTime = DateTime.UtcNow.AddHours(7);

            var dto = new AuctionUpdateDto
            {
                StartingPrice = 150,
                MinIncrement = 15,
                BuyNowPrice = 300,
                StartTime = baseTime.AddHours(2),
                EndTime = baseTime.AddHours(5)
            };

            var account = new Account { AccountId = accountId, UserId = userId };
            var existingAuction = new Auction
            {
                AuctionId = auctionId,
                SellerId = userId,
                StartTime = baseTime.AddHours(-1), // already started (in the past compared to baseTime)
                EndTime = baseTime.AddHours(4),
                Status = "Ongoing",
                Bid = new List<Bid>()
            };

            _accountRepository.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);
            _auctionRepository.Setup(x => x.GetByIdAsync(auctionId)).ReturnsAsync(existingAuction);

            // Act & Assert
            await _service.Invoking(s => s.UpdateAuctionAsync(accountId, auctionId, dto))
                .Should().ThrowAsync<Exception>()
                .WithMessage("Auction can only be updated before it becomes active.");
        }

        [Fact]
        public async Task UpdateAuctionAsync_ShouldThrowException_WhenAuctionHasEnded()
        {
            // Arrange
            var accountId = "acc_1";
            var userId = "seller_1";
            var auctionId = "auc_123";
            var baseTime = DateTime.UtcNow.AddHours(7);

            var dto = new AuctionUpdateDto
            {
                StartingPrice = 150,
                MinIncrement = 15,
                BuyNowPrice = 300,
                StartTime = baseTime.AddHours(2),
                EndTime = baseTime.AddHours(5)
            };

            var account = new Account { AccountId = accountId, UserId = userId };
            var existingAuction = new Auction
            {
                AuctionId = auctionId,
                SellerId = userId,
                StartTime = null,
                EndTime = baseTime.AddHours(-1), // already ended in past
                Status = "Ended",
                Bid = new List<Bid>()
            };

            _accountRepository.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);
            _auctionRepository.Setup(x => x.GetByIdAsync(auctionId)).ReturnsAsync(existingAuction);

            // Act & Assert
            await _service.Invoking(s => s.UpdateAuctionAsync(accountId, auctionId, dto))
                .Should().ThrowAsync<Exception>()
                .WithMessage("Ended auctions cannot be updated.");
        }

        [Fact]
        public async Task UpdateAuctionAsync_ShouldThrowException_WhenAuctionHasExistingBids()
        {
            // Arrange
            var accountId = "acc_1";
            var userId = "seller_1";
            var auctionId = "auc_123";
            var baseTime = DateTime.UtcNow.AddHours(7);

            var dto = new AuctionUpdateDto
            {
                StartingPrice = 150,
                MinIncrement = 15,
                BuyNowPrice = 300,
                StartTime = baseTime.AddHours(2),
                EndTime = baseTime.AddHours(5)
            };

            var account = new Account { AccountId = accountId, UserId = userId };
            var existingAuction = new Auction
            {
                AuctionId = auctionId,
                SellerId = userId,
                StartTime = baseTime.AddHours(1),
                EndTime = baseTime.AddHours(4),
                Status = "Upcoming",
                Bid = new List<Bid>
                {
                    new Bid { BidId = "bid_1", BidAmount = 110 } // existing bid
                }
            };

            _accountRepository.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);
            _auctionRepository.Setup(x => x.GetByIdAsync(auctionId)).ReturnsAsync(existingAuction);

            // Act & Assert
            await _service.Invoking(s => s.UpdateAuctionAsync(accountId, auctionId, dto))
                .Should().ThrowAsync<Exception>()
                .WithMessage("Auction with existing bids cannot be updated.");
        }

        [Fact]
        public async Task UpdateAuctionAsync_ShouldThrowException_WhenNewStartTimeIsNotInFuture()
        {
            // Arrange
            var accountId = "acc_1";
            var userId = "seller_1";
            var auctionId = "auc_123";
            var baseTime = DateTime.UtcNow.AddHours(7);

            var dto = new AuctionUpdateDto
            {
                StartingPrice = 150,
                MinIncrement = 15,
                BuyNowPrice = 300,
                StartTime = baseTime.AddHours(-1), // not in future compared to now (baseTime)
                EndTime = baseTime.AddHours(2)
            };

            var account = new Account { AccountId = accountId, UserId = userId };
            var existingAuction = new Auction
            {
                AuctionId = auctionId,
                SellerId = userId,
                StartTime = baseTime.AddHours(1),
                EndTime = baseTime.AddHours(4),
                Status = "Upcoming",
                Bid = new List<Bid>()
            };

            _accountRepository.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);
            _auctionRepository.Setup(x => x.GetByIdAsync(auctionId)).ReturnsAsync(existingAuction);

            // Act & Assert
            await _service.Invoking(s => s.UpdateAuctionAsync(accountId, auctionId, dto))
                .Should().ThrowAsync<Exception>()
                .WithMessage("Auction start time must remain in the future.");
        }

        [Fact]
        public async Task UpdateAuctionAsync_ShouldThrowException_WhenValidationValuesAreInvalid()
        {
            // Arrange
            var accountId = "acc_1";
            var userId = "seller_1";
            var auctionId = "auc_123";
            var baseTime = DateTime.UtcNow.AddHours(7);

            var account = new Account { AccountId = accountId, UserId = userId };
            _accountRepository.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);

            // startingPrice <= 0
            var dtoInvalidPrice = new AuctionUpdateDto { StartingPrice = 0, MinIncrement = 10, BuyNowPrice = 200, StartTime = baseTime.AddHours(1), EndTime = baseTime.AddHours(3) };
            await _service.Invoking(s => s.UpdateAuctionAsync(accountId, auctionId, dtoInvalidPrice))
                .Should().ThrowAsync<Exception>()
                .WithMessage("Starting bid must be greater than 0.");

            // minIncrement <= 0
            var dtoInvalidStep = new AuctionUpdateDto { StartingPrice = 100, MinIncrement = 0, BuyNowPrice = 200, StartTime = baseTime.AddHours(1), EndTime = baseTime.AddHours(3) };
            await _service.Invoking(s => s.UpdateAuctionAsync(accountId, auctionId, dtoInvalidStep))
                .Should().ThrowAsync<Exception>()
                .WithMessage("Bid step must be greater than 0.");

            // endTime <= startTime
            var dtoInvalidTime = new AuctionUpdateDto { StartingPrice = 100, MinIncrement = 10, BuyNowPrice = 200, StartTime = baseTime.AddHours(3), EndTime = baseTime.AddHours(2) };
            await _service.Invoking(s => s.UpdateAuctionAsync(accountId, auctionId, dtoInvalidTime))
                .Should().ThrowAsync<Exception>()
                .WithMessage("Auction end time must be after start time.");

            // buyNowPrice.Value <= startingPrice
            var dtoInvalidBuyNow = new AuctionUpdateDto { StartingPrice = 100, MinIncrement = 10, BuyNowPrice = 90, StartTime = baseTime.AddHours(1), EndTime = baseTime.AddHours(3) };
            await _service.Invoking(s => s.UpdateAuctionAsync(accountId, auctionId, dtoInvalidBuyNow))
                .Should().ThrowAsync<Exception>()
                .WithMessage("Buy now price must be greater than starting bid.");
        }

        #endregion
    }
}
