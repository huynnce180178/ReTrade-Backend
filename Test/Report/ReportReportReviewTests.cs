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
    public class ReportReportReviewTests
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

        public ReportReportReviewTests()
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
        public async Task ReportReviewAsync_ShouldThrowUnauthorizedAccessException_WhenAccountNotFound()
        {
            // Arrange
            string accountId = "invalid_acc";
            _accountService.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync((Account?)null);

            // Act
            Func<Task> act = async () => await _service.ReportReviewAsync(accountId, "review_id", new ReportCreateDto());

            // Assert
            await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("Account not found.");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task ReportReviewAsync_ShouldThrowInvalidOperationException_WhenReviewIdIsNullOrWhiteSpace(string? reviewId)
        {
            // Arrange
            string accountId = "acc_id";
            var account = new Account { AccountId = accountId, UserId = "user_id" };
            _accountService.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);

            // Act
            Func<Task> act = async () => await _service.ReportReviewAsync(accountId, reviewId!, new ReportCreateDto());

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("ReviewId is required.");
        }

        [Fact]
        public async Task ReportReviewAsync_ShouldThrowInvalidOperationException_WhenRequestOrReasonIsMissing()
        {
            // Arrange
            string accountId = "acc_id";
            var account = new Account { AccountId = accountId, UserId = "user_id" };
            _accountService.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);

            // Act & Assert
            Func<Task> actWithNullRequest = async () => await _service.ReportReviewAsync(accountId, "review_id", null!);
            await actWithNullRequest.Should().ThrowAsync<InvalidOperationException>().WithMessage("Report reason is required.");

            Func<Task> actWithEmptyReason = async () => await _service.ReportReviewAsync(accountId, "review_id", new ReportCreateDto { Reason = "" });
            await actWithEmptyReason.Should().ThrowAsync<InvalidOperationException>().WithMessage("Report reason is required.");
        }

        [Fact]
        public async Task ReportReviewAsync_ShouldThrowKeyNotFoundException_WhenReviewNotFound()
        {
            // Arrange
            string accountId = "acc_id";
            string reviewId = "review_id";
            var account = new Account { AccountId = accountId, UserId = "user_id" };
            _accountService.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);
            _reviewService.Setup(x => x.GetByIdForReportAsync(reviewId)).ReturnsAsync((Review?)null);

            // Act
            Func<Task> act = async () => await _service.ReportReviewAsync(accountId, reviewId, new ReportCreateDto { Reason = "Spam" });

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("Review not found.");
        }

        [Fact]
        public async Task ReportReviewAsync_ShouldThrowInvalidOperationException_WhenReviewAlreadyDeleted()
        {
            // Arrange
            string accountId = "acc_id";
            string reviewId = "review_id";
            var account = new Account { AccountId = accountId, UserId = "user_id" };
            var review = new Review { ReviewId = reviewId, IsDeleted = true };

            _accountService.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);
            _reviewService.Setup(x => x.GetByIdForReportAsync(reviewId)).ReturnsAsync(review);

            // Act
            Func<Task> act = async () => await _service.ReportReviewAsync(accountId, reviewId, new ReportCreateDto { Reason = "Spam" });

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("The review has already been hidden.");
        }

        [Fact]
        public async Task ReportReviewAsync_ShouldThrowInvalidOperationException_WhenAlreadyReported()
        {
            // Arrange
            string accountId = "acc_id";
            string reviewId = "review_id";
            string reporterId = "user_id";
            var account = new Account { AccountId = accountId, UserId = reporterId };
            var review = new Review { ReviewId = reviewId, IsDeleted = false };

            _accountService.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);
            _reviewService.Setup(x => x.GetByIdForReportAsync(reviewId)).ReturnsAsync(review);
            _reportRepository.Setup(x => x.ExistsAsync(reviewId, reporterId, "review")).ReturnsAsync(true);

            // Act
            Func<Task> act = async () => await _service.ReportReviewAsync(accountId, reviewId, new ReportCreateDto { Reason = "Spam" });

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("You have already reported this review.");
        }

        [Fact]
        public async Task ReportReviewAsync_ShouldCreateReport_WhenParametersAreValid()
        {
            // Arrange
            string accountId = "acc_id";
            string reviewId = "review_id";
            string reporterId = "user_id";
            var account = new Account { AccountId = accountId, UserId = reporterId };
            var review = new Review { ReviewId = reviewId, IsDeleted = false };
            var request = new ReportCreateDto { Reason = "Spam", Description = "Irrelevant comment" };

            _accountService.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);
            _reviewService.Setup(x => x.GetByIdForReportAsync(reviewId)).ReturnsAsync(review);
            _reportRepository.Setup(x => x.ExistsAsync(reviewId, reporterId, "review")).ReturnsAsync(false);
            _reportRepository.Setup(x => x.AddAsync(It.IsAny<Report>())).Returns(Task.CompletedTask);

            // Act
            var result = await _service.ReportReviewAsync(accountId, reviewId, request);

            // Assert
            result.Should().NotBeNull();
            result.Reason.Should().Be("Spam");
            result.Description.Should().Be("Irrelevant comment");
            result.TargetId.Should().Be(reviewId);
            result.ReporterId.Should().Be(reporterId);
            result.TargetType.Should().Be("review");
            result.Status.Should().Be("Pending");

            _reportRepository.Verify(x => x.AddAsync(It.Is<Report>(r =>
                r.ReporterId == reporterId &&
                r.TargetId == reviewId &&
                r.TargetType == "review" &&
                r.Reason == "Spam" &&
                r.Description == "Irrelevant comment"
            )), Times.Once);
        }
    }
}
