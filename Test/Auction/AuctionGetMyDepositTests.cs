using System;
using System.Collections.Generic;
using System.Linq;
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
    public class AuctionGetMyDepositTests
    {
        private readonly Mock<IAuctionRepository> _auctionRepository;
        private readonly Mock<IAccountRepository> _accountRepository;
        private readonly Mock<AppDbContext> _context;
        private readonly Mock<IPaymentService> _paymentService;
        private readonly Mock<IHubContext<AuctionHub>> _auctionHub;
        private readonly Mock<INotificationService> _notificationService;
        private readonly AuctionService _service;

        public AuctionGetMyDepositTests()
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
        public async Task GetMyDepositAsync_ShouldReturnMappedDto_WhenDepositExists()
        {
            // Arrange
            string accountId = "acc_100";
            string userId = "user_200";
            string auctionId = "auc_300";

            var account = new Account { AccountId = accountId, UserId = userId };
            _accountRepository.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);

            var deposit = new AuctionDeposit
            {
                AuctionDepositId = "adep_500",
                AuctionId = auctionId,
                UserId = userId,
                DepositAmount = 50000m,
                Status = "Paid",
                PolicyAccepted = true,
                CreatedAt = DateTime.UtcNow
            };

            var deposits = new List<AuctionDeposit> { deposit };
            _context.Setup(c => c.AuctionDeposit).Returns(deposits.AsMockDbSet().Object);

            // Mock spent bids in context.Bid
            var bids = new List<Bid>(); // No bids placed yet
            _context.Setup(c => c.Bid).Returns(bids.AsMockDbSet().Object);

            // Act
            var result = await _service.GetMyDepositAsync(accountId, auctionId);

            // Assert
            result.Should().NotBeNull();
            result!.AuctionDepositId.Should().Be("adep_500");
            result.AuctionId.Should().Be(auctionId);
            result.UserId.Should().Be(userId);
            result.TotalDepositAmount.Should().Be(50000m);
            result.DepositAmount.Should().Be(50000m); // Available limit (50k total - 0 spent)
            result.Status.Should().Be("Paid");
            result.PolicyAccepted.Should().BeTrue();

            _accountRepository.Verify(x => x.GetByIdAsync(accountId), Times.Once);
        }

        #endregion

        #region Abnormal Tests (A)

        [Fact]
        public async Task GetMyDepositAsync_ShouldThrowException_WhenAccountDoesNotExist()
        {
            // Arrange
            string accountId = "invalid_acc";
            string auctionId = "auc_300";

            _accountRepository.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync((Account?)null);

            // Act
            Func<Task> act = async () => await _service.GetMyDepositAsync(accountId, auctionId);

            // Assert
            await act.Should().ThrowAsync<Exception>().WithMessage("Account does not exist.");
        }

        [Fact]
        public async Task GetMyDepositAsync_ShouldThrowException_WhenAccountNotLinkedToUser()
        {
            // Arrange
            string accountId = "acc_100";
            string auctionId = "auc_300";

            var account = new Account { AccountId = accountId, UserId = null }; // Not linked to user info
            _accountRepository.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);

            // Act
            Func<Task> act = async () => await _service.GetMyDepositAsync(accountId, auctionId);

            // Assert
            await act.Should().ThrowAsync<Exception>().WithMessage("Account is not linked to a user.");
        }

        #endregion

        #region Boundary Tests (B)

        [Fact]
        public async Task GetMyDepositAsync_ShouldReturnNull_WhenNoDepositExists()
        {
            // Arrange
            string accountId = "acc_100";
            string userId = "user_200";
            string auctionId = "auc_300";

            var account = new Account { AccountId = accountId, UserId = userId };
            _accountRepository.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);

            // Empty deposits
            var deposits = new List<AuctionDeposit>();
            _context.Setup(c => c.AuctionDeposit).Returns(deposits.AsMockDbSet().Object);

            // Act
            var result = await _service.GetMyDepositAsync(accountId, auctionId);

            // Assert
            result.Should().BeNull();

            _accountRepository.Verify(x => x.GetByIdAsync(accountId), Times.Once);
        }

        #endregion
    }
}
