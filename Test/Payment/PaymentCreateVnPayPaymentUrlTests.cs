using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using RetradeBE.Config;
using RetradeBE.Data;
using RetradeBE.Hubs;
using RetradeBE.Models;
using RetradeBE.Models.DTOs;
using RetradeBE.Services;
using Xunit;

namespace Test.PaymentTests
{
    public class PaymentCreateVnPayPaymentUrlTests
    {
        private readonly Mock<AppDbContext> _context;
        private readonly VnPaySettings _validSettings;
        private readonly Mock<ILogger<PaymentService>> _logger;
        private readonly Mock<IHubContext<OrderHub>> _orderHub;
        private readonly Mock<IHubContext<AuctionHub>> _auctionHub;
        private readonly Mock<ISubscriptionVoucherService> _subscriptionVoucherService;

        public PaymentCreateVnPayPaymentUrlTests()
        {
            _context = new Mock<AppDbContext>();
            _logger = new Mock<ILogger<PaymentService>>();
            _orderHub = new Mock<IHubContext<OrderHub>>();
            _auctionHub = new Mock<IHubContext<AuctionHub>>();
            _subscriptionVoucherService = new Mock<ISubscriptionVoucherService>();

            _validSettings = new VnPaySettings
            {
                TmnCode = "TEST_TMN",
                HashSecret = "TEST_SECRET_KEY_1234567890",
                BaseUrl = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html",
                CallbackUrl = "http://localhost:5000/api/payment/vnpay-return",
                Version = "2.1.0",
                Command = "pay",
                CurrencyCode = "VND",
                Locale = "vn"
            };
        }

        private PaymentService CreateService(VnPaySettings settings)
        {
            var options = Options.Create(settings);
            return new PaymentService(
                _context.Object,
                options,
                _logger.Object,
                _orderHub.Object,
                _auctionHub.Object,
                _subscriptionVoucherService.Object
            );
        }

        #region Normal Tests (N)
        [Fact]
        public async Task CreateVnPayPaymentUrlAsync_ShouldCreatePaymentUrl_WhenRequestIsValid()
        {
            // Arrange
            var service = CreateService(_validSettings);
            var accountId = "acc_100";
            var userId = "user_100";
            _accountSetupHelper(accountId, userId);

            // 1. Order payment
            var orderId = "order_200";
            _context.Setup(c => c.Order).Returns(new List<Order> { new Order { OrderId = orderId, BuyerId = userId } }.AsMockDbSet().Object);
            _context.Setup(c => c.Payment).Returns(new List<Payment>().AsMockDbSet().Object);
            _context.Setup(c => c.SaveChangesAsync(default)).ReturnsAsync(1);

            var reqOrder = new CreateVnPayPaymentRequestDto { OrderId = orderId, Amount = 100000m, OrderDescription = "Order Payment" };
            var resOrder = await service.CreateVnPayPaymentUrlAsync(accountId, reqOrder, "127.0.0.1");
            resOrder.Should().NotBeNull();
            resOrder.PaymentUrl.Should().Contain(_validSettings.BaseUrl);

            // 2. Service payment
            var serviceId = "srv_300";
            _context.Setup(c => c.ServiceSubscription).Returns(new List<ServiceSubscription> { new ServiceSubscription { ServiceId = serviceId } }.AsMockDbSet().Object);
            var reqSrv = new CreateVnPayPaymentRequestDto { ServiceId = serviceId, Amount = 200000m, OrderDescription = "Service Payment" };
            var resSrv = await service.CreateVnPayPaymentUrlAsync(accountId, reqSrv, "127.0.0.1");
            resSrv.Should().NotBeNull();

            // 3. Auction deposit payment
            var depositId = "adep_400";
            var auction = new Auction { AuctionId = "auc_500", Status = "Ongoing", EndTime = DateTime.UtcNow.AddDays(2) };
            var deposit = new AuctionDeposit { AuctionDepositId = depositId, UserId = userId, AuctionId = "auc_500", Auction = auction, Status = "Pending", DepositAmount = 50000m };
            _context.Setup(c => c.AuctionDeposit).Returns(new List<AuctionDeposit> { deposit }.AsMockDbSet().Object);
            var reqDep = new CreateVnPayPaymentRequestDto { AuctionDepositId = depositId, Amount = 50000m, OrderDescription = "Deposit Payment" };
            var resDep = await service.CreateVnPayPaymentUrlAsync(accountId, reqDep, "127.0.0.1");
            resDep.Should().NotBeNull();
        }

