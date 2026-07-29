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
    public class ReportReportSellerTests
    {
        private readonly Mock<IReportRepository> _reportRepository;
        private readonly Mock<IOrderService> _orderService;
        private readonly Mock<IAccountService> _accountService;
        private readonly Mock<IUserService> _userService;
        private readonly Mock<IProductService> _productService;
        private readonly Mock<IReviewService> _reviewService;
        private readonly IMapper _mapper;
        private readonly ReportService _service;

        public ReportReportSellerTests()
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
            }, new Mock<Microsoft.Extensions.Logging.ILoggerFactory>().Object);
            _mapper = configuration.CreateMapper();

            _service = new ReportService(
                _reportRepository.Object,
                _orderService.Object,
                _accountService.Object,
                _userService.Object,
                _productService.Object,
                _reviewService.Object,
                _mapper
            );
        }

        [Fact]
        public async Task ReportSellerAsync_ShouldThrowUnauthorizedAccessException_WhenAccountNotFound()
        {
            // Arrange
            string accountId = "invalid_acc";
            _accountService.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync((Account?)null);

            // Act
            Func<Task> act = async () => await _service.ReportSellerAsync(accountId, "order_id", new ReportCreateDto());

            // Assert
            await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("Account not found.");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task ReportSellerAsync_ShouldThrowInvalidOperationException_WhenOrderIdIsNullOrWhiteSpace(string? orderId)
        {
            // Arrange
            string accountId = "acc_id";
            var account = new Account { AccountId = accountId, UserId = "user_id" };
            _accountService.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);

            // Act
            Func<Task> act = async () => await _service.ReportSellerAsync(accountId, orderId!, new ReportCreateDto());

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("OrderId is required.");
        }

        [Fact]
        public async Task ReportSellerAsync_ShouldThrowInvalidOperationException_WhenRequestOrReasonIsMissing()
        {
            // Arrange
            string accountId = "acc_id";
            var account = new Account { AccountId = accountId, UserId = "user_id" };
            _accountService.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);

            // Act & Assert
            Func<Task> actWithNullRequest = async () => await _service.ReportSellerAsync(accountId, "order_id", null!);
            await actWithNullRequest.Should().ThrowAsync<InvalidOperationException>().WithMessage("Report reason is required.");

            Func<Task> actWithEmptyReason = async () => await _service.ReportSellerAsync(accountId, "order_id", new ReportCreateDto { Reason = "" });
            await actWithEmptyReason.Should().ThrowAsync<InvalidOperationException>().WithMessage("Report reason is required.");
        }

        [Fact]
        public async Task ReportSellerAsync_ShouldThrowKeyNotFoundException_WhenOrderNotFound()
        {
            // Arrange
            string accountId = "acc_id";
            string orderId = "order_id";
            var account = new Account { AccountId = accountId, UserId = "user_id" };
            _accountService.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);
            _orderService.Setup(x => x.GetByIdAsync(orderId)).ReturnsAsync((Order?)null);

            // Act
            Func<Task> act = async () => await _service.ReportSellerAsync(accountId, orderId, new ReportCreateDto { Reason = "Scam" });

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("Order not found.");
        }

        [Fact]
        public async Task ReportSellerAsync_ShouldThrowInvalidOperationException_WhenOrderIsNotCompleted()
        {
            // Arrange
            string accountId = "acc_id";
            string orderId = "order_id";
            var account = new Account { AccountId = accountId, UserId = "user_id" };
            var order = new Order { OrderId = orderId, Status = "Pending" };

            _accountService.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);
            _orderService.Setup(x => x.GetByIdAsync(orderId)).ReturnsAsync(order);

            // Act
            Func<Task> act = async () => await _service.ReportSellerAsync(accountId, orderId, new ReportCreateDto { Reason = "Scam" });

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Only completed orders can be reported.");
        }

        [Theory]
        [InlineData(null, "buyer_id")]
        [InlineData("seller_id", null)]
        [InlineData("", "buyer_id")]
        [InlineData("seller_id", "")]
        public async Task ReportSellerAsync_ShouldThrowInvalidOperationException_WhenOrderLacksBuyerOrSeller(string? sellerId, string? buyerId)
        {
            // Arrange
            string accountId = "acc_id";
            string orderId = "order_id";
            var account = new Account { AccountId = accountId, UserId = "user_id" };
            var order = new Order { OrderId = orderId, Status = "Completed", SellerId = sellerId, BuyerId = buyerId };

            _accountService.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);
            _orderService.Setup(x => x.GetByIdAsync(orderId)).ReturnsAsync(order);

            // Act
            Func<Task> act = async () => await _service.ReportSellerAsync(accountId, orderId, new ReportCreateDto { Reason = "Scam" });

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("The order does not have a valid buyer or seller.");
        }

        [Fact]
        public async Task ReportSellerAsync_ShouldThrowUnauthorizedAccessException_WhenReporterIsNotBuyerOfOrder()
        {
            // Arrange
            string accountId = "acc_id";
            string orderId = "order_id";
            string reporterId = "user_id";
            var account = new Account { AccountId = accountId, UserId = reporterId };
            var order = new Order { OrderId = orderId, Status = "Completed", SellerId = "seller_id", BuyerId = "other_buyer_id" };

            _accountService.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);
            _orderService.Setup(x => x.GetByIdAsync(orderId)).ReturnsAsync(order);

            // Act
            Func<Task> act = async () => await _service.ReportSellerAsync(accountId, orderId, new ReportCreateDto { Reason = "Scam" });

            // Assert
            await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("Only the buyer of the order can report the seller.");
        }

        [Fact]
        public async Task ReportSellerAsync_ShouldThrowKeyNotFoundException_WhenSellerNotFound()
        {
            // Arrange
            string accountId = "acc_id";
            string orderId = "order_id";
            string reporterId = "buyer_id";
            var account = new Account { AccountId = accountId, UserId = reporterId };
            var order = new Order { OrderId = orderId, Status = "Completed", SellerId = "seller_id", BuyerId = reporterId };

            _accountService.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);
            _orderService.Setup(x => x.GetByIdAsync(orderId)).ReturnsAsync(order);
            _userService.Setup(x => x.GetByIdAsync("seller_id")).ReturnsAsync((User?)null);

            // Act
            Func<Task> act = async () => await _service.ReportSellerAsync(accountId, orderId, new ReportCreateDto { Reason = "Scam" });

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("Seller not found.");
        }

        [Fact]
        public async Task ReportSellerAsync_ShouldThrowInvalidOperationException_WhenAlreadyReported()
        {
            // Arrange
            string accountId = "acc_id";
            string orderId = "order_id";
            string reporterId = "buyer_id";
            string sellerId = "seller_id";
            var account = new Account { AccountId = accountId, UserId = reporterId };
            var order = new Order { OrderId = orderId, Status = "Completed", SellerId = sellerId, BuyerId = reporterId };
            var seller = new User { UserId = sellerId };

            _accountService.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);
            _orderService.Setup(x => x.GetByIdAsync(orderId)).ReturnsAsync(order);
            _userService.Setup(x => x.GetByIdAsync(sellerId)).ReturnsAsync(seller);
            _reportRepository.Setup(x => x.ExistsAsync(orderId, reporterId, "seller")).ReturnsAsync(true);

            // Act
            Func<Task> act = async () => await _service.ReportSellerAsync(accountId, orderId, new ReportCreateDto { Reason = "Scam" });

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("You have already reported this seller for this order.");
        }

        [Fact]
        public async Task ReportSellerAsync_ShouldCreateReport_WhenParametersAreValid()
        {
            // Arrange
            string accountId = "acc_id";
            string orderId = "order_id";
            string reporterId = "buyer_id";
            string sellerId = "seller_id";
            var account = new Account { AccountId = accountId, UserId = reporterId };
            var order = new Order { OrderId = orderId, Status = "Completed", SellerId = sellerId, BuyerId = reporterId };
            var seller = new User { UserId = sellerId };
            var request = new ReportCreateDto { Reason = "Scam", Description = "Fake seller listing" };

            _accountService.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);
            _orderService.Setup(x => x.GetByIdAsync(orderId)).ReturnsAsync(order);
            _userService.Setup(x => x.GetByIdAsync(sellerId)).ReturnsAsync(seller);
            _reportRepository.Setup(x => x.ExistsAsync(orderId, reporterId, "seller")).ReturnsAsync(false);
            _reportRepository.Setup(x => x.AddAsync(It.IsAny<Report>())).Returns(Task.CompletedTask);

            // Act
            var result = await _service.ReportSellerAsync(accountId, orderId, request);

            // Assert
            result.Should().NotBeNull();
            result.Reason.Should().Be("Scam");
            result.Description.Should().Be("Fake seller listing");
            result.TargetId.Should().Be(orderId);
            result.ReporterId.Should().Be(reporterId);
            result.TargetType.Should().Be("seller");
            result.Status.Should().Be("Pending");

            _reportRepository.Verify(x => x.AddAsync(It.Is<Report>(r =>
                r.ReporterId == reporterId &&
                r.TargetId == orderId &&
                r.TargetType == "seller" &&
                r.Reason == "Scam" &&
                r.Description == "Fake seller listing"
            )), Times.Once);
        }
    }
}
