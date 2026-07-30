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
    public class ReportGetByIdTests
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

        public ReportGetByIdTests()
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

        [Fact]
        public async Task GetByIdAsync_ShouldReturnNull_WhenReportDoesNotExist()
        {
            // Arrange
            string reportId = "invalid_id";
            _reportRepository.Setup(x => x.GetByIdAsync(reportId)).ReturnsAsync((Report?)null);

            // Act
            var result = await _service.GetByIdAsync(reportId);

            // Assert
            result.Should().BeNull();
            _reportRepository.Verify(x => x.GetByIdAsync(reportId), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnDetailsWithReview_WhenTargetTypeIsReview()
        {
            // Arrange
            string reportId = "R1";
            var report = new Report
            {
                ReportId = reportId,
                TargetType = "review",
                TargetId = "review_123",
                Reason = "Inappropriate content"
            };

            var review = new Review
            {
                ReviewId = "review_123",
                Rating = 5,
                Comment = "Great service!"
            };

            _reportRepository.Setup(x => x.GetByIdAsync(reportId)).ReturnsAsync(report);
            _reviewService.Setup(x => x.GetByIdForReportAsync("review_123")).ReturnsAsync(review);

            // Act
            var result = await _service.GetByIdAsync(reportId);

            // Assert
            result.Should().NotBeNull();
            result!.ReportId.Should().Be(reportId);
            result.TargetType.Should().Be("review");
            result.Review.Should().NotBeNull();
            result.Review!.ReviewId.Should().Be("review_123");
            result.Review.Comment.Should().Be("Great service!");

            _reviewService.Verify(x => x.GetByIdForReportAsync("review_123"), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnDetailsWithBuyerAndOrder_WhenTargetTypeIsBuyer()
        {
            // Arrange
            string reportId = "R2";
            var report = new Report
            {
                ReportId = reportId,
                TargetType = "buyer",
                TargetId = "order_123",
                Reason = "Scam"
            };

            var order = new Order
            {
                OrderId = "order_123",
                BuyerId = "buyer_123",
                Status = "Completed"
            };

            var buyer = new User
            {
                UserId = "buyer_123",
                FirstName = "Buyer",
                LastName = "User"
            };

            _reportRepository.Setup(x => x.GetByIdAsync(reportId)).ReturnsAsync(report);
            _orderService.Setup(x => x.GetByIdAsync("order_123")).ReturnsAsync(order);
            _userService.Setup(x => x.GetByIdAsync("buyer_123")).ReturnsAsync(buyer);

            // Act
            var result = await _service.GetByIdAsync(reportId);

            // Assert
            result.Should().NotBeNull();
            result!.ReportId.Should().Be(reportId);
            result.TargetType.Should().Be("buyer");
            result.Order.Should().NotBeNull();
            result.Order!.OrderId.Should().Be("order_123");
            result.Buyer.Should().NotBeNull();
            result.Buyer!.UserId.Should().Be("buyer_123");
            result.Buyer.UserName.Should().Be("Buyer User");

            _userService.Verify(x => x.GetByIdAsync("buyer_123"), Times.Once);
            _orderService.Verify(x => x.GetByIdAsync("order_123"), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnDetailsWithSellerAndOrder_WhenTargetTypeIsSeller()
        {
            // Arrange
            string reportId = "R3";
            var report = new Report
            {
                ReportId = reportId,
                TargetType = "seller",
                TargetId = "order_456",
                Reason = "Item not as described"
            };

            var order = new Order
            {
                OrderId = "order_456",
                SellerId = "seller_456",
                Status = "Completed"
            };

            var seller = new User
            {
                UserId = "seller_456",
                FirstName = "Seller",
                LastName = "User"
            };

            _reportRepository.Setup(x => x.GetByIdAsync(reportId)).ReturnsAsync(report);
            _orderService.Setup(x => x.GetByIdAsync("order_456")).ReturnsAsync(order);
            _userService.Setup(x => x.GetByIdAsync("seller_456")).ReturnsAsync(seller);

            // Act
            var result = await _service.GetByIdAsync(reportId);

            // Assert
            result.Should().NotBeNull();
            result!.ReportId.Should().Be(reportId);
            result.TargetType.Should().Be("seller");
            result.Order.Should().NotBeNull();
            result.Order!.OrderId.Should().Be("order_456");
            result.Seller.Should().NotBeNull();
            result.Seller!.UserId.Should().Be("seller_456");
            result.Seller.UserName.Should().Be("Seller User");

            _userService.Verify(x => x.GetByIdAsync("seller_456"), Times.Once);
            _orderService.Verify(x => x.GetByIdAsync("order_456"), Times.Once);
        }
    }
}
