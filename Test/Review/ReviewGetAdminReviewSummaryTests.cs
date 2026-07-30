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
    public class ReviewGetAdminReviewSummaryTests
    {
        private readonly Mock<IOrderRepository> _orderRepository;
        private readonly Mock<IReviewRepository> _reviewRepository;
        private readonly Mock<IReportRepository> _reportRepository;
        private readonly Mock<IAccountRepository> _accountRepository;
        private readonly Mock<INotificationService> _notificationService;
        private readonly ReviewService _service;

        public ReviewGetAdminReviewSummaryTests()
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
        public async Task GetAdminReviewSummaryAsync_ShouldReturnFilteredSummary()
        {
            // Arrange
            var query = new ReviewQueryDto { Rating = 5 }; // Filter by rating 5

            var reviewsList = new List<Review>
            {
                new Review { ReviewId = "R1", Rating = 5 },
                new Review { ReviewId = "R2", Rating = 3 } // Should be filtered out
            };

            _reviewRepository.Setup(x => x.Query()).Returns(reviewsList.AsAsyncQueryable());

            var reportsList = new List<Report>().AsAsyncQueryable();
            _reportRepository.Setup(x => x.Query()).Returns(reportsList);

            // Act
            var result = await _service.GetAdminReviewSummaryAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.TotalReviews.Should().Be(1); // Only rating 5 review remains
            result.AverageRating.Should().Be(5);
            result.RatingStats![5].Should().Be(1);
            result.RatingStats[3].Should().Be(0);
        }
    }
}
