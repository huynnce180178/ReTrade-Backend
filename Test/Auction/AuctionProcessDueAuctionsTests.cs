using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
    public class AuctionProcessDueAuctionsTests
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

        private readonly List<Auction> _contextAuctions;
        private readonly List<Bid> _contextBids;
        private readonly List<AuctionDeposit> _contextDeposits;
        private readonly List<Order> _contextOrders;
        private readonly List<Payment> _contextPayments;
        private readonly List<RefundRequest> _contextRefundRequests;
        private readonly List<Address> _contextAddresses;

        public AuctionProcessDueAuctionsTests()
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

            // Khởi tạo các danh sách mock DB
            _contextAuctions = new List<Auction>();
            _contextBids = new List<Bid>();
            _contextDeposits = new List<AuctionDeposit>();
            _contextOrders = new List<Order>();
            _contextPayments = new List<Payment>();
            _contextRefundRequests = new List<RefundRequest>();
            _contextAddresses = new List<Address>();

            // Setup default database mock dbset
            _context.Setup(c => c.Auction).Returns(_contextAuctions.AsMockDbSet().Object);
            _context.Setup(c => c.Bid).Returns(_contextBids.AsMockDbSet().Object);
            _context.Setup(c => c.AuctionDeposit).Returns(_contextDeposits.AsMockDbSet().Object);

            var mockOrder = _contextOrders.AsMockDbSet();
            mockOrder.Setup(m => m.Add(It.IsAny<Order>())).Callback<Order>(o => _contextOrders.Add(o));
            _context.Setup(c => c.Order).Returns(mockOrder.Object);

            var mockPayment = _contextPayments.AsMockDbSet();
            mockPayment.Setup(m => m.Add(It.IsAny<Payment>())).Callback<Payment>(p => _contextPayments.Add(p));
            _context.Setup(c => c.Payment).Returns(mockPayment.Object);

            var mockRefund = _contextRefundRequests.AsMockDbSet();
            mockRefund.Setup(m => m.Add(It.IsAny<RefundRequest>())).Callback<RefundRequest>(r => _contextRefundRequests.Add(r));
            _context.Setup(c => c.RefundRequest).Returns(mockRefund.Object);

            _context.Setup(c => c.Address).Returns(_contextAddresses.AsMockDbSet().Object);

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
        public async Task ProcessDueAuctionsAsync_ShouldReturnZero_WhenNoAuctionsAreDue()
        {
            // Arrange
            var baseTime = DateTime.UtcNow.AddHours(7);
            var activeAuction = new Auction { AuctionId = "auc_active", Status = "Ongoing", EndTime = baseTime.AddHours(2) }; // Ends in future
            _contextAuctions.Add(activeAuction);
            _context.Setup(c => c.Auction).Returns(_contextAuctions.AsMockDbSet().Object);

            // Act
            var result = await _service.ProcessDueAuctionsAsync(CancellationToken.None);

            // Assert
            result.Should().Be(0);
        }

        [Fact]
        public async Task ProcessDueAuctionsAsync_ShouldProcessEndedNoBid_WhenAuctionIsDueAndHasNoBids()
        {
            // Arrange
            var baseTime = DateTime.UtcNow.AddHours(7);
            var dueAuction = new Auction 
            { 
                AuctionId = "auc_due", 
                Status = "Ongoing", 
                EndTime = baseTime.AddHours(-1), // in past
                SellerId = "seller_1",
                Product = new Product { ProductId = "p1", Name = "Unwanted Item" }
            }; 
            _contextAuctions.Add(dueAuction);
            _context.Setup(c => c.Auction).Returns(_contextAuctions.AsMockDbSet().Object);

            _auctionRepository.Setup(x => x.GetByIdAsync("auc_due")).ReturnsAsync(dueAuction);

            // Act
            var result = await _service.ProcessDueAuctionsAsync(CancellationToken.None);

            // Assert
            result.Should().Be(1);
            dueAuction.Status.Should().Be("EndedNoBid");
            _context.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
            _notificationService.Verify(x => x.CreateAndSendAsync(It.Is<CreateNotificationDto>(n => 
                n.UserId == "seller_1" && n.Title == "Auction Ended")), Times.Once);
        }

        [Fact]
        public async Task ProcessDueAuctionsAsync_ShouldProcessEndedByTime_WhenAuctionIsDueAndHasBids()
        {
            // Arrange
            var baseTime = DateTime.UtcNow.AddHours(7);
            var dueAuction = new Auction 
            { 
                AuctionId = "auc_due", 
                Status = "Ongoing", 
                EndTime = baseTime.AddHours(-1),
                SellerId = "seller_1",
                Product = new Product { ProductId = "p1", Name = "Hot Item" },
                Bid = new List<Bid>
                {
                    new Bid { BidId = "bid_1", UserId = "buyer_1", BidAmount = 150, CreatedAt = baseTime.AddMinutes(-5) }
                }
            }; 
            _contextAuctions.Add(dueAuction);
            _context.Setup(c => c.Auction).Returns(_contextAuctions.AsMockDbSet().Object);

            var deposit = new AuctionDeposit { AuctionId = "auc_due", UserId = "buyer_1", Status = "Paid", DepositAmount = 50000 };
            _contextDeposits.Add(deposit);
            _context.Setup(c => c.AuctionDeposit).Returns(_contextDeposits.AsMockDbSet().Object);

            _auctionRepository.Setup(x => x.GetByIdAsync("auc_due")).ReturnsAsync(dueAuction);

            // Act
            var result = await _service.ProcessDueAuctionsAsync(CancellationToken.None);

            // Assert
            result.Should().Be(1);
            dueAuction.Status.Should().Be("EndedByTime");
            dueAuction.WinnerId.Should().Be("buyer_1");
            deposit.Status.Should().Be("AppliedToOrder");
        }

        [Fact]
        public async Task ProcessDueAuctionsAsync_ShouldSkipProcessing_WhenAuctionStatusIsEndedOrCancelled()
        {
            // Arrange
            var baseTime = DateTime.UtcNow.AddHours(7);
            var dueAuction = new Auction { AuctionId = "auc_due", Status = "Ongoing", EndTime = baseTime.AddHours(-1) }; 
            _contextAuctions.Add(dueAuction);
            _context.Setup(c => c.Auction).Returns(_contextAuctions.AsMockDbSet().Object);

            var alreadyEndedAuction = new Auction { AuctionId = "auc_due", Status = "Cancelled" };
            _auctionRepository.Setup(x => x.GetByIdAsync("auc_due")).ReturnsAsync(alreadyEndedAuction);

            // Act
            var result = await _service.ProcessDueAuctionsAsync(CancellationToken.None);

            // Assert
            result.Should().Be(0); // Should skip and not increment processed counter
        }

        [Fact]
        public async Task ProcessDueAuctionsAsync_ShouldSkipProcessing_WhenAuctionNotFoundInRepository()
        {
            // Arrange
            var baseTime = DateTime.UtcNow.AddHours(7);
            var dueAuction = new Auction { AuctionId = "auc_due", Status = "Ongoing", EndTime = baseTime.AddHours(-1) }; 
            _contextAuctions.Add(dueAuction);
            _context.Setup(c => c.Auction).Returns(_contextAuctions.AsMockDbSet().Object);

            _auctionRepository.Setup(x => x.GetByIdAsync("auc_due")).ReturnsAsync((Auction)null!);

            // Act
            var result = await _service.ProcessDueAuctionsAsync(CancellationToken.None);

            // Assert
            result.Should().Be(0);
        }

        [Fact]
        public async Task ProcessDueAuctionsAsync_ShouldThrowOperationCanceledException_WhenCancellationIsRequested()
        {
            // Arrange
            var baseTime = DateTime.UtcNow.AddHours(7);
            var dueAuction = new Auction { AuctionId = "auc_due", Status = "Ongoing", EndTime = baseTime.AddHours(-1) }; 
            _contextAuctions.Add(dueAuction);
            _context.Setup(c => c.Auction).Returns(_contextAuctions.AsMockDbSet().Object);

            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            await _service.Invoking(s => s.ProcessDueAuctionsAsync(cts.Token))
                .Should().ThrowAsync<OperationCanceledException>();
        }

        [Fact]
        public async Task ProcessDueAuctionsAsync_ShouldCreateRefundsForLosers_WhenNoBidAuctionHasPaidDeposits()
        {
            // Arrange
            var baseTime = DateTime.UtcNow.AddHours(7);
            var dueAuction = new Auction 
            { 
                AuctionId = "auc_due", 
                Status = "Ongoing", 
                EndTime = baseTime.AddHours(-1),
                SellerId = "seller_1",
                Product = new Product { ProductId = "p1", Name = "Unwanted Item" }
            }; 
            _contextAuctions.Add(dueAuction);
            _context.Setup(c => c.Auction).Returns(_contextAuctions.AsMockDbSet().Object);

            // Mock deposit of a bidder who didn't bid (loser)
            var loserDeposit = new AuctionDeposit { AuctionId = "auc_due", UserId = "loser_1", DepositAmount = 50000, Status = "Paid" };
            _contextDeposits.Add(loserDeposit);
            _context.Setup(c => c.AuctionDeposit).Returns(_contextDeposits.AsMockDbSet().Object);

            _auctionRepository.Setup(x => x.GetByIdAsync("auc_due")).ReturnsAsync(dueAuction);

            // Act
            await _service.ProcessDueAuctionsAsync(CancellationToken.None);

            // Assert
            loserDeposit.Status.Should().Be("RefundPending");
            _contextRefundRequests.Should().NotBeEmpty();
            _contextRefundRequests[0].UserId.Should().Be("loser_1");
            _contextRefundRequests[0].Amount.Should().Be(30000); // 50K deposit - 20K penalty/fee = 30K
        }

        #endregion

        #region Abnormal Tests (A)

        [Fact]
        public async Task ProcessDueAuctionsAsync_ShouldThrowException_WhenDatabaseQueryFails()
        {
            // Arrange
            _context.Setup(c => c.Auction).Throws(new Exception("Database connection lost"));

            // Act & Assert
            await _service.Invoking(s => s.ProcessDueAuctionsAsync(CancellationToken.None))
                .Should().ThrowAsync<Exception>()
                .WithMessage("Database connection lost");
        }

        [Fact]
        public async Task ProcessDueAuctionsAsync_ShouldContinueProcessing_WhenNotificationFails()
        {
            // Arrange
            var baseTime = DateTime.UtcNow.AddHours(7);
            var dueAuction = new Auction 
            { 
                AuctionId = "auc_due", 
                Status = "Ongoing", 
                EndTime = baseTime.AddHours(-1),
                SellerId = "seller_1",
                Product = new Product { ProductId = "p1", Name = "Hot Item" }
            }; 
            _contextAuctions.Add(dueAuction);
            _context.Setup(c => c.Auction).Returns(_contextAuctions.AsMockDbSet().Object);

            _auctionRepository.Setup(x => x.GetByIdAsync("auc_due")).ReturnsAsync(dueAuction);

            // Mock notification service to throw an exception
            _notificationService.Setup(x => x.CreateAndSendAsync(It.IsAny<CreateNotificationDto>()))
                .ThrowsAsync(new Exception("Notification service down"));

            // Act
            var result = await _service.ProcessDueAuctionsAsync(CancellationToken.None);

            // Assert
            result.Should().Be(1); // Still completes successfully because of internal try-catch
            dueAuction.Status.Should().Be("EndedNoBid");
        }

        #endregion

        #region Boundary Tests (B)

        [Fact]
        public async Task ProcessDueAuctionsAsync_ShouldOnlyProcessAuctionsWhereEndTimeIsBeforeOrEqualNow()
        {
            // Arrange
            var baseTime = DateTime.UtcNow.AddHours(7);
            
            // Ended exact at now (due)
            var dueAuction1 = new Auction { AuctionId = "auc_due_exact", Status = "Ongoing", EndTime = baseTime }; 
            // Ended in past (due)
            var dueAuction2 = new Auction { AuctionId = "auc_due_past", Status = "Ongoing", EndTime = baseTime.AddMinutes(-5) }; 
            // Ends in future (not due)
            var activeAuction = new Auction { AuctionId = "auc_active", Status = "Ongoing", EndTime = baseTime.AddMinutes(5) }; 

            _contextAuctions.AddRange(new[] { dueAuction1, dueAuction2, activeAuction });
            _context.Setup(c => c.Auction).Returns(_contextAuctions.AsMockDbSet().Object);

            _auctionRepository.Setup(x => x.GetByIdAsync("auc_due_exact")).ReturnsAsync(dueAuction1);
            _auctionRepository.Setup(x => x.GetByIdAsync("auc_due_past")).ReturnsAsync(dueAuction2);

            // Act
            var result = await _service.ProcessDueAuctionsAsync(CancellationToken.None);

            // Assert
            result.Should().Be(2); // Only due_exact and due_past processed
        }

        #endregion
    }
}
