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
    public class ReviewGetSellerReviewsTests
    {
        private readonly Mock<IOrderRepository> _orderRepository;
        private readonly Mock<IReviewRepository> _reviewRepository;
        private readonly Mock<IReportRepository> _reportRepository;
        private readonly Mock<IAccountRepository> _accountRepository;
        private readonly Mock<INotificationService> _notificationService;
        private readonly ReviewService _service;

        public ReviewGetSellerReviewsTests()
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
        public async Task GetSellerReviewsAsync_ShouldReturnEmptyPagedResult_WhenSellerAccountNotFound()
        {
            // Arrange
            string accountId = "invalid_acc";
            var query = new ReviewQueryDto { Page = 1, PageSize = 10 };
            _accountRepository.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync((Account?)null);

            // Act
            var result = await _service.GetSellerReviewsAsync(accountId, query);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().BeEmpty();
            result.TotalItems.Should().Be(0);
        }

        [Fact]
        public async Task GetSellerReviewsAsync_ShouldReturnPrivateReviews_WhenSellerAccountIsValid()
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
                Rating = 5,
                Comment = "Highly recommended!",
                CreatedAt = DateTime.UtcNow,
                Reviewer = new User { UserId = "buyer_123", FirstName = "Jane", LastName = "Smith", Email = "jane@example.com", AvatarUrl = "avatar.jpg" }
            };

            var reviews = new List<Review> { review }.AsAsyncQueryable();
            _reviewRepository.Setup(x => x.Query()).Returns(reviews);

            var reports = new List<Report>().AsAsyncQueryable();
            _reportRepository.Setup(x => x.Query()).Returns(reports);

            // Act
            var result = await _service.GetSellerReviewsAsync(accountId, query);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().HaveCount(1);
            result.TotalItems.Should().Be(1);

            var resultItem = result.Items.First();
            resultItem.ReviewId.Should().Be("review_123");
            resultItem.ReviewerName.Should().Be("Jane Smith");
            resultItem.ReviewerEmail.Should().Be("jane@example.com"); // Private reviews INCLUDE email
            resultItem.ReviewerAvatarUrl.Should().Be("avatar.jpg"); // Private reviews INCLUDE avatar
            resultItem.Comment.Should().Be("Highly recommended!");
        }
    }
}
