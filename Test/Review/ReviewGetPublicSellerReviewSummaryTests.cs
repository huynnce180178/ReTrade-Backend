using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using RetradeBE.Models;
using RetradeBE.Models.DTOs;
using RetradeBE.Repositories;
using RetradeBE.Services;
using Xunit;

namespace Test.ReviewTests
{
    public class ReviewGetPublicSellerReviewSummaryTests
    {
        private readonly Mock<IOrderRepository> _orderRepository;
        private readonly Mock<IReviewRepository> _reviewRepository;
        private readonly Mock<IReportRepository> _reportRepository;
        private readonly Mock<IAccountRepository> _accountRepository;
        private readonly Mock<INotificationService> _notificationService;
        private readonly ReviewService _service;

        public ReviewGetPublicSellerReviewSummaryTests()
        {
            _orderRepository = new Mock<IOrderRepository>();
            _reviewRepository = new Mock<IReviewRepository>();
            _reportRepository = new Mock<IReportRepository>();
            _accountRepository = new Mock<IAccountRepository>();
            _notificationService = new Mock<INotificationService>();

            _service = new ReviewService(
                _orderRepository.Object,
                _reviewRepository.Object,
                _reportRepository.Object,
                _accountRepository.Object,
                _notificationService.Object
            );
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task GetPublicSellerReviewSummaryAsync_ShouldReturnEmptySummary_WhenSellerIdIsNullOrWhiteSpace(string? sellerId)
        {
            // Act
            var result = await _service.GetPublicSellerReviewSummaryAsync(sellerId!);

            // Assert
            result.Should().NotBeNull();
            result.TotalReviews.Should().Be(0);
            result.AverageRating.Should().Be(0);
        }

        [Fact]
        public async Task GetPublicSellerReviewSummaryAsync_ShouldReturnAggregatedSummary_WhenSellerIdIsValid()
        {
            // Arrange
            string sellerId = "seller_user_id";
            string accountId = "seller_acc_id";

            var account = new Account { AccountId = accountId, UserId = sellerId };
            _accountRepository.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);

            var reviewsList = new List<Review>
            {
                new Review { ReviewId = "R1", SellerId = sellerId, Rating = 5 },
                new Review { ReviewId = "R2", SellerId = sellerId, Rating = 4 },
                new Review { ReviewId = "R3", SellerId = sellerId, Rating = 5 }
            };

            _reviewRepository.Setup(x => x.Query()).Returns(reviewsList.AsAsyncQueryable());

            var reportsList = new List<Report>
            {
                new Report { ReportId = "Rep1", TargetType = "Review", TargetId = "R1" }
            };

            _reportRepository.Setup(x => x.Query()).Returns(reportsList.AsAsyncQueryable());

            // Act
            var result = await _service.GetPublicSellerReviewSummaryAsync(accountId);

            // Assert
            result.Should().NotBeNull();
            result.TotalReviews.Should().Be(3);
            result.AverageRating.Should().Be(4.67); // (5 + 4 + 5) / 3 = 4.666... Rounded to 2 decimals is 4.67
            result.ReportedReviews.Should().Be(1); // R1 was reported
            result.RatingStats.Should().NotBeNull();
            result.RatingStats![5].Should().Be(2);
            result.RatingStats[4].Should().Be(1);
            result.RatingStats[3].Should().Be(0);
        }
    }
}