        [Fact]
        public async Task CreateVnPayPaymentUrlAsync_ShouldHandleBankCodeAndDefaultLocale_Correctly()
        {
            // Arrange
            var service = CreateService(_validSettings);
            var accountId = "acc_100";
            _accountSetupHelper(accountId, "user_100");
            _context.Setup(c => c.Payment).Returns(new List<Payment>().AsMockDbSet().Object);
            _context.Setup(c => c.SaveChangesAsync(default)).ReturnsAsync(1);

            // Scenario 1: BankCode provided
            var reqBank = new CreateVnPayPaymentRequestDto { Amount = 50000m, OrderDescription = "Desc", BankCode = "NCB" };
            var resBank = await service.CreateVnPayPaymentUrlAsync(accountId, reqBank, "127.0.0.1");
            resBank.PaymentUrl.Should().Contain("vnp_BankCode=NCB");

            // Scenario 2: Empty Locale defaults to vn
            var reqLocale = new CreateVnPayPaymentRequestDto { Amount = 50000m, OrderDescription = "Desc", Locale = "  " };
            var resLocale = await service.CreateVnPayPaymentUrlAsync(accountId, reqLocale, "127.0.0.1");
            resLocale.PaymentUrl.Should().Contain("vnp_Locale=vn");
        }
        #endregion

        #region Abnormal Tests (A)
        [Fact]
        public async Task CreateVnPayPaymentUrlAsync_ShouldThrowInvalidOperationException_WhenSettingsOrAccountInvalid()
        {
            // Scenario 1: Invalid settings
            var invalidService = CreateService(new VnPaySettings { TmnCode = "" });
            Func<Task> actSettings = async () => await invalidService.CreateVnPayPaymentUrlAsync("acc_100", new CreateVnPayPaymentRequestDto(), "127.0.0.1");
            await actSettings.Should().ThrowAsync<InvalidOperationException>().WithMessage("*VNPAY settings are not configured properly.*");

            // Scenario 2: Account not found
            var service = CreateService(_validSettings);
            _context.Setup(c => c.Account).Returns(new List<Account>().AsMockDbSet().Object);
            Func<Task> actAccNotFound = async () => await service.CreateVnPayPaymentUrlAsync("invalid_acc", new CreateVnPayPaymentRequestDto(), "127.0.0.1");
            await actAccNotFound.Should().ThrowAsync<InvalidOperationException>().WithMessage("Account not found.");

            // Scenario 3: Account unlinked
            _context.Setup(c => c.Account).Returns(new List<Account> { new Account { AccountId = "acc_unlinked", UserId = null } }.AsMockDbSet().Object);
            Func<Task> actUnlinked = async () => await service.CreateVnPayPaymentUrlAsync("acc_unlinked", new CreateVnPayPaymentRequestDto(), "127.0.0.1");
            await actUnlinked.Should().ThrowAsync<InvalidOperationException>().WithMessage("Account not found.");
        }

        [Fact]
        public async Task CreateVnPayPaymentUrlAsync_ShouldThrowInvalidOperationException_WhenOrderOrServiceNotFound()
        {
            var service = CreateService(_validSettings);
            _accountSetupHelper("acc_100", "user_100");

            // Scenario 1: Order not found
            _context.Setup(c => c.Order).Returns(new List<Order>().AsMockDbSet().Object);
            Func<Task> actOrder = async () => await service.CreateVnPayPaymentUrlAsync("acc_100", new CreateVnPayPaymentRequestDto { OrderId = "invalid_order", Amount = 50000m, OrderDescription = "Desc" }, "127.0.0.1");
            await actOrder.Should().ThrowAsync<InvalidOperationException>().WithMessage("Order not found.");

            // Scenario 2: Service subscription not found
            _context.Setup(c => c.ServiceSubscription).Returns(new List<ServiceSubscription>().AsMockDbSet().Object);
            Func<Task> actSrv = async () => await service.CreateVnPayPaymentUrlAsync("acc_100", new CreateVnPayPaymentRequestDto { ServiceId = "invalid_service", Amount = 50000m, OrderDescription = "Desc" }, "127.0.0.1");
            await actSrv.Should().ThrowAsync<InvalidOperationException>().WithMessage("Service package not found.");
        }

