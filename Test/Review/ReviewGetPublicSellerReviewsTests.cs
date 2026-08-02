using System;
using System.Collections.Generic;
using System.Linq;
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
    public class ReviewGetPublicSellerReviewsTests
    {
        private readonly Mock<IOrderRepository> _orderRepository;
        private readonly Mock<IReviewRepository> _reviewRepository;
        private readonly Mock<IReportRepository> _reportRepository;
        private readonly Mock<IAccountRepository> _accountRepository;
        private readonly Mock<INotificationService> _notificationService;
        private readonly IMapper _mapper;
        private readonly ReviewService _service;

        public ReviewGetPublicSellerReviewsTests()
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

        [Fact]
        public async Task GetPublicSellerReviewsAsync_ShouldFilterByRating_WhenRatingQueryIsSpecified()
        {
            // Arrange
            string sellerId = "seller_1";
            var query = new ReviewQueryDto { Rating = 5, Page = 1, PageSize = 10 };

            var reviews = new List<Review>
            {
                new Review { ReviewId = "r1", SellerId = sellerId, Rating = 5, CreatedAt = DateTime.UtcNow },
                new Review { ReviewId = "r2", SellerId = sellerId, Rating = 3, CreatedAt = DateTime.UtcNow }
            }.AsAsyncQueryable();

            _reviewRepository.Setup(x => x.Query()).Returns(reviews);
            _reportRepository.Setup(x => x.Query()).Returns(new List<Report>().AsAsyncQueryable());

            // Act
            var result = await _service.GetPublicSellerReviewsAsync(sellerId, query);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().ContainSingle();
            result.Items.First().ReviewId.Should().Be("r1");
            result.Items.First().Rating.Should().Be(5);
        }

        [Fact]
        public async Task GetPublicSellerReviewsAsync_ShouldFilterBySearchTerm_WhenSearchTermMatchesComment()
        {
            // Arrange
            string sellerId = "seller_1";
            var query = new ReviewQueryDto { SearchTerm = "Awesome", Page = 1, PageSize = 10 };

            var reviews = new List<Review>
            {
                new Review { ReviewId = "r1", SellerId = sellerId, Comment = "This is awesome", Rating = 5, CreatedAt = DateTime.UtcNow },
                new Review { ReviewId = "r2", SellerId = sellerId, Comment = "Not good", Rating = 2, CreatedAt = DateTime.UtcNow }
            }.AsAsyncQueryable();

            _reviewRepository.Setup(x => x.Query()).Returns(reviews);
            _reportRepository.Setup(x => x.Query()).Returns(new List<Report>().AsAsyncQueryable());

            // Act
            var result = await _service.GetPublicSellerReviewsAsync(sellerId, query);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().ContainSingle();
            result.Items.First().ReviewId.Should().Be("r1");
        }

        [Fact]
        public async Task GetPublicSellerReviewsAsync_ShouldFilterBySearchTerm_WhenSearchTermMatchesOrderCodeOrProductName()
        {
            // Arrange
            string sellerId = "seller_1";
            var query = new ReviewQueryDto { SearchTerm = "ORD123", Page = 1, PageSize = 10 };

            var reviews = new List<Review>
            {
                new Review 
                { 
                    ReviewId = "r1", 
                    SellerId = sellerId, 
                    Rating = 5, 
                    CreatedAt = DateTime.UtcNow,
                    Order = new Order { OrderCode = "ORD123", Product = new Product { Name = "Phone" } }
                },
                new Review 
                { 
                    ReviewId = "r2", 
                    SellerId = sellerId, 
                    Rating = 2, 
                    CreatedAt = DateTime.UtcNow,
                    Order = new Order { OrderCode = "ORD999", Product = new Product { Name = "Laptop" } }
                }
            }.AsAsyncQueryable();

            _reviewRepository.Setup(x => x.Query()).Returns(reviews);
            _reportRepository.Setup(x => x.Query()).Returns(new List<Report>().AsAsyncQueryable());

            // Act
            var result = await _service.GetPublicSellerReviewsAsync(sellerId, query);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().ContainSingle();
            result.Items.First().ReviewId.Should().Be("r1");
        }

        [Fact]
        public async Task GetPublicSellerReviewsAsync_ShouldFilterBySearchTerm_WhenSearchTermMatchesReviewerName()
        {
            // Arrange
            string sellerId = "seller_1";
            var query = new ReviewQueryDto { SearchTerm = "John", Page = 1, PageSize = 10 };

            var reviews = new List<Review>
            {
                new Review 
                { 
                    ReviewId = "r1", 
                    SellerId = sellerId, 
                    Rating = 5, 
                    CreatedAt = DateTime.UtcNow,
                    Reviewer = new User { FirstName = "John", LastName = "Doe" }
                },
                new Review 
                { 
                    ReviewId = "r2", 
                    SellerId = sellerId, 
                    Rating = 2, 
                    CreatedAt = DateTime.UtcNow,
                    Reviewer = new User { FirstName = "Jane", LastName = "Smith" }
                }
            }.AsAsyncQueryable();

            _reviewRepository.Setup(x => x.Query()).Returns(reviews);
            _reportRepository.Setup(x => x.Query()).Returns(new List<Report>().AsAsyncQueryable());

            // Act
            var result = await _service.GetPublicSellerReviewsAsync(sellerId, query);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().ContainSingle();
            result.Items.First().ReviewId.Should().Be("r1");
        }

        [Fact]
        public async Task GetPublicSellerReviewsAsync_ShouldFilterByStatusReported_WhenStatusIsReported()
        {
            // Arrange
            string sellerId = "seller_1";
            var query = new ReviewQueryDto { Status = "Reported", Page = 1, PageSize = 10 };

            var reviews = new List<Review>
            {
                new Review { ReviewId = "r1", SellerId = sellerId, Rating = 5, CreatedAt = DateTime.UtcNow },
                new Review { ReviewId = "r2", SellerId = sellerId, Rating = 2, CreatedAt = DateTime.UtcNow }
            }.AsAsyncQueryable();

            var reports = new List<Report>
            {
                new Report { ReportId = "rep1", TargetId = "r1", TargetType = "Review" }
            }.AsAsyncQueryable();

            _reviewRepository.Setup(x => x.Query()).Returns(reviews);
            _reportRepository.Setup(x => x.Query()).Returns(reports);

            // Act
            var result = await _service.GetPublicSellerReviewsAsync(sellerId, query);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().ContainSingle();
            result.Items.First().ReviewId.Should().Be("r1");
        }

        [Fact]
        public async Task GetPublicSellerReviewsAsync_ShouldFilterByStatusUnreported_WhenStatusIsUnreported()
        {
            // Arrange
            string sellerId = "seller_1";
            var query = new ReviewQueryDto { Status = "Unreported", Page = 1, PageSize = 10 };

            var reviews = new List<Review>
            {
                new Review { ReviewId = "r1", SellerId = sellerId, Rating = 5, CreatedAt = DateTime.UtcNow },
                new Review { ReviewId = "r2", SellerId = sellerId, Rating = 2, CreatedAt = DateTime.UtcNow }
            }.AsAsyncQueryable();

            var reports = new List<Report>
            {
                new Report { ReportId = "rep1", TargetId = "r1", TargetType = "Review" }
            }.AsAsyncQueryable();

            _reviewRepository.Setup(x => x.Query()).Returns(reviews);
            _reportRepository.Setup(x => x.Query()).Returns(reports);

            // Act
            var result = await _service.GetPublicSellerReviewsAsync(sellerId, query);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().ContainSingle();
            result.Items.First().ReviewId.Should().Be("r2");
        }

        [Fact]
        public async Task GetPublicSellerReviewsAsync_ShouldSortByOldest_WhenSortByIsOldest()
        {
            // Arrange
            string sellerId = "seller_1";
            var query = new ReviewQueryDto { SortBy = "oldest", Page = 1, PageSize = 10 };
            var now = DateTime.UtcNow;

            var reviews = new List<Review>
            {
                new Review { ReviewId = "r1", SellerId = sellerId, CreatedAt = now },
                new Review { ReviewId = "r2", SellerId = sellerId, CreatedAt = now.AddHours(-1) },
                new Review { ReviewId = "r3", SellerId = sellerId, CreatedAt = now.AddHours(-2) }
            }.AsAsyncQueryable();

            _reviewRepository.Setup(x => x.Query()).Returns(reviews);
            _reportRepository.Setup(x => x.Query()).Returns(new List<Report>().AsAsyncQueryable());

            // Act
            var result = await _service.GetPublicSellerReviewsAsync(sellerId, query);

            // Assert
            result.Should().NotBeNull();
            result.Items.Select(x => x.ReviewId).Should().ContainInOrder("r3", "r2", "r1");
        }

        [Fact]
        public async Task GetPublicSellerReviewsAsync_ShouldSortByRatingDesc_WhenSortByIsRatingDesc()
        {
            // Arrange
            string sellerId = "seller_1";
            var query = new ReviewQueryDto { SortBy = "rating_desc", Page = 1, PageSize = 10 };

            var reviews = new List<Review>
            {
                new Review { ReviewId = "r1", SellerId = sellerId, Rating = 3, CreatedAt = DateTime.UtcNow },
                new Review { ReviewId = "r2", SellerId = sellerId, Rating = 5, CreatedAt = DateTime.UtcNow },
                new Review { ReviewId = "r3", SellerId = sellerId, Rating = 4, CreatedAt = DateTime.UtcNow }
            }.AsAsyncQueryable();

            _reviewRepository.Setup(x => x.Query()).Returns(reviews);
            _reportRepository.Setup(x => x.Query()).Returns(new List<Report>().AsAsyncQueryable());

            // Act
            var result = await _service.GetPublicSellerReviewsAsync(sellerId, query);

            // Assert
            result.Should().NotBeNull();
            result.Items.Select(x => x.ReviewId).Should().ContainInOrder("r2", "r3", "r1");
        }

        [Fact]
        public async Task GetPublicSellerReviewsAsync_ShouldSortByRatingAsc_WhenSortByIsRatingAsc()
        {
            // Arrange
            string sellerId = "seller_1";
            var query = new ReviewQueryDto { SortBy = "rating_asc", Page = 1, PageSize = 10 };

            var reviews = new List<Review>
            {
                new Review { ReviewId = "r1", SellerId = sellerId, Rating = 3, CreatedAt = DateTime.UtcNow },
                new Review { ReviewId = "r2", SellerId = sellerId, Rating = 5, CreatedAt = DateTime.UtcNow },
                new Review { ReviewId = "r3", SellerId = sellerId, Rating = 1, CreatedAt = DateTime.UtcNow }
            }.AsAsyncQueryable();

            _reviewRepository.Setup(x => x.Query()).Returns(reviews);
            _reportRepository.Setup(x => x.Query()).Returns(new List<Report>().AsAsyncQueryable());

            // Act
            var result = await _service.GetPublicSellerReviewsAsync(sellerId, query);

            // Assert
            result.Should().NotBeNull();
            result.Items.Select(x => x.ReviewId).Should().ContainInOrder("r3", "r1", "r2");
        }

        [Fact]
        public async Task GetPublicSellerReviewsAsync_ShouldSortByReportedCount_WhenSortByIsReported()
        {
            // Arrange
            string sellerId = "seller_1";
            var query = new ReviewQueryDto { SortBy = "reported", Page = 1, PageSize = 10 };

            var reviews = new List<Review>
            {
                new Review { ReviewId = "r1", SellerId = sellerId, CreatedAt = DateTime.UtcNow.AddMinutes(-5) },
                new Review { ReviewId = "r2", SellerId = sellerId, CreatedAt = DateTime.UtcNow.AddMinutes(-10) },
                new Review { ReviewId = "r3", SellerId = sellerId, CreatedAt = DateTime.UtcNow }
            }.AsAsyncQueryable();

            var reports = new List<Report>
            {
                new Report { ReportId = "rep1", TargetId = "r1", TargetType = "Review", CreatedAt = DateTime.UtcNow },
                new Report { ReportId = "rep2", TargetId = "r1", TargetType = "Review", CreatedAt = DateTime.UtcNow.AddMinutes(1) },
                new Report { ReportId = "rep3", TargetId = "r2", TargetType = "Review", CreatedAt = DateTime.UtcNow }
            }.AsAsyncQueryable();

            _reviewRepository.Setup(x => x.Query()).Returns(reviews);
            _reportRepository.Setup(x => x.Query()).Returns(reports);

            // Act
            var result = await _service.GetPublicSellerReviewsAsync(sellerId, query);

            // Assert
            result.Should().NotBeNull();
            result.Items.Select(x => x.ReviewId).Should().ContainInOrder("r1", "r2", "r3");
        }
        #endregion

        #region Abnormal Tests (A)
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
        public async Task GetPublicSellerReviewsAsync_ShouldReturnEmptyPagedResult_WhenSellerIdDoesNotExistAndNoReviewsFound()
        {
            // Arrange
            string sellerId = "non_existent_seller";
            var query = new ReviewQueryDto { Page = 1, PageSize = 10 };

            _accountRepository.Setup(x => x.GetByIdAsync(sellerId)).ReturnsAsync((Account?)null);
            _reviewRepository.Setup(x => x.Query()).Returns(new List<Review>().AsAsyncQueryable());
            _reportRepository.Setup(x => x.Query()).Returns(new List<Report>().AsAsyncQueryable());

            // Act
            var result = await _service.GetPublicSellerReviewsAsync(sellerId, query);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().BeEmpty();
            result.TotalItems.Should().Be(0);
        }
        #endregion

        #region Boundary Tests (B)
        [Fact]
        public async Task GetPublicSellerReviewsAsync_ShouldPagingCorrectly_WhenPageAndPageSizeAreValid()
        {
            // Arrange
            string sellerId = "seller_1";
            var query = new ReviewQueryDto { Page = 2, PageSize = 2 };

            var reviews = new List<Review>
            {
                new Review { ReviewId = "r1", SellerId = sellerId, CreatedAt = DateTime.UtcNow.AddMinutes(5) },
                new Review { ReviewId = "r2", SellerId = sellerId, CreatedAt = DateTime.UtcNow.AddMinutes(4) },
                new Review { ReviewId = "r3", SellerId = sellerId, CreatedAt = DateTime.UtcNow.AddMinutes(3) },
                new Review { ReviewId = "r4", SellerId = sellerId, CreatedAt = DateTime.UtcNow.AddMinutes(2) },
                new Review { ReviewId = "r5", SellerId = sellerId, CreatedAt = DateTime.UtcNow.AddMinutes(1) }
            }.AsAsyncQueryable();

            _reviewRepository.Setup(x => x.Query()).Returns(reviews);
            _reportRepository.Setup(x => x.Query()).Returns(new List<Report>().AsAsyncQueryable());

            // Act
            var result = await _service.GetPublicSellerReviewsAsync(sellerId, query);

            // Assert
            result.Should().NotBeNull();
            result.TotalItems.Should().Be(5);
            result.TotalPages.Should().Be(3);
            result.Page.Should().Be(2);
            result.PageSize.Should().Be(2);
            result.Items.Select(x => x.ReviewId).Should().ContainInOrder("r3", "r4");
        }

        [Fact]
        public async Task GetPublicSellerReviewsAsync_ShouldClampInvalidPageAndPageSize_ToDefaultValues()
        {
            // Arrange
            string sellerId = "seller_1";
            var query = new ReviewQueryDto { Page = 0, PageSize = -5 };

            _reviewRepository.Setup(x => x.Query()).Returns(new List<Review>().AsAsyncQueryable());
            _reportRepository.Setup(x => x.Query()).Returns(new List<Report>().AsAsyncQueryable());

            // Act
            var result = await _service.GetPublicSellerReviewsAsync(sellerId, query);

            // Assert
            result.Should().NotBeNull();
            result.Page.Should().Be(1);
            result.PageSize.Should().Be(12);
        }

        [Fact]
        public async Task GetPublicSellerReviewsAsync_ShouldCapPageSize_WhenPageSizeIsTooLarge()
        {
            // Arrange
            string sellerId = "seller_1";
            var query = new ReviewQueryDto { Page = 1, PageSize = 200 };

            _reviewRepository.Setup(x => x.Query()).Returns(new List<Review>().AsAsyncQueryable());
            _reportRepository.Setup(x => x.Query()).Returns(new List<Report>().AsAsyncQueryable());

            // Act
            var result = await _service.GetPublicSellerReviewsAsync(sellerId, query);

            // Assert
            result.Should().NotBeNull();
            result.PageSize.Should().Be(100);
        }
        #endregion
    }
}

