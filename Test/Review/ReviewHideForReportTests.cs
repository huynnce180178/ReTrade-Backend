using System;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using RetradeBE.Models;
using RetradeBE.Repositories;
using RetradeBE.Services;
using Xunit;

namespace Test.ReviewTests
{
    public class ReviewHideForReportTests
    {
        private readonly Mock<IOrderRepository> _orderRepository;
        private readonly Mock<IReviewRepository> _reviewRepository;
        private readonly Mock<IReportRepository> _reportRepository;
        private readonly Mock<IAccountRepository> _accountRepository;
        private readonly ReviewService _service;

        public ReviewHideForReportTests()
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
        public async Task HideForReportAsync_ShouldDoNothing_WhenReviewNotFound()
        {
            // Arrange
            string reviewId = "R1";
            _reviewRepository.Setup(x => x.GetByIdForReportAsync(reviewId)).ReturnsAsync((Review?)null);

            // Act
            await _service.HideForReportAsync(reviewId, DateTime.UtcNow);

            // Assert
            _reviewRepository.Verify(x => x.UpdateAsync(It.IsAny<Review>()), Times.Never);
        }

        [Fact]
        public async Task HideForReportAsync_ShouldMarkDeletedAndUpdateTime_WhenReviewExists()
        {
            // Arrange
            string reviewId = "R1";
            var review = new Review { ReviewId = reviewId, IsDeleted = false };
            var updatedAt = DateTime.UtcNow;

            _reviewRepository.Setup(x => x.GetByIdForReportAsync(reviewId)).ReturnsAsync(review);
            _reviewRepository.Setup(x => x.UpdateAsync(review)).Returns(Task.CompletedTask);

            // Act
            await _service.HideForReportAsync(reviewId, updatedAt);

            // Assert
            review.IsDeleted.Should().BeTrue();
            review.UpdatedAt.Should().Be(updatedAt);

            _reviewRepository.Verify(x => x.UpdateAsync(review), Times.Once);
        }
    }
}
