using System;
using System.Threading.Tasks;
using AutoMapper;
using FluentAssertions;
using Moq;
using RetradeBE.Mappings;
using RetradeBE.Models;
using RetradeBE.Models.DTOs;
using RetradeBE.Repositories;
using RetradeBE.Services;
using Xunit;

namespace Test.ReportTests
{
    public class ReportReportBuyerTests
    {
        private readonly Mock<IReportRepository> _reportRepository;
        private readonly Mock<IOrderService> _orderService;
        private readonly Mock<IAccountService> _accountService;
        private readonly Mock<IUserService> _userService;
        private readonly Mock<IProductService> _productService;
        private readonly Mock<IReviewService> _reviewService;
        private readonly IMapper _mapper;
        private readonly Mock<INotificationService> _notificationService;
        private readonly ReportService _service;

        public ReportReportBuyerTests()
        {
            _reportRepository = new Mock<IReportRepository>();
            _orderService = new Mock<IOrderService>();
            _accountService = new Mock<IAccountService>();
            _userService = new Mock<IUserService>();
            _productService = new Mock<IProductService>();
            _reviewService = new Mock<IReviewService>();

            var configuration = new AutoMapper.MapperConfiguration(cfg =>
            {
                cfg.AddProfile<AutoMapperProfile>();
            }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
            _mapper = configuration.CreateMapper();
            _notificationService = new Mock<INotificationService>();

            _service = new ReportService(
                _reportRepository.Object,
                _orderService.Object,
                _accountService.Object,
                _userService.Object,
                _productService.Object,
                _reviewService.Object,
                _mapper,
                _notificationService.Object
            );
        }

        #region Normal Tests (N)
        [Fact]
        public async Task ReportBuyerAsync_ShouldCreateReport_WhenParametersAreValid()
        {
            // Arrange
            string accountId = "acc_id";
            string orderId = "order_id";
            string reporterId = "seller_id";
            string buyerId = "buyer_id";
            var account = new Account { AccountId = accountId, UserId = reporterId };
            var order = new Order { OrderId = orderId, Status = "Completed", SellerId = reporterId, BuyerId = buyerId };
            var buyer = new User { UserId = buyerId };
            var request = new ReportCreateDto { Reason = "Scam", Description = "Fake payment slip" };

            _accountService.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);
            _orderService.Setup(x => x.GetByIdAsync(orderId)).ReturnsAsync(order);
            _userService.Setup(x => x.GetByIdAsync(buyerId)).ReturnsAsync(buyer);
            _reportRepository.Setup(x => x.ExistsAsync(orderId, reporterId, "buyer")).ReturnsAsync(false);
            _reportRepository.Setup(x => x.AddAsync(It.IsAny<Report>())).Returns(Task.CompletedTask);

            // Act
            var result = await _service.ReportBuyerAsync(accountId, orderId, request);

            // Assert
            result.Should().NotBeNull();
            result.Reason.Should().Be("Scam");
            result.Description.Should().Be("Fake payment slip");
            result.TargetId.Should().Be(orderId);
            result.ReporterId.Should().Be(reporterId);
            result.TargetType.Should().Be("buyer");
            result.Status.Should().Be("Pending");

            _reportRepository.Verify(x => x.AddAsync(It.Is<Report>(r =>
                r.ReporterId == reporterId &&
                r.TargetId == orderId &&
                r.TargetType == "buyer" &&
                r.Reason == "Scam" &&
                r.Description == "Fake payment slip"
            )), Times.Once);
        }
        #endregion

        #region Abnormal Tests (A)
        [Fact]
        public async Task ReportBuyerAsync_ShouldThrowUnauthorizedAccessException_WhenAccountNotFound()
        {
            // Arrange
            string accountId = "invalid_acc";
            _accountService.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync((Account?)null);

            // Act
            Func<Task> act = async () => await _service.ReportBuyerAsync(accountId, "order_id", new ReportCreateDto());

            // Assert
            await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("Account not found.");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task ReportBuyerAsync_ShouldThrowInvalidOperationException_WhenOrderIdIsNullOrWhiteSpace(string? orderId)
        {
            // Arrange
            string accountId = "acc_id";
            var account = new Account { AccountId = accountId, UserId = "user_id" };
            _accountService.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);

            // Act
            Func<Task> act = async () => await _service.ReportBuyerAsync(accountId, orderId!, new ReportCreateDto());

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("OrderId is required.");
        }

        [Fact]
        public async Task ReportBuyerAsync_ShouldThrowInvalidOperationException_WhenRequestOrReasonIsMissing()
        {
            // Arrange
            string accountId = "acc_id";
            var account = new Account { AccountId = accountId, UserId = "user_id" };
            _accountService.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);

            // Act & Assert
            Func<Task> actWithNullRequest = async () => await _service.ReportBuyerAsync(accountId, "order_id", null!);
            await actWithNullRequest.Should().ThrowAsync<InvalidOperationException>().WithMessage("Report reason is required.");

