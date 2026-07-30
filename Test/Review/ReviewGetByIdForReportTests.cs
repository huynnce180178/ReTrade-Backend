using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using RetradeBE.Models;
using RetradeBE.Repositories;
using RetradeBE.Services;
using Xunit;

namespace Test.ReviewTests
{
    public class ReviewGetByIdForReportTests
    {
        private readonly Mock<IOrderRepository> _orderRepository;
        private readonly Mock<IReviewRepository> _reviewRepository;
        private readonly Mock<IReportRepository> _reportRepository;
        private readonly Mock<IAccountRepository> _accountRepository;
        private readonly Mock<INotificationService> _notificationService;
        private readonly ReviewService _service;

        public ReviewGetByIdForReportTests()
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
        public async Task GetByIdForReportAsync_ShouldCallRepositoryAndReturnReview()
        {
            // Arrange
            string reviewId = "R1";
            var expectedReview = new Review { ReviewId = reviewId, Comment = "Good product" };
            _reviewRepository.Setup(x => x.GetByIdForReportAsync(reviewId)).ReturnsAsync(expectedReview);

            // Act
            var result = await _service.GetByIdForReportAsync(reviewId);

            // Assert
            result.Should().NotBeNull();
            result!.ReviewId.Should().Be(reviewId);
            result.Comment.Should().Be("Good product");

            _reviewRepository.Verify(x => x.GetByIdForReportAsync(reviewId), Times.Once);
        }
    }
}
