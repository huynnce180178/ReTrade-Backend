using System;
using System.Collections.Generic;
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

namespace Test.ReviewTests
{
    public class ReviewReportReviewTests
    {
        private readonly Mock<IOrderRepository> _orderRepository;
        private readonly Mock<IReviewRepository> _reviewRepository;
        private readonly Mock<IReportRepository> _reportRepository;
        private readonly Mock<IAccountRepository> _accountRepository;
        private readonly Mock<INotificationService> _notificationService;
        private readonly IMapper _mapper;
        private readonly ReviewService _service;

        public ReviewReportReviewTests()
        {
            _orderRepository = new Mock<IOrderRepository>();
            _reviewRepository = new Mock<IReviewRepository>();
            _reportRepository = new Mock<IReportRepository>();
            _accountRepository = new Mock<IAccountRepository>();
            _notificationService = new Mock<INotificationService>();

            // Setup AutoMapper with NullLoggerFactory to comply with prompt dựng test code.md
            var configuration = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<AutoMapperProfile>();
            }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
            _mapper = configuration.CreateMapper();

            _service = new ReviewService(
                _orderRepository.Object,
                _reviewRepository.Object,
                _reportRepository.Object,
                _accountRepository.Object,
                _notificationService.Object
            );
        }

        #region Normal Tests (N)
        [Fact]
        public async Task ReportReviewAsync_ShouldSaveReportAndReturnMappedDto_WhenReporterIsSellerOfReview()
        {
            // Arrange
            string accountId = "acc_123";
            string userId = "user_123";
            var account = new Account { AccountId = accountId, UserId = userId };
            _accountRepository.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);

            var review = new Review { ReviewId = "review_123", SellerId = userId };
            _reviewRepository.Setup(x => x.GetByIdForReportAsync("review_123")).ReturnsAsync(review);

            _reviewRepository.Setup(x => x.GetReportByReporterAsync("review_123", userId)).ReturnsAsync((Report?)null);
            _reportRepository.Setup(x => x.AddAsync(It.IsAny<Report>())).Returns(Task.CompletedTask);

            var request = new ReportCreateDto { Reason = "Abusive comment", Description = "Very rude behavior" };

            // Act
            var result = await _service.ReportReviewAsync(accountId, "review_123", request, false);

            // Assert
            result.Should().NotBeNull();
            result.Reason.Should().Be("Abusive comment");
            result.Description.Should().Be("Very rude behavior");
            result.TargetId.Should().Be("review_123");
            result.TargetType.Should().Be("Review");
            result.ReporterId.Should().Be(userId);

