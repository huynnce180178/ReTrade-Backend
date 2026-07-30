using System;
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
    public class ReviewCreateTests
    {
        private readonly Mock<IOrderRepository> _orderRepository;
        private readonly Mock<IReviewRepository> _reviewRepository;
        private readonly Mock<IReportRepository> _reportRepository;
        private readonly Mock<IAccountRepository> _accountRepository;
        private readonly Mock<INotificationService> _notificationService;
        private readonly ReviewService _service;

        public ReviewCreateTests()
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
        public async Task CreateAsync_ShouldThrowUnauthorizedAccessException_WhenAccountNotFound()
        {
            // Arrange
            string accountId = "invalid_acc";
            _accountRepository.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync((Account?)null);

            // Act
            Func<Task> act = async () => await _service.CreateAsync(accountId, "buyer_123", new ReviewCreateDto());

            // Assert
            await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("Account not found.");
        }

        [Fact]
        public async Task CreateAsync_ShouldThrowUnauthorizedAccessException_WhenBuyerIdDoesNotMatchAuthenticatedBuyerId()
        {
            // Arrange
            string accountId = "acc_123";
            string authenticatedUserId = "user_123";
            var account = new Account { AccountId = accountId, UserId = authenticatedUserId };

            _accountRepository.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);

            // Act
            Func<Task> act = async () => await _service.CreateAsync(accountId, "other_buyer_id", new ReviewCreateDto());

            // Assert
            await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("You can only create reviews for your own completed orders.");
        }

        [Fact]
        public async Task CreateAsync_ShouldThrowInvalidOperationException_WhenRequestOrOrderIdIsMissing()
        {
            // Arrange
            string accountId = "acc_123";
            string authenticatedUserId = "user_123";
            var account = new Account { AccountId = accountId, UserId = authenticatedUserId };

            _accountRepository.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);

            // Act & Assert
            Func<Task> actNullRequest = async () => await _service.CreateAsync(accountId, authenticatedUserId, null!);
            await actNullRequest.Should().ThrowAsync<InvalidOperationException>().WithMessage("Invalid review request.");

            Func<Task> actEmptyOrderId = async () => await _service.CreateAsync(accountId, authenticatedUserId, new ReviewCreateDto { OrderId = "" });
            await actEmptyOrderId.Should().ThrowAsync<InvalidOperationException>().WithMessage("Invalid review request.");
        }

        [Theory]
        [InlineData(0)]
        [InlineData(6)]
        public async Task CreateAsync_ShouldThrowInvalidOperationException_WhenRatingIsInvalid(int rating)
        {
            // Arrange
            string accountId = "acc_123";
            string authenticatedUserId = "user_123";
            var account = new Account { AccountId = accountId, UserId = authenticatedUserId };

            _accountRepository.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);

            var request = new ReviewCreateDto { OrderId = "order_123", Rating = rating };

            // Act
            Func<Task> act = async () => await _service.CreateAsync(accountId, authenticatedUserId, request);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Rating must be between 1 and 5.");
        }

        [Fact]
        public async Task CreateAsync_ShouldReturnNull_WhenOrderNotFoundOrNotBelongingToBuyer()
        {
            // Arrange
            string accountId = "acc_123";
            string authenticatedUserId = "user_123";
            var account = new Account { AccountId = accountId, UserId = authenticatedUserId };

            _accountRepository.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);

            var request = new ReviewCreateDto { OrderId = "order_123", Rating = 5 };

            // 1. Order is null
            _orderRepository.Setup(x => x.GetForUpdateAsync("order_123")).ReturnsAsync((Order?)null);
            var resultNullOrder = await _service.CreateAsync(accountId, authenticatedUserId, request);
            resultNullOrder.Should().BeNull();

            // 2. Order belongs to other buyer
            var orderOfOtherBuyer = new Order { OrderId = "order_123", BuyerId = "other_buyer" };
            _orderRepository.Setup(x => x.GetForUpdateAsync("order_123")).ReturnsAsync(orderOfOtherBuyer);
            var resultOtherBuyer = await _service.CreateAsync(accountId, authenticatedUserId, request);
            resultOtherBuyer.Should().BeNull();
        }

        [Fact]
        public async Task CreateAsync_ShouldThrowInvalidOperationException_WhenBuyerTriesToReviewTheirOwnStore()
        {
            // Arrange
            string accountId = "acc_123";
            string authenticatedUserId = "user_123";
            var account = new Account { AccountId = accountId, UserId = authenticatedUserId };

            _accountRepository.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);

            var order = new Order { OrderId = "order_123", BuyerId = authenticatedUserId, SellerId = authenticatedUserId };
            _orderRepository.Setup(x => x.GetForUpdateAsync("order_123")).ReturnsAsync(order);

            var request = new ReviewCreateDto { OrderId = "order_123", Rating = 5 };

            // Act
            Func<Task> act = async () => await _service.CreateAsync(accountId, authenticatedUserId, request);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("You cannot review your own store.");
        }

        [Fact]
        public async Task CreateAsync_ShouldThrowInvalidOperationException_WhenOrderIsNotCompleted()
        {
            // Arrange
            string accountId = "acc_123";
            string authenticatedUserId = "user_123";
            var account = new Account { AccountId = accountId, UserId = authenticatedUserId };

            _accountRepository.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);

            var order = new Order { OrderId = "order_123", BuyerId = authenticatedUserId, SellerId = "seller_123", Status = "Pending" };
            _orderRepository.Setup(x => x.GetForUpdateAsync("order_123")).ReturnsAsync(order);

            var request = new ReviewCreateDto { OrderId = "order_123", Rating = 5 };

            // Act
            Func<Task> act = async () => await _service.CreateAsync(accountId, authenticatedUserId, request);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Review can only be submitted after the order is completed.");
        }

        [Fact]
        public async Task CreateAsync_ShouldThrowInvalidOperationException_WhenReviewAlreadyExists()
        {
            // Arrange
            string accountId = "acc_123";
            string authenticatedUserId = "user_123";
            var account = new Account { AccountId = accountId, UserId = authenticatedUserId };

            _accountRepository.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);

            var order = new Order { OrderId = "order_123", BuyerId = authenticatedUserId, SellerId = "seller_123", Status = "Completed" };
            _orderRepository.Setup(x => x.GetForUpdateAsync("order_123")).ReturnsAsync(order);

            _reviewRepository.Setup(x => x.GetByBuyerOrderAsync(authenticatedUserId, "order_123")).ReturnsAsync(new Review());

            var request = new ReviewCreateDto { OrderId = "order_123", Rating = 5 };

            // Act
            Func<Task> act = async () => await _service.CreateAsync(accountId, authenticatedUserId, request);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("You have already reviewed this order.");
        }

        [Fact]
        public async Task CreateAsync_ShouldSaveAndReturnReview_WhenAllConditionsAreMet()
        {
            // Arrange
            string accountId = "acc_123";
            string authenticatedUserId = "user_123";
            var account = new Account { AccountId = accountId, UserId = authenticatedUserId };

            _accountRepository.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);

            var order = new Order
            {
                OrderId = "order_123",
                BuyerId = authenticatedUserId,
                SellerId = "seller_123",
                Status = "Completed",
                Buyer = new User { UserId = authenticatedUserId, FirstName = "John", LastName = "Doe" }
            };
            _orderRepository.Setup(x => x.GetForUpdateAsync("order_123")).ReturnsAsync(order);

            _reviewRepository.Setup(x => x.GetByBuyerOrderAsync(authenticatedUserId, "order_123")).ReturnsAsync((Review?)null);
            _reviewRepository.Setup(x => x.AddAsync(It.IsAny<Review>())).Returns(Task.CompletedTask);

            var request = new ReviewCreateDto { OrderId = "order_123", Rating = 5, Comment = "Excellent product!" };

            // Act
            var result = await _service.CreateAsync(accountId, authenticatedUserId, request);

            // Assert
            result.Should().NotBeNull();
            result!.Rating.Should().Be(5);
            result.Comment.Should().Be("Excellent product!");
            result.ReviewerName.Should().Be("John Doe");

            _reviewRepository.Verify(x => x.AddAsync(It.Is<Review>(r =>
                r.Rating == 5 &&
                r.Comment == "Excellent product!" &&
                r.ReviewerId == authenticatedUserId &&
                r.OrderId == "order_123" &&
                r.SellerId == "seller_123"
            )), Times.Once);
        }
    }
}
