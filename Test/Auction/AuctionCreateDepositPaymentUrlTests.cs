using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Moq;
using RetradeBE.Data;
using RetradeBE.Hubs;
using RetradeBE.Models;
using RetradeBE.Models.DTOs;
using RetradeBE.Repositories;
using RetradeBE.Services;
using Xunit;

namespace Test.AuctionTests
{
    public class AuctionCreateDepositPaymentUrlTests
    {
        private readonly Mock<IAuctionRepository> _auctionRepository;
        private readonly Mock<IAccountRepository> _accountRepository;
        private readonly Mock<AppDbContext> _context;
        private readonly Mock<IPaymentService> _paymentService;
        private readonly Mock<IHubContext<AuctionHub>> _auctionHub;
        private readonly Mock<INotificationService> _notificationService;
        private readonly AuctionService _service;

        public AuctionCreateDepositPaymentUrlTests()
        {
            _auctionRepository = new Mock<IAuctionRepository>();
            _accountRepository = new Mock<IAccountRepository>();
            _context = new Mock<AppDbContext>();
            _paymentService = new Mock<IPaymentService>();
            _auctionHub = new Mock<IHubContext<AuctionHub>>();
            _notificationService = new Mock<INotificationService>();

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
        public async Task CreateDepositPaymentUrlAsync_ShouldCreateDepositAndReturnPaymentUrl_WhenRequestIsValidAndNoExistingDeposit()
        {
            // Arrange
            var accountId = "acc_100";
            var userId = "user_100";
            var auctionId = "auc_200";
            var sellerId = "seller_300";
            var ipAddress = "127.0.0.1";

            var account = new Account { AccountId = accountId, UserId = userId };
            _accountRepository.Setup(r => r.GetByIdAsync(accountId)).ReturnsAsync(account);

            var auction = new Auction
            {
                AuctionId = auctionId,
                SellerId = sellerId,
                Status = "Ongoing",
                StartTime = DateTime.UtcNow.AddHours(-1),
                EndTime = DateTime.UtcNow.AddHours(24)
            };
            _auctionRepository.Setup(r => r.GetByIdAsync(auctionId)).ReturnsAsync(auction);

            var deposits = new List<AuctionDeposit>().AsMockDbSet();
            _context.Setup(c => c.AuctionDeposit).Returns(deposits.Object);
            _context.Setup(c => c.SaveChangesAsync(default)).ReturnsAsync(1);

            var dto = new AuctionDepositPaymentRequestDto
            {
                DepositAmount = 50000m,
                PolicyAccepted = true,
                BankCode = "NCB",
                Locale = "vn"
            };

            var expectedResponse = new CreateVnPayPaymentResponseDto { PaymentUrl = "https://vnpay.vn/pay" };
            _paymentService.Setup(p => p.CreateVnPayPaymentUrlAsync(accountId, It.IsAny<CreateVnPayPaymentRequestDto>(), ipAddress))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _service.CreateDepositPaymentUrlAsync(accountId, auctionId, dto, ipAddress);

            // Assert
            result.Should().NotBeNull();
            result.PaymentUrl.Should().Be("https://vnpay.vn/pay");

            _context.Verify(c => c.SaveChangesAsync(default), Times.Once);
            _paymentService.Verify(p => p.CreateVnPayPaymentUrlAsync(accountId, It.Is<CreateVnPayPaymentRequestDto>(
                req => req.Amount == 50000m && req.BankCode == "NCB"
            ), ipAddress), Times.Once);
        }
        #endregion

        #region Abnormal Tests (A)
        [Fact]
        public async Task CreateDepositPaymentUrlAsync_ShouldThrowException_WhenAccountNotFoundOrNotLinkedToUser()
        {
            var dto = new AuctionDepositPaymentRequestDto { DepositAmount = 50000m, PolicyAccepted = true };

            // Scenario 1: Account not found
            _accountRepository.Setup(r => r.GetByIdAsync("acc_invalid")).ReturnsAsync((Account?)null);
            Func<Task> act1 = async () => await _service.CreateDepositPaymentUrlAsync("acc_invalid", "auc_200", dto, "127.0.0.1");
            await act1.Should().ThrowAsync<Exception>().WithMessage("*Account does not exist.*");

            // Scenario 2: Account not linked to a user
            var unlinkedAccount = new Account { AccountId = "acc_unlinked", UserId = null };
            _accountRepository.Setup(r => r.GetByIdAsync("acc_unlinked")).ReturnsAsync(unlinkedAccount);
            Func<Task> act2 = async () => await _service.CreateDepositPaymentUrlAsync("acc_unlinked", "auc_200", dto, "127.0.0.1");
            await act2.Should().ThrowAsync<Exception>().WithMessage("*Account is not linked to a user.*");
        }

        [Fact]
        public async Task CreateDepositPaymentUrlAsync_ShouldThrowException_WhenDepositAmountLessThanMinimumOrPolicyNotAccepted()
        {
            var accountId = "acc_100";
            var userId = "user_100";
            _accountRepository.Setup(r => r.GetByIdAsync(accountId)).ReturnsAsync(new Account { AccountId = accountId, UserId = userId });

            // Scenario 1: Deposit amount < 20,000 VND
            var dtoLowAmount = new AuctionDepositPaymentRequestDto { DepositAmount = 10000m, PolicyAccepted = true };
            Func<Task> actLow = async () => await _service.CreateDepositPaymentUrlAsync(accountId, "auc_200", dtoLowAmount, "127.0.0.1");
            await actLow.Should().ThrowAsync<Exception>().WithMessage("*Deposit amount must be at least 20,000 VND.*");

            // Scenario 2: Policy not accepted
            var dtoNoPolicy = new AuctionDepositPaymentRequestDto { DepositAmount = 50000m, PolicyAccepted = false };
            Func<Task> actNoPolicy = async () => await _service.CreateDepositPaymentUrlAsync(accountId, "auc_200", dtoNoPolicy, "127.0.0.1");
            await actNoPolicy.Should().ThrowAsync<Exception>().WithMessage("*Auction policy must be accepted before paying deposit.*");
        }

        [Fact]
        public async Task CreateDepositPaymentUrlAsync_ShouldThrowException_WhenAuctionNotFoundOrUserIsSeller()
        {
            var accountId = "acc_100";
            var userId = "user_100";
            _accountRepository.Setup(r => r.GetByIdAsync(accountId)).ReturnsAsync(new Account { AccountId = accountId, UserId = userId });
            var dto = new AuctionDepositPaymentRequestDto { DepositAmount = 50000m, PolicyAccepted = true };

            // Scenario 1: Auction not found
            _auctionRepository.Setup(r => r.GetByIdAsync("auc_invalid")).ReturnsAsync((Auction?)null);
            Func<Task> actNotFound = async () => await _service.CreateDepositPaymentUrlAsync(accountId, "auc_invalid", dto, "127.0.0.1");
            await actNotFound.Should().ThrowAsync<Exception>().WithMessage("*Auction not found.*");

            // Scenario 2: User is seller of the auction
            var ownAuction = new Auction { AuctionId = "auc_own", SellerId = userId };
            _auctionRepository.Setup(r => r.GetByIdAsync("auc_own")).ReturnsAsync(ownAuction);
            Func<Task> actSeller = async () => await _service.CreateDepositPaymentUrlAsync(accountId, "auc_own", dto, "127.0.0.1");
            await actSeller.Should().ThrowAsync<Exception>().WithMessage("*You cannot deposit for your own auction.*");
        }

        [Fact]
        public async Task CreateDepositPaymentUrlAsync_ShouldThrowException_WhenAuctionIsInTerminalStatus()
        {
            var accountId = "acc_100";
            var userId = "user_100";
            _accountRepository.Setup(r => r.GetByIdAsync(accountId)).ReturnsAsync(new Account { AccountId = accountId, UserId = userId });
            var dto = new AuctionDepositPaymentRequestDto { DepositAmount = 50000m, PolicyAccepted = true };

            var cancelledAuction = new Auction
            {
                AuctionId = "auc_cancelled",
                SellerId = "seller_300",
                Status = "Cancelled"
            };
            _auctionRepository.Setup(r => r.GetByIdAsync("auc_cancelled")).ReturnsAsync(cancelledAuction);

            Func<Task> act = async () => await _service.CreateDepositPaymentUrlAsync(accountId, "auc_cancelled", dto, "127.0.0.1");
            await act.Should().ThrowAsync<Exception>().WithMessage("*This auction is not available for deposit.*");
        }
        #endregion

        #region Boundary Tests (B)
        [Fact]
        public async Task CreateDepositPaymentUrlAsync_ShouldReuseExistingDeposit_WhenPendingOrPaidDepositExists()
        {
            // Arrange
            var accountId = "acc_100";
            var userId = "user_100";
            var auctionId = "auc_200";
            var sellerId = "seller_300";
            var ipAddress = "127.0.0.1";

            var account = new Account { AccountId = accountId, UserId = userId };
            _accountRepository.Setup(r => r.GetByIdAsync(accountId)).ReturnsAsync(account);

            var auction = new Auction
            {
                AuctionId = auctionId,
                SellerId = sellerId,
                Status = "Ongoing",
                StartTime = DateTime.UtcNow.AddHours(-1),
                EndTime = DateTime.UtcNow.AddHours(24)
            };
            _auctionRepository.Setup(r => r.GetByIdAsync(auctionId)).ReturnsAsync(auction);

            var existingDeposit = new AuctionDeposit
            {
                AuctionDepositId = "ADEP_EXISTING",
                AuctionId = auctionId,
                UserId = userId,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            var deposits = new List<AuctionDeposit> { existingDeposit }.AsMockDbSet();
            _context.Setup(c => c.AuctionDeposit).Returns(deposits.Object);
            _context.Setup(c => c.SaveChangesAsync(default)).ReturnsAsync(1);

            var dto = new AuctionDepositPaymentRequestDto
            {
                DepositAmount = 100000m,
                PolicyAccepted = true
            };

            var expectedResponse = new CreateVnPayPaymentResponseDto { PaymentUrl = "https://vnpay.vn/pay" };
            _paymentService.Setup(p => p.CreateVnPayPaymentUrlAsync(accountId, It.IsAny<CreateVnPayPaymentRequestDto>(), ipAddress))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _service.CreateDepositPaymentUrlAsync(accountId, auctionId, dto, ipAddress);

            // Assert
            result.Should().NotBeNull();
            existingDeposit.DepositAmount.Should().Be(100000m);
            existingDeposit.PolicyAccepted.Should().BeTrue();

            _context.Verify(c => c.SaveChangesAsync(default), Times.Once);
            _paymentService.Verify(p => p.CreateVnPayPaymentUrlAsync(accountId, It.Is<CreateVnPayPaymentRequestDto>(
                req => req.AuctionDepositId == "ADEP_EXISTING" && req.Amount == 100000m
            ), ipAddress), Times.Once);
        }
        #endregion
    }
}
