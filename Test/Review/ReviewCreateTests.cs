using System;
using System.Collections.Generic;
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

        #region Normal Tests (N)
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
                OrderCode = "ORD-123",
                BuyerId = authenticatedUserId,
                SellerId = "seller_123",
                Status = "Completed",
                Buyer = new User { UserId = authenticatedUserId, FirstName = "John", LastName = "Doe" }
            };
            _orderRepository.Setup(x => x.GetForUpdateAsync("order_123")).ReturnsAsync(order);
            _reviewRepository.Setup(x => x.GetByBuyerOrderAsync(authenticatedUserId, "order_123")).ReturnsAsync((Review?)null);
            _reviewRepository.Setup(x => x.AddAsync(It.IsAny<Review>())).Returns(Task.CompletedTask);
            _notificationService.Setup(x => x.CreateAndSendAsync(It.IsAny<CreateNotificationDto>())).ReturnsAsync(new NotificationDto());

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
            _notificationService.Verify(x => x.CreateAndSendAsync(It.IsAny<CreateNotificationDto>()), Times.Once);
        }
        #endregion

        #region Abnormal Tests (A)
        [Fact]
        public async Task CreateAsync_ShouldThrowUnauthorizedAccessException_WhenAccountNotFound()
        {
            // Arrange
            _accountRepository.Setup(x => x.GetByIdAsync("invalid_acc")).ReturnsAsync((Account?)null);

            // Act
            Func<Task> act = async () => await _service.CreateAsync("invalid_acc", "buyer_123", new ReviewCreateDto());

            // Assert
            await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("Account not found.");
        }

        [Fact]
        public async Task CreateAsync_ShouldThrowUnauthorizedAccessException_WhenBuyerIdMismatch()
        {
            // Arrange
            string accountId = "acc_123";
            string authenticatedUserId = "user_123";
            _accountRepository.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(new Account { AccountId = accountId, UserId = authenticatedUserId });

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
            _accountRepository.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(new Account { AccountId = accountId, UserId = authenticatedUserId });

            // Scenario 1: Null request
            Func<Task> actNull = async () => await _service.CreateAsync(accountId, authenticatedUserId, null!);
            await actNull.Should().ThrowAsync<InvalidOperationException>().WithMessage("Invalid review request.");

            // Scenario 2: Empty OrderId
            Func<Task> actEmptyOrder = async () => await _service.CreateAsync(accountId, authenticatedUserId, new ReviewCreateDto { OrderId = "" });
            await actEmptyOrder.Should().ThrowAsync<InvalidOperationException>().WithMessage("Invalid review request.");
        }

        [Fact]
        public async Task CreateAsync_ShouldThrowInvalidOperationException_WhenRatingIsInvalid()
        {
            // Arrange
            string accountId = "acc_123";
            string authenticatedUserId = "user_123";
            _accountRepository.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(new Account { AccountId = accountId, UserId = authenticatedUserId });

            // Scenario 1: Rating < 1
            Func<Task> actLow = async () => await _service.CreateAsync(accountId, authenticatedUserId, new ReviewCreateDto { OrderId = "order_123", Rating = 0 });
            await actLow.Should().ThrowAsync<InvalidOperationException>().WithMessage("Rating must be between 1 and 5.");

            // Scenario 2: Rating > 5
            Func<Task> actHigh = async () => await _service.CreateAsync(accountId, authenticatedUserId, new ReviewCreateDto { OrderId = "order_123", Rating = 6 });
            await actHigh.Should().ThrowAsync<InvalidOperationException>().WithMessage("Rating must be between 1 and 5.");
        }

        [Fact]
        public async Task CreateAsync_ShouldReturnNull_WhenOrderNotFoundOrNotBelongingToBuyer()
        {
            // Arrange
            string accountId = "acc_123";
            string authenticatedUserId = "user_123";
            _accountRepository.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(new Account { AccountId = accountId, UserId = authenticatedUserId });
            var request = new ReviewCreateDto { OrderId = "order_123", Rating = 5 };

            // Scenario 1: Order null
            _orderRepository.Setup(x => x.GetForUpdateAsync("order_123")).ReturnsAsync((Order?)null);
            var resultNull = await _service.CreateAsync(accountId, authenticatedUserId, request);
            resultNull.Should().BeNull();

            // Scenario 2: Order belongs to another buyer
            var otherBuyerOrder = new Order { OrderId = "order_123", BuyerId = "other_buyer" };
            _orderRepository.Setup(x => x.GetForUpdateAsync("order_123")).ReturnsAsync(otherBuyerOrder);
            var resultOther = await _service.CreateAsync(accountId, authenticatedUserId, request);
            resultOther.Should().BeNull();
        }

        [Fact]
        public async Task CreateAsync_ShouldThrowInvalidOperationException_WhenReviewingOwnStoreOrOrderNotCompletedOrReviewExists()
        {
            // Arrange
            string accountId = "acc_123";
            string authenticatedUserId = "user_123";
            _accountRepository.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(new Account { AccountId = accountId, UserId = authenticatedUserId });
            var request = new ReviewCreateDto { OrderId = "order_123", Rating = 5 };

            // Scenario 1: Reviewing own store
            var ownStoreOrder = new Order { OrderId = "order_123", BuyerId = authenticatedUserId, SellerId = authenticatedUserId };
            _orderRepository.Setup(x => x.GetForUpdateAsync("order_123")).ReturnsAsync(ownStoreOrder);
            Func<Task> actOwn = async () => await _service.CreateAsync(accountId, authenticatedUserId, request);
            await actOwn.Should().ThrowAsync<InvalidOperationException>().WithMessage("You cannot review your own store.");

            // Scenario 2: Order not completed
            var pendingOrder = new Order { OrderId = "order_123", BuyerId = authenticatedUserId, SellerId = "seller_123", Status = "Pending" };
            _orderRepository.Setup(x => x.GetForUpdateAsync("order_123")).ReturnsAsync(pendingOrder);
            Func<Task> actPending = async () => await _service.CreateAsync(accountId, authenticatedUserId, request);
            await actPending.Should().ThrowAsync<InvalidOperationException>().WithMessage("Review can only be submitted after the order is completed.");

            // Scenario 3: Review already exists
            var completedOrder = new Order { OrderId = "order_123", BuyerId = authenticatedUserId, SellerId = "seller_123", Status = "Completed" };
            _orderRepository.Setup(x => x.GetForUpdateAsync("order_123")).ReturnsAsync(completedOrder);
            _reviewRepository.Setup(x => x.GetByBuyerOrderAsync(authenticatedUserId, "order_123")).ReturnsAsync(new Review());
            Func<Task> actExists = async () => await _service.CreateAsync(accountId, authenticatedUserId, request);
            await actExists.Should().ThrowAsync<InvalidOperationException>().WithMessage("You have already reviewed this order.");
        }
        #endregion

        #region Boundary Tests (B)
        [Fact]
        public async Task CreateAsync_ShouldSaveReviewSuccessfully_EvenIfSendNotificationFails()
        {
            // Arrange
            string accountId = "acc_123";
            string authenticatedUserId = "user_123";
            var account = new Account { AccountId = accountId, UserId = authenticatedUserId };

            _accountRepository.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);

            var order = new Order
            {
                OrderId = "order_123",
                OrderCode = "ORD-123",
                BuyerId = authenticatedUserId,
                SellerId = "seller_123",
                Status = "Completed",
                Buyer = new User { UserId = authenticatedUserId, FirstName = "John", LastName = "Doe" }
            };
            _orderRepository.Setup(x => x.GetForUpdateAsync("order_123")).ReturnsAsync(order);
            _reviewRepository.Setup(x => x.GetByBuyerOrderAsync(authenticatedUserId, "order_123")).ReturnsAsync((Review?)null);
            _reviewRepository.Setup(x => x.AddAsync(It.IsAny<Review>())).Returns(Task.CompletedTask);
            _notificationService.Setup(x => x.CreateAndSendAsync(It.IsAny<CreateNotificationDto>())).ThrowsAsync(new Exception("Notification service down"));

            var request = new ReviewCreateDto { OrderId = "order_123", Rating = 4, Comment = "Good quality" };

            // Act
            var result = await _service.CreateAsync(accountId, authenticatedUserId, request);

            // Assert
            result.Should().NotBeNull();
            result!.Rating.Should().Be(4);
            _reviewRepository.Verify(x => x.AddAsync(It.IsAny<Review>()), Times.Once);
        }
        #endregion
    }
}
