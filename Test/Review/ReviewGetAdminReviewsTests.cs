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
    public class ReviewGetAdminReviewsTests
    {
        private readonly Mock<IOrderRepository> _orderRepository;
        private readonly Mock<IReviewRepository> _reviewRepository;
        private readonly Mock<IReportRepository> _reportRepository;
        private readonly Mock<IAccountRepository> _accountRepository;
        private readonly ReviewService _service;

        public ReviewGetAdminReviewsTests()
        {
            _orderRepository = new Mock<IOrderRepository>();
            _reviewRepository = new Mock<IReviewRepository>();
            _reportRepository = new Mock<IReportRepository>();
            _accountRepository = new Mock<IAccountRepository>();

            _service = new ReviewService(
                _orderRepository.Object,
                _reviewRepository.Object,
                _reportRepository.Object,
                _accountRepository.Object
            );
        }

        [Fact]
        public async Task GetAdminReviewsAsync_ShouldReturnAllReviewsWithReportsAndPrivateInfo()
        {
            // Arrange
            var query = new ReviewQueryDto { Page = 1, PageSize = 10 };

            var review = new Review
            {
                ReviewId = "review_123",
                Rating = 2,
                Comment = "Bad seller",
                CreatedAt = DateTime.UtcNow,
                Reviewer = new User { UserId = "buyer_123", FirstName = "Jane", LastName = "Smith", Email = "jane@example.com", AvatarUrl = "avatar.jpg" }
            };

            var reviews = new List<Review> { review }.AsAsyncQueryable();
            _reviewRepository.Setup(x => x.Query()).Returns(reviews);

            var report = new Report
            {
                ReportId = "rep_123",
                TargetId = "review_123",
                TargetType = "Review",
                ReporterId = "reporter_123",
                Reason = "Harassment",
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            var reports = new List<Report> { report }.AsAsyncQueryable();
            _reportRepository.Setup(x => x.Query()).Returns(reports);

            // Act
            var result = await _service.GetAdminReviewsAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().HaveCount(1);
            result.TotalItems.Should().Be(1);

            var resultItem = result.Items.First();
            resultItem.ReviewId.Should().Be("review_123");
            resultItem.ReviewerEmail.Should().Be("jane@example.com"); // Admin views INCLUDE email
            resultItem.ReviewerAvatarUrl.Should().Be("avatar.jpg"); // Admin views INCLUDE avatar
            resultItem.ReportCount.Should().Be(1);
            resultItem.Reports.Should().HaveCount(1);
            resultItem.Reports.First().ReportId.Should().Be("rep_123");
        }
    }
}