            Func<Task> actWithEmptyReason = async () => await _service.ReportBuyerAsync(accountId, "order_id", new ReportCreateDto { Reason = "" });
            await actWithEmptyReason.Should().ThrowAsync<InvalidOperationException>().WithMessage("Report reason is required.");
        }

        [Fact]
        public async Task ReportBuyerAsync_ShouldThrowKeyNotFoundException_WhenOrderNotFound()
        {
            // Arrange
            string accountId = "acc_id";
            string orderId = "order_id";
            var account = new Account { AccountId = accountId, UserId = "user_id" };
            _accountService.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);
            _orderService.Setup(x => x.GetByIdAsync(orderId)).ReturnsAsync((Order?)null);

            // Act
            Func<Task> act = async () => await _service.ReportBuyerAsync(accountId, orderId, new ReportCreateDto { Reason = "Scam" });

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("Order not found.");
        }

        [Fact]
        public async Task ReportBuyerAsync_ShouldThrowInvalidOperationException_WhenOrderIsNotCompleted()
        {
            // Arrange
            string accountId = "acc_id";
            string orderId = "order_id";
            var account = new Account { AccountId = accountId, UserId = "user_id" };
            var order = new Order { OrderId = orderId, Status = "Pending" };

            _accountService.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);
            _orderService.Setup(x => x.GetByIdAsync(orderId)).ReturnsAsync(order);

            // Act
            Func<Task> act = async () => await _service.ReportBuyerAsync(accountId, orderId, new ReportCreateDto { Reason = "Scam" });

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Only completed orders can be reported.");
        }

        [Theory]
        [InlineData(null, "buyer_id")]
        [InlineData("seller_id", null)]
        [InlineData("", "buyer_id")]
        [InlineData("seller_id", "")]
        public async Task ReportBuyerAsync_ShouldThrowInvalidOperationException_WhenOrderLacksBuyerOrSeller(string? sellerId, string? buyerId)
        {
            // Arrange
            string accountId = "acc_id";
            string orderId = "order_id";
            var account = new Account { AccountId = accountId, UserId = "user_id" };
            var order = new Order { OrderId = orderId, Status = "Completed", SellerId = sellerId, BuyerId = buyerId };

            _accountService.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);
            _orderService.Setup(x => x.GetByIdAsync(orderId)).ReturnsAsync(order);

            // Act
            Func<Task> act = async () => await _service.ReportBuyerAsync(accountId, orderId, new ReportCreateDto { Reason = "Scam" });

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("The order does not have a valid buyer or seller.");
        }

        [Fact]
        public async Task ReportBuyerAsync_ShouldThrowUnauthorizedAccessException_WhenReporterIsNotSellerOfOrder()
        {
            // Arrange
            string accountId = "acc_id";
            string orderId = "order_id";
            string reporterId = "user_id";
            var account = new Account { AccountId = accountId, UserId = reporterId };
            var order = new Order { OrderId = orderId, Status = "Completed", SellerId = "other_seller_id", BuyerId = "buyer_id" };

            _accountService.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);
            _orderService.Setup(x => x.GetByIdAsync(orderId)).ReturnsAsync(order);

            // Act
            Func<Task> act = async () => await _service.ReportBuyerAsync(accountId, orderId, new ReportCreateDto { Reason = "Scam" });

            // Assert
            await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("Only the seller of the order can report the buyer.");
        }

        [Fact]
        public async Task ReportBuyerAsync_ShouldThrowKeyNotFoundException_WhenBuyerNotFound()
        {
            // Arrange
            string accountId = "acc_id";
            string orderId = "order_id";
            string reporterId = "seller_id";
            var account = new Account { AccountId = accountId, UserId = reporterId };
            var order = new Order { OrderId = orderId, Status = "Completed", SellerId = reporterId, BuyerId = "buyer_id" };

            _accountService.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);
            _orderService.Setup(x => x.GetByIdAsync(orderId)).ReturnsAsync(order);
            _userService.Setup(x => x.GetByIdAsync("buyer_id")).ReturnsAsync((User?)null);

            // Act
            Func<Task> act = async () => await _service.ReportBuyerAsync(accountId, orderId, new ReportCreateDto { Reason = "Scam" });

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("Buyer not found.");
        }

        [Fact]
        public async Task ReportBuyerAsync_ShouldThrowInvalidOperationException_WhenAlreadyReported()
        {
            // Arrange
            string accountId = "acc_id";
            string orderId = "order_id";
            string reporterId = "seller_id";
            string buyerId = "buyer_id";
            var account = new Account { AccountId = accountId, UserId = reporterId };
            var order = new Order { OrderId = orderId, Status = "Completed", SellerId = reporterId, BuyerId = buyerId };
            var buyer = new User { UserId = buyerId };

            _accountService.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);
            _orderService.Setup(x => x.GetByIdAsync(orderId)).ReturnsAsync(order);
            _userService.Setup(x => x.GetByIdAsync(buyerId)).ReturnsAsync(buyer);
            _reportRepository.Setup(x => x.ExistsAsync(orderId, reporterId, "buyer")).ReturnsAsync(true);

            // Act
            Func<Task> act = async () => await _service.ReportBuyerAsync(accountId, orderId, new ReportCreateDto { Reason = "Scam" });

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("You have already reported this buyer for this order.");
        }
        #endregion

        #region Boundary Tests (B)
        [Fact]
        public async Task ReportBuyerAsync_ShouldTrimReasonAndDescription_WhenTheyHaveLeadingOrTrailingWhitespaces()
        {
            // Arrange
            string accountId = "acc_id";
            string orderId = "order_id";
            string reporterId = "seller_id";
            string buyerId = "buyer_id";
            var account = new Account { AccountId = accountId, UserId = reporterId };
            var order = new Order { OrderId = orderId, Status = "Completed", SellerId = reporterId, BuyerId = buyerId };
            var buyer = new User { UserId = buyerId };
            var request = new ReportCreateDto { Reason = "   Scam   ", Description = "   Fake payment slip   " };

            _accountService.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);
            _orderService.Setup(x => x.GetByIdAsync(orderId)).ReturnsAsync(order);
            _userService.Setup(x => x.GetByIdAsync(buyerId)).ReturnsAsync(buyer);
            _reportRepository.Setup(x => x.ExistsAsync(orderId, reporterId, "buyer")).ReturnsAsync(false);
            _reportRepository.Setup(x => x.AddAsync(It.IsAny<Report>())).Returns(Task.CompletedTask);

            // Act
            var result = await _service.ReportBuyerAsync(accountId, orderId, request);

            // Assert
            result.Should().NotBeNull();
            result.Reason.Should().Be("Scam");
            result.Description.Should().Be("Fake payment slip");

            _reportRepository.Verify(x => x.AddAsync(It.Is<Report>(r =>
                r.Reason == "Scam" &&
                r.Description == "Fake payment slip"
            )), Times.Once);
        }

        [Fact]
        public async Task ReportBuyerAsync_ShouldCreateReportWithNullDescription_WhenDescriptionIsNull()
        {
            // Arrange
            string accountId = "acc_id";
            string orderId = "order_id";
            string reporterId = "seller_id";
            string buyerId = "buyer_id";
            var account = new Account { AccountId = accountId, UserId = reporterId };
            var order = new Order { OrderId = orderId, Status = "Completed", SellerId = reporterId, BuyerId = buyerId };
            var buyer = new User { UserId = buyerId };
            var request = new ReportCreateDto { Reason = "Scam", Description = null };

            _accountService.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);
            _orderService.Setup(x => x.GetByIdAsync(orderId)).ReturnsAsync(order);
            _userService.Setup(x => x.GetByIdAsync(buyerId)).ReturnsAsync(buyer);
            _reportRepository.Setup(x => x.ExistsAsync(orderId, reporterId, "buyer")).ReturnsAsync(false);
            _reportRepository.Setup(x => x.AddAsync(It.IsAny<Report>())).Returns(Task.CompletedTask);

            // Act
            var result = await _service.ReportBuyerAsync(accountId, orderId, request);

            // Assert
            result.Should().NotBeNull();
            result.Description.Should().BeNull();

            _reportRepository.Verify(x => x.AddAsync(It.Is<Report>(r =>
                r.Reason == "Scam" &&
                r.Description == null
            )), Times.Once);
        }

        [Theory]
        [InlineData("COMPLETED")]
        [InlineData("completed")]
        [InlineData("CoMpLeTeD")]
        public async Task ReportBuyerAsync_ShouldProceedSuccessfully_WhenOrderStatusIsCompletedInDifferentCase(string status)
        {
            // Arrange
            string accountId = "acc_id";
            string orderId = "order_id";
            string reporterId = "seller_id";
            string buyerId = "buyer_id";
            var account = new Account { AccountId = accountId, UserId = reporterId };
            var order = new Order { OrderId = orderId, Status = status, SellerId = reporterId, BuyerId = buyerId };
            var buyer = new User { UserId = buyerId };
            var request = new ReportCreateDto { Reason = "Scam" };

            _accountService.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);
            _orderService.Setup(x => x.GetByIdAsync(orderId)).ReturnsAsync(order);
            _userService.Setup(x => x.GetByIdAsync(buyerId)).ReturnsAsync(buyer);
            _reportRepository.Setup(x => x.ExistsAsync(orderId, reporterId, "buyer")).ReturnsAsync(false);
            _reportRepository.Setup(x => x.AddAsync(It.IsAny<Report>())).Returns(Task.CompletedTask);

            // Act
            var result = await _service.ReportBuyerAsync(accountId, orderId, request);

            // Assert
            result.Should().NotBeNull();
            _reportRepository.Verify(x => x.AddAsync(It.IsAny<Report>()), Times.Once);
        }
        #endregion
    }
}