            _reportRepository.Verify(x => x.AddAsync(It.Is<Report>(r =>
                r.TargetId == "review_123" &&
                r.TargetType == "Review" &&
                r.ReporterId == userId &&
                r.Reason == "Abusive comment" &&
                r.Description == "Very rude behavior"
            )), Times.Once);
        }

        [Fact]
        public async Task ReportReviewAsync_ShouldSaveReportAndReturnMappedDto_WhenReporterIsSellerOfOrderButReviewSellerIdIsNull()
        {
            // Arrange
            string accountId = "acc_123";
            string userId = "user_123";
            var account = new Account { AccountId = accountId, UserId = userId };
            _accountRepository.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);

            var review = new Review 
            { 
                ReviewId = "review_123", 
                SellerId = null, 
                Order = new Order { SellerId = userId } 
            };
            _reviewRepository.Setup(x => x.GetByIdForReportAsync("review_123")).ReturnsAsync(review);

            _reviewRepository.Setup(x => x.GetReportByReporterAsync("review_123", userId)).ReturnsAsync((Report?)null);
            _reportRepository.Setup(x => x.AddAsync(It.IsAny<Report>())).Returns(Task.CompletedTask);

            var request = new ReportCreateDto { Reason = "Fake review" };

            // Act
            var result = await _service.ReportReviewAsync(accountId, "review_123", request, false);

            // Assert
            result.Should().NotBeNull();
            result.ReporterId.Should().Be(userId);
            _reportRepository.Verify(x => x.AddAsync(It.IsAny<Report>()), Times.Once);
        }

        [Fact]
        public async Task ReportReviewAsync_ShouldSaveReportAndReturnMappedDto_WhenReporterIsNotSellerButIsAdmin()
        {
            // Arrange
            string accountId = "admin_acc";
            string userId = "admin_user";
            var account = new Account { AccountId = accountId, UserId = userId };
            _accountRepository.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);

            var review = new Review { ReviewId = "review_123", SellerId = "some_other_seller" };
            _reviewRepository.Setup(x => x.GetByIdForReportAsync("review_123")).ReturnsAsync(review);

            _reviewRepository.Setup(x => x.GetReportByReporterAsync("review_123", userId)).ReturnsAsync((Report?)null);
            _reportRepository.Setup(x => x.AddAsync(It.IsAny<Report>())).Returns(Task.CompletedTask);

            var request = new ReportCreateDto { Reason = "Inappropriate text" };

            // Act
            var result = await _service.ReportReviewAsync(accountId, "review_123", request, true);

            // Assert
            result.Should().NotBeNull();
            result.ReporterId.Should().Be(userId);
            _reportRepository.Verify(x => x.AddAsync(It.IsAny<Report>()), Times.Once);
        }
        #endregion

        #region Abnormal Tests (A)
        [Fact]
        public async Task ReportReviewAsync_ShouldThrowUnauthorizedAccessException_WhenAccountNotFound()
        {
            // Arrange
            string accountId = "invalid_acc";
            _accountRepository.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync((Account?)null);

            // Act
            Func<Task> act = async () => await _service.ReportReviewAsync(accountId, "review_123", new ReportCreateDto(), false);

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
            string accountId = "acc_123";
            var account = new Account { AccountId = accountId, UserId = "user_123" };
            _accountRepository.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);

            // Act
            Func<Task> act = async () => await _service.ReportReviewAsync(accountId, reviewId!, new ReportCreateDto(), false);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("ReviewId is required.");
        }

        [Fact]
        public async Task ReportReviewAsync_ShouldThrowInvalidOperationException_WhenRequestOrReasonIsMissing()
        {
            // Arrange
            string accountId = "acc_123";
            string userId = "user_123";
            var account = new Account { AccountId = accountId, UserId = userId };
            _accountRepository.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);

            // Act & Assert
            Func<Task> actNullRequest = async () => await _service.ReportReviewAsync(accountId, "review_123", null!, false);
            await actNullRequest.Should().ThrowAsync<InvalidOperationException>().WithMessage("Report reason is required.");

            Func<Task> actEmptyReason = async () => await _service.ReportReviewAsync(accountId, "review_123", new ReportCreateDto { Reason = "" }, false);
            await actEmptyReason.Should().ThrowAsync<InvalidOperationException>().WithMessage("Report reason is required.");
        }

        [Fact]
        public async Task ReportReviewAsync_ShouldThrowKeyNotFoundException_WhenReviewNotFound()
        {
            // Arrange
            string accountId = "acc_123";
            string userId = "user_123";
            var account = new Account { AccountId = accountId, UserId = userId };
            _accountRepository.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);

            _reviewRepository.Setup(x => x.GetByIdForReportAsync("review_123")).ReturnsAsync((Review?)null);

            // Act
            Func<Task> act = async () => await _service.ReportReviewAsync(accountId, "review_123", new ReportCreateDto { Reason = "Spam" }, false);

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("Review not found.");
        }

        [Fact]
        public async Task ReportReviewAsync_ShouldThrowUnauthorizedAccessException_WhenNonAdminTriesToReportReviewOfOtherStore()
        {
            // Arrange
            string accountId = "acc_123";
            string userId = "user_123";
            var account = new Account { AccountId = accountId, UserId = userId };
            _accountRepository.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);

            var review = new Review { ReviewId = "review_123", SellerId = "other_seller_id" };
            _reviewRepository.Setup(x => x.GetByIdForReportAsync("review_123")).ReturnsAsync(review);

            // Act
            Func<Task> act = async () => await _service.ReportReviewAsync(accountId, "review_123", new ReportCreateDto { Reason = "Spam" }, false);

            // Assert
            await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("You can only report reviews for your own store.");
        }

        [Fact]
        public async Task ReportReviewAsync_ShouldThrowInvalidOperationException_WhenAlreadyReportedByReporter()
        {
            // Arrange
            string accountId = "acc_123";
            string userId = "user_123";
            var account = new Account { AccountId = accountId, UserId = userId };
            _accountRepository.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);

            var review = new Review { ReviewId = "review_123", SellerId = userId };
            _reviewRepository.Setup(x => x.GetByIdForReportAsync("review_123")).ReturnsAsync(review);

            _reviewRepository.Setup(x => x.GetReportByReporterAsync("review_123", userId)).ReturnsAsync(new Report());

            // Act
            Func<Task> act = async () => await _service.ReportReviewAsync(accountId, "review_123", new ReportCreateDto { Reason = "Spam" }, false);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("You have already reported this review.");
        }
        #endregion

        #region Boundary Tests (B)
        [Fact]
        public async Task ReportReviewAsync_ShouldTrimReasonAndDescription_WhenTheyHaveLeadingOrTrailingWhitespaces()
        {
            // Arrange
            string accountId = "acc_123";
            string userId = "user_123";
            var account = new Account { AccountId = accountId, UserId = userId };
            _accountRepository.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);

            var review = new Review { ReviewId = "review_123", SellerId = userId };
            _reviewRepository.Setup(x => x.GetByIdForReportAsync("review_123")).ReturnsAsync(review);

            _reviewRepository.Setup(x => x.GetReportByReporterAsync("review_123", userId)).ReturnsAsync((Report?)null);
            _reportRepository.Setup(x => x.AddAsync(It.IsAny<Report>())).Returns(Task.CompletedTask);

            var request = new ReportCreateDto { Reason = "   Spam   ", Description = "   Abusive language   " };

            // Act
            var result = await _service.ReportReviewAsync(accountId, "review_123", request, false);

            // Assert
            result.Should().NotBeNull();
            result.Reason.Should().Be("Spam");
            result.Description.Should().Be("Abusive language");

            _reportRepository.Verify(x => x.AddAsync(It.Is<Report>(r =>
                r.Reason == "Spam" &&
                r.Description == "Abusive language"
            )), Times.Once);
        }

        [Fact]
        public async Task ReportReviewAsync_ShouldSaveReportWithNullDescription_WhenDescriptionIsNull()
        {
            // Arrange
            string accountId = "acc_123";
            string userId = "user_123";
            var account = new Account { AccountId = accountId, UserId = userId };
            _accountRepository.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);

            var review = new Review { ReviewId = "review_123", SellerId = userId };
            _reviewRepository.Setup(x => x.GetByIdForReportAsync("review_123")).ReturnsAsync(review);

            _reviewRepository.Setup(x => x.GetReportByReporterAsync("review_123", userId)).ReturnsAsync((Report?)null);
            _reportRepository.Setup(x => x.AddAsync(It.IsAny<Report>())).Returns(Task.CompletedTask);

            var request = new ReportCreateDto { Reason = "Spam", Description = null };

            // Act
            var result = await _service.ReportReviewAsync(accountId, "review_123", request, false);

            // Assert
            result.Should().NotBeNull();
            result.Description.Should().BeNull();

            _reportRepository.Verify(x => x.AddAsync(It.Is<Report>(r =>
                r.Reason == "Spam" &&
                r.Description == null
            )), Times.Once);
        }

        [Fact]
        public async Task ReportReviewAsync_ShouldProceedSuccessfully_WhenIsAdminIsTrueAndReviewSellerIdAndOrderAreBothNull()
        {
            // Arrange
            string accountId = "admin_acc";
            string userId = "admin_user";
            var account = new Account { AccountId = accountId, UserId = userId };
            _accountRepository.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);

            var review = new Review { ReviewId = "review_123", SellerId = null, Order = null };
            _reviewRepository.Setup(x => x.GetByIdForReportAsync("review_123")).ReturnsAsync(review);

            _reviewRepository.Setup(x => x.GetReportByReporterAsync("review_123", userId)).ReturnsAsync((Report?)null);
            _reportRepository.Setup(x => x.AddAsync(It.IsAny<Report>())).Returns(Task.CompletedTask);

            var request = new ReportCreateDto { Reason = "Uncategorized report" };

            // Act
            var result = await _service.ReportReviewAsync(accountId, "review_123", request, true);

            // Assert
            result.Should().NotBeNull();
            _reportRepository.Verify(x => x.AddAsync(It.IsAny<Report>()), Times.Once);
        }
        #endregion
    }
}

