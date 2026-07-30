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
    public class ReviewGetPublicSellerReviewsTests
    {
        private readonly Mock<IOrderRepository> _orderRepository;
        private readonly Mock<IReviewRepository> _reviewRepository;
        private readonly Mock<IReportRepository> _reportRepository;
        private readonly Mock<IAccountRepository> _accountRepository;
        private readonly Mock<INotificationService> _notificationService;
        private readonly ReviewService _service;

        public ReviewGetPublicSellerReviewsTests()
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
        public async Task GetPublicSellerReviewsAsync_ShouldReturnEmptyPagedResult_WhenSellerIdIsNullOrWhiteSpace(string? sellerId)
        {
            // Arrange
            var query = new ReviewQueryDto { Page = 1, PageSize = 10 };

            // Act
            var result = await _service.GetPublicSellerReviewsAsync(sellerId!, query);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().BeEmpty();
            result.TotalItems.Should().Be(0);
        }

        [Fact]
        public async Task GetPublicSellerReviewsAsync_ShouldReturnMappedPublicReviews_WhenSellerIdIsValid()
        {
            // Arrange
            string sellerId = "seller_user_id";
            string accountId = "seller_acc_id";
            var query = new ReviewQueryDto { Page = 1, PageSize = 10 };

            var account = new Account { AccountId = accountId, UserId = sellerId };
            _accountRepository.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);

            var review = new Review
            {
                ReviewId = "review_123",
                SellerId = sellerId,
                Rating = 4,
                Comment = "Very good store!",
                CreatedAt = DateTime.UtcNow,
                Reviewer = new User { UserId = "buyer_123", FirstName = "Jane", LastName = "Smith", Email = "jane@example.com" }
            };

            var reviews = new List<Review> { review }.AsAsyncQueryable();
            _reviewRepository.Setup(x => x.Query()).Returns(reviews);

            var reports = new List<Report>().AsAsyncQueryable();
            _reportRepository.Setup(x => x.Query()).Returns(reports);

            // Act
            var result = await _service.GetPublicSellerReviewsAsync(accountId, query);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().HaveCount(1);
            result.TotalItems.Should().Be(1);

            var resultItem = result.Items.First();
            resultItem.ReviewId.Should().Be("review_123");
            resultItem.ReviewerName.Should().Be("Jane Smith");
            resultItem.ReviewerEmail.Should().BeNull(); // Public reviews do NOT include reviewer private email
            resultItem.ReviewerAvatarUrl.Should().BeNull(); // Public reviews do NOT include reviewer avatar
            resultItem.Comment.Should().Be("Very good store!");
        }
    }
}
