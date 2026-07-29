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
    public class ReportUpdateStatusTests
    {
        private readonly Mock<IReportRepository> _reportRepository;
        private readonly Mock<IOrderService> _orderService;
        private readonly Mock<IAccountService> _accountService;
        private readonly Mock<IUserService> _userService;
        private readonly Mock<IProductService> _productService;
        private readonly Mock<IReviewService> _reviewService;
        private readonly IMapper _mapper;
        private readonly ReportService _service;

        public ReportUpdateStatusTests()
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
        public async Task UpdateStatusAsync_ShouldThrowInvalidOperationException_WhenRequestOrStatusIsMissing()
        {
            // Act & Assert
            Func<Task> actNullRequest = async () => await _service.UpdateStatusAsync("R1", null!);
            await actNullRequest.Should().ThrowAsync<InvalidOperationException>().WithMessage("Status is required.");

            Func<Task> actEmptyStatus = async () => await _service.UpdateStatusAsync("R1", new ReportStatusUpdateDto { Status = "" });
            await actEmptyStatus.Should().ThrowAsync<InvalidOperationException>().WithMessage("Status is required.");
        }

        [Fact]
        public async Task UpdateStatusAsync_ShouldReturnNull_WhenReportNotFound()
        {
            // Arrange
            _reportRepository.Setup(x => x.GetByIdAsync("R1")).ReturnsAsync((Report?)null);

            // Act
            var result = await _service.UpdateStatusAsync("R1", new ReportStatusUpdateDto { Status = "Reject" });

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task UpdateStatusAsync_ShouldSetRejected_WhenStatusIsReject()
        {
            // Arrange
            var report = new Report { ReportId = "R1", Status = "Pending" };
            _reportRepository.Setup(x => x.GetByIdAsync("R1")).ReturnsAsync(report);
            _reportRepository.Setup(x => x.UpdateAsync(It.IsAny<Report>())).Returns(Task.CompletedTask);

            // Act
            var result = await _service.UpdateStatusAsync("R1", new ReportStatusUpdateDto { Status = "Reject" });

            // Assert
            result.Should().NotBeNull();
            result!.Status.Should().Be("Rejected");
            _reportRepository.Verify(x => x.UpdateAsync(It.Is<Report>(r => r.Status == "Rejected")), Times.Once);
        }

        [Fact]
        public async Task UpdateStatusAsync_ShouldAcceptAndHideReview_WhenStatusIsAcceptReview()
        {
            // Arrange
            var report = new Report { ReportId = "R1", TargetId = "review_123", TargetType = "review", Status = "Pending" };
            _reportRepository.Setup(x => x.GetByIdAsync("R1")).ReturnsAsync(report);
            _reviewService.Setup(x => x.HideForReportAsync("review_123", It.IsAny<DateTime>())).Returns(Task.CompletedTask);
            _reportRepository.Setup(x => x.UpdateAsync(It.IsAny<Report>())).Returns(Task.CompletedTask);

            // Act
            var result = await _service.UpdateStatusAsync("R1", new ReportStatusUpdateDto { Status = "Accept Review" });

            // Assert
            result.Should().NotBeNull();
            result!.Status.Should().Be("Accepted");
            _reviewService.Verify(x => x.HideForReportAsync("review_123", It.IsAny<DateTime>()), Times.Once);
            _reportRepository.Verify(x => x.UpdateAsync(It.Is<Report>(r => r.Status == "Accepted")), Times.Once);
        }

        [Fact]
        public async Task UpdateStatusAsync_ShouldAcceptAndIncrementFlagCount_WhenStatusIsAcceptBuyer_AndFlagCountIsUnderLimit()
        {
            // Arrange
            var report = new Report { ReportId = "R1", TargetId = "order_123", TargetType = "buyer", Status = "Pending" };
            var order = new Order { OrderId = "order_123", BuyerId = "buyer_123" };
            var buyer = new User { UserId = "buyer_123", FlagCount = 0 };

            _reportRepository.Setup(x => x.GetByIdAsync("R1")).ReturnsAsync(report);
            _orderService.Setup(x => x.GetByIdAsync("order_123")).ReturnsAsync(order);
            _userService.Setup(x => x.GetByIdAsync("buyer_123")).ReturnsAsync(buyer);
            _userService.Setup(x => x.UpdateAsync(buyer)).Returns(Task.CompletedTask);
            _reportRepository.Setup(x => x.UpdateAsync(It.IsAny<Report>())).Returns(Task.CompletedTask);

            // Act
            var result = await _service.UpdateStatusAsync("R1", new ReportStatusUpdateDto { Status = "Accept Buyer" });

            // Assert
            result.Should().NotBeNull();
            result!.Status.Should().Be("Accepted");
            buyer.FlagCount.Should().Be(1);
            buyer.IsDeleted.Should().BeNull(); // Or false, wasn't set to true because flag count is 1 < 2

            _userService.Verify(x => x.UpdateAsync(It.Is<User>(u => u.FlagCount == 1)), Times.Once);
            _accountService.Verify(x => x.BanUserAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task UpdateStatusAsync_ShouldBanAndHideProducts_WhenStatusIsAcceptBuyer_AndFlagCountReachesLimit()
        {
            // Arrange
            var report = new Report { ReportId = "R1", TargetId = "order_123", TargetType = "buyer", Status = "Pending" };
            var order = new Order { OrderId = "order_123", BuyerId = "buyer_123" };
            var buyer = new User { UserId = "buyer_123", FlagCount = 1 }; // Will become 2
            var account = new Account { AccountId = "acc_123", UserId = "buyer_123", Status = "Active" };

            _reportRepository.Setup(x => x.GetByIdAsync("R1")).ReturnsAsync(report);
            _orderService.Setup(x => x.GetByIdAsync("order_123")).ReturnsAsync(order);
            _userService.Setup(x => x.GetByIdAsync("buyer_123")).ReturnsAsync(buyer);
            _userService.Setup(x => x.UpdateAsync(buyer)).Returns(Task.CompletedTask);
            _accountService.Setup(x => x.GetByUserIdAsync("buyer_123")).ReturnsAsync(account);
            _accountService.Setup(x => x.BanUserAsync("acc_123")).ReturnsAsync(true);
            _productService.Setup(x => x.HideProductsBySellerAsync("buyer_123", It.IsAny<DateTime>())).Returns(Task.CompletedTask);
            _reportRepository.Setup(x => x.UpdateAsync(It.IsAny<Report>())).Returns(Task.CompletedTask);

            // Act
            var result = await _service.UpdateStatusAsync("R1", new ReportStatusUpdateDto { Status = "Accept Buyer" });

            // Assert
            result.Should().NotBeNull();
            result!.Status.Should().Be("Accepted");
            buyer.FlagCount.Should().Be(2);
            buyer.IsDeleted.Should().BeTrue();

            _userService.Verify(x => x.UpdateAsync(It.Is<User>(u => u.FlagCount == 2 && u.IsDeleted == true)), Times.Exactly(2)); // Updates first for FlagCount, then for IsDeleted
            _accountService.Verify(x => x.BanUserAsync("acc_123"), Times.Once);
            _productService.Verify(x => x.HideProductsBySellerAsync("buyer_123", It.IsAny<DateTime>()), Times.Once);
        }

        [Fact]
        public async Task UpdateStatusAsync_ShouldBanAndHideProducts_WhenStatusIsAcceptSeller()
        {
            // Arrange
            var report = new Report { ReportId = "R1", TargetId = "order_123", TargetType = "seller", Status = "Pending" };
            var order = new Order { OrderId = "order_123", SellerId = "seller_123" };
            var seller = new User { UserId = "seller_123", FlagCount = 0 };
            var account = new Account { AccountId = "acc_456", UserId = "seller_123", Status = "Active" };

            _reportRepository.Setup(x => x.GetByIdAsync("R1")).ReturnsAsync(report);
            _orderService.Setup(x => x.GetByIdAsync("order_123")).ReturnsAsync(order);
            _userService.Setup(x => x.GetByIdAsync("seller_123")).ReturnsAsync(seller);
            _userService.Setup(x => x.UpdateAsync(seller)).Returns(Task.CompletedTask);
            _accountService.Setup(x => x.GetByUserIdAsync("seller_123")).ReturnsAsync(account);
            _accountService.Setup(x => x.BanUserAsync("acc_456")).ReturnsAsync(true);
            _productService.Setup(x => x.HideProductsBySellerAsync("seller_123", It.IsAny<DateTime>())).Returns(Task.CompletedTask);
            _reportRepository.Setup(x => x.UpdateAsync(It.IsAny<Report>())).Returns(Task.CompletedTask);

            // Act
            var result = await _service.UpdateStatusAsync("R1", new ReportStatusUpdateDto { Status = "Accept Seller" });

            // Assert
            result.Should().NotBeNull();
            result!.Status.Should().Be("Accepted");
            seller.IsDeleted.Should().BeTrue();

            _userService.Verify(x => x.UpdateAsync(It.Is<User>(u => u.IsDeleted == true)), Times.Once);
            _accountService.Verify(x => x.BanUserAsync("acc_456"), Times.Once);
            _productService.Verify(x => x.HideProductsBySellerAsync("seller_123", It.IsAny<DateTime>()), Times.Once);
        }

        [Fact]
        public async Task UpdateStatusAsync_ShouldThrowInvalidOperationException_WhenStatusIsUnsupported()
        {
            // Arrange
            var report = new Report { ReportId = "R1", Status = "Pending" };
            _reportRepository.Setup(x => x.GetByIdAsync("R1")).ReturnsAsync(report);

            // Act
            Func<Task> act = async () => await _service.UpdateStatusAsync("R1", new ReportStatusUpdateDto { Status = "InvalidStatus" });

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Unsupported report status.");
        }
    }
}
