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
    public class ReviewGetByBuyerOrderTests
    {
        private readonly Mock<IOrderRepository> _orderRepository;
        private readonly Mock<IReviewRepository> _reviewRepository;
        private readonly Mock<IReportRepository> _reportRepository;
        private readonly Mock<IAccountRepository> _accountRepository;
        private readonly Mock<INotificationService> _notificationService;
        private readonly ReviewService _service;

        public ReviewGetByBuyerOrderTests()
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
        [InlineData(null, "order_123")]
        [InlineData("buyer_123", null)]
        [InlineData("", "order_123")]
        [InlineData("buyer_123", "")]
        [InlineData("   ", "order_123")]
        public async Task GetByBuyerOrderAsync_ShouldReturnNull_WhenParametersAreMissing(string? buyerId, string? orderId)
        {
            // Act
            var result = await _service.GetByBuyerOrderAsync("acc_123", buyerId!, orderId!, false);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByBuyerOrderAsync_ShouldThrowUnauthorizedAccessException_WhenAccountNotFound()
        {
            // Arrange
            string accountId = "invalid_acc";
            _accountRepository.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync((Account?)null);

            // Act
            Func<Task> act = async () => await _service.GetByBuyerOrderAsync(accountId, "buyer_123", "order_123", false);

            // Assert
            await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("Account not found.");
        }

        [Fact]
        public async Task GetByBuyerOrderAsync_ShouldThrowUnauthorizedAccessException_WhenNonAdminRequestsOtherUsersReview()
        {
            // Arrange
            string accountId = "acc_123";
            string buyerId = "buyer_123";
            string requesterUserId = "other_user_id";
            var account = new Account { AccountId = accountId, UserId = requesterUserId };

            _accountRepository.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);

            // Act
            Func<Task> act = async () => await _service.GetByBuyerOrderAsync(accountId, buyerId, "order_123", false);

            // Assert
            await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("You can only view reviews for your own orders.");
        }

        [Fact]
        public async Task GetByBuyerOrderAsync_ShouldReturnNull_WhenReviewNotFound()
        {
            // Arrange
            string accountId = "acc_123";
            string buyerId = "buyer_123";
            string orderId = "order_123";
            var account = new Account { AccountId = accountId, UserId = buyerId };

            _accountRepository.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);

            var reviews = new List<Review>().AsAsyncQueryable();
            _reviewRepository.Setup(x => x.Query()).Returns(reviews);

            // Act
            var result = await _service.GetByBuyerOrderAsync(accountId, buyerId, orderId, false);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByBuyerOrderAsync_ShouldReturnMappedReview_WhenReviewExists()
        {
            // Arrange
            string accountId = "acc_123";
            string buyerId = "buyer_123";
            string orderId = "order_123";
            var account = new Account { AccountId = accountId, UserId = buyerId };

            _accountRepository.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);

            var review = new Review
            {
                ReviewId = "review_123",
                ReviewerId = buyerId,
                OrderId = orderId,
                Rating = 5,
                Comment = "Perfect!",
                Reviewer = new User { UserId = buyerId, FirstName = "John", LastName = "Doe", Email = "buyer@example.com" }
            };

            var reviews = new List<Review> { review }.AsAsyncQueryable();
            _reviewRepository.Setup(x => x.Query()).Returns(reviews);

            // Act
            var result = await _service.GetByBuyerOrderAsync(accountId, buyerId, orderId, false);

            // Assert
            result.Should().NotBeNull();
            result!.ReviewId.Should().Be("review_123");
            result.ReviewerName.Should().Be("John Doe");
            result.ReviewerEmail.Should().Be("buyer@example.com");
            result.Rating.Should().Be(5);
            result.Comment.Should().Be("Perfect!");
        }
    }
}
