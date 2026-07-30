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
    public class ReviewGetSellerReviewSummaryTests
    {
        private readonly Mock<IOrderRepository> _orderRepository;
        private readonly Mock<IReviewRepository> _reviewRepository;
        private readonly Mock<IReportRepository> _reportRepository;
        private readonly Mock<IAccountRepository> _accountRepository;
        private readonly Mock<INotificationService> _notificationService;
        private readonly ReviewService _service;

        public ReviewGetSellerReviewSummaryTests()
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

        [Fact]
        public async Task GetSellerReviewSummaryAsync_ShouldReturnEmptySummary_WhenSellerAccountNotFound()
        {
            // Arrange
            string accountId = "invalid_acc";
            _accountRepository.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync((Account?)null);

            // Act
            var result = await _service.GetSellerReviewSummaryAsync(accountId);

            // Assert
            result.Should().NotBeNull();
            result.TotalReviews.Should().Be(0);
        }

        [Fact]
        public async Task GetSellerReviewSummaryAsync_ShouldReturnAggregatedSummary_WhenSellerAccountIsValid()
        {
            // Arrange
            string sellerId = "seller_user_id";
            string accountId = "seller_acc_id";

            var account = new Account { AccountId = accountId, UserId = sellerId };
            _accountRepository.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);

            var reviewsList = new List<Review>
            {
                new Review { ReviewId = "R1", SellerId = sellerId, Rating = 5 },
                new Review { ReviewId = "R2", SellerId = sellerId, Rating = 3 }
            };

            _reviewRepository.Setup(x => x.Query()).Returns(reviewsList.AsAsyncQueryable());

            var reportsList = new List<Report>().AsAsyncQueryable();
            _reportRepository.Setup(x => x.Query()).Returns(reportsList);

            // Act
            var result = await _service.GetSellerReviewSummaryAsync(accountId);

            // Assert
            result.Should().NotBeNull();
            result.TotalReviews.Should().Be(2);
            result.AverageRating.Should().Be(4); // (5 + 3) / 2 = 4
            result.ReportedReviews.Should().Be(0);
            result.RatingStats![5].Should().Be(1);
            result.RatingStats[3].Should().Be(1);
        }
    }
}