        [Fact]
        public async Task CreateVnPayPaymentUrlAsync_ShouldThrowInvalidOperationException_WhenAuctionDepositNotFoundOrStatusInvalid()
        {
            var service = CreateService(_validSettings);
            _accountSetupHelper("acc_100", "user_100");

            // Scenario 1: Deposit not found
            _context.Setup(c => c.AuctionDeposit).Returns(new List<AuctionDeposit>().AsMockDbSet().Object);
            Func<Task> actNotFound = async () => await service.CreateVnPayPaymentUrlAsync("acc_100", new CreateVnPayPaymentRequestDto { AuctionDepositId = "invalid_dep", Amount = 50000m, OrderDescription = "Desc" }, "127.0.0.1");
            await actNotFound.Should().ThrowAsync<InvalidOperationException>().WithMessage("Auction deposit not found.");

            // Scenario 2: Deposit status not Pending or Paid
            var refundedDeposit = new AuctionDeposit { AuctionDepositId = "adep_refunded", UserId = "user_100", Status = "Refunded" };
            _context.Setup(c => c.AuctionDeposit).Returns(new List<AuctionDeposit> { refundedDeposit }.AsMockDbSet().Object);
            Func<Task> actStatus = async () => await service.CreateVnPayPaymentUrlAsync("acc_100", new CreateVnPayPaymentRequestDto { AuctionDepositId = "adep_refunded", Amount = 50000m, OrderDescription = "Desc" }, "127.0.0.1");
            await actStatus.Should().ThrowAsync<InvalidOperationException>().WithMessage("Auction deposit is not available for payment.");
        }

        [Fact]
        public async Task CreateVnPayPaymentUrlAsync_ShouldThrowInvalidOperationException_WhenAuctionEndedOrUnavailable()
        {
            var service = CreateService(_validSettings);
            _accountSetupHelper("acc_100", "user_100");

            var endedAuction = new Auction { AuctionId = "auc_ended", Status = "Ended" };
            var deposit = new AuctionDeposit { AuctionDepositId = "adep_ended", UserId = "user_100", Status = "Pending", Auction = endedAuction };
            _context.Setup(c => c.AuctionDeposit).Returns(new List<AuctionDeposit> { deposit }.AsMockDbSet().Object);

            Func<Task> act = async () => await service.CreateVnPayPaymentUrlAsync("acc_100", new CreateVnPayPaymentRequestDto { AuctionDepositId = "adep_ended", Amount = 50000m, OrderDescription = "Desc" }, "127.0.0.1");
            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("This auction is not available for deposit payment.");
        }
        #endregion

        #region Boundary Tests (B)
        [Fact]
        public async Task CreateVnPayPaymentUrlAsync_ShouldThrowInvalidOperationException_WhenDepositAmountIsLessThanMinimumOrMismatched()
        {
            var service = CreateService(_validSettings);
            _accountSetupHelper("acc_100", "user_100");

            var auction = new Auction { AuctionId = "auc_ongoing", Status = "Ongoing", EndTime = DateTime.UtcNow.AddDays(1) };
            var deposit = new AuctionDeposit { AuctionDepositId = "adep_valid", UserId = "user_100", Status = "Pending", DepositAmount = 50000m, Auction = auction };
            _context.Setup(c => c.AuctionDeposit).Returns(new List<AuctionDeposit> { deposit }.AsMockDbSet().Object);

            // Scenario 1: Amount < 20,000 VND
            var reqLow = new CreateVnPayPaymentRequestDto { AuctionDepositId = "adep_valid", Amount = 10000m, OrderDescription = "Desc" };
            Func<Task> actLow = async () => await service.CreateVnPayPaymentUrlAsync("acc_100", reqLow, "127.0.0.1");
            await actLow.Should().ThrowAsync<InvalidOperationException>().WithMessage("Deposit amount must be at least 20,000 VND.");

            // Scenario 2: Amount mismatch
            var reqMismatch = new CreateVnPayPaymentRequestDto { AuctionDepositId = "adep_valid", Amount = 30000m, OrderDescription = "Desc" };
            Func<Task> actMismatch = async () => await service.CreateVnPayPaymentUrlAsync("acc_100", reqMismatch, "127.0.0.1");
            await actMismatch.Should().ThrowAsync<InvalidOperationException>().WithMessage("Payment amount does not match auction deposit.");
        }
        #endregion

        private void _accountSetupHelper(string accountId, string userId)
        {
            var account = new Account { AccountId = accountId, UserId = userId };
            _context.Setup(c => c.Account).Returns(new List<Account> { account }.AsMockDbSet().Object);
        }
    }
}
