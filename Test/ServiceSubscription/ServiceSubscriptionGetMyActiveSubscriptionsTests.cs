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

namespace Test.ServiceSubscriptionTests
{
    public class ServiceSubscriptionGetMyActiveSubscriptionsTests
    {
        private readonly Mock<IServiceSubscriptionRepository> _serviceSubscriptionRepo;
        private readonly Mock<IMyServiceRepository> _myServiceRepo;
        private readonly Mock<IAccountRepository> _accountRepo;
        private readonly Mock<IPaymentService> _paymentService;
        private readonly ServiceSubscriptionService _service;

        public ServiceSubscriptionGetMyActiveSubscriptionsTests()
        {
            _serviceSubscriptionRepo = new Mock<IServiceSubscriptionRepository>();
            _myServiceRepo = new Mock<IMyServiceRepository>();
            _accountRepo = new Mock<IAccountRepository>();
            _paymentService = new Mock<IPaymentService>();

            _service = new ServiceSubscriptionService(
                _serviceSubscriptionRepo.Object,
                _myServiceRepo.Object,
                _accountRepo.Object,
                _paymentService.Object
            );
        }

        #region Normal Tests (N)

        [Fact]
        public async Task GetMyActiveSubscriptionsAsync_ShouldReturnActiveSubscriptions_WhenUserHasActiveAndUnexpiredSubscriptions()
        {
            // Arrange
            string accountId = "acc_123";
            string userId = "user_123";
            var now = DateTime.UtcNow;

            var account = new Account { AccountId = accountId, UserId = userId };
            _accountRepo.Setup(x => x.Query()).Returns(new List<Account> { account }.AsAsyncQueryable());

            var activeSub = new MyService
            {
                UserSubId = "sub_active",
                UserId = userId,
                ServiceId = "svc_1",
                Status = "Active",
                StartDate = now.AddDays(-5),
                EndDate = now.AddDays(25)
            };

            _myServiceRepo.Setup(x => x.Query()).Returns(new List<MyService> { activeSub }.AsAsyncQueryable());

            // Act
            var result = (await _service.GetMyActiveSubscriptionsAsync(accountId)).ToList();

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            result.First().UserSubId.Should().Be("sub_active");
            result.First().ServiceId.Should().Be("svc_1");
            result.First().Status.Should().Be("Active");
        }

        [Fact]
        public async Task GetMyActiveSubscriptionsAsync_ShouldReturnMultipleActiveSubscriptions_WhenUserHasMultipleActivePackages()
        {
            // Arrange
            string accountId = "acc_123";
            string userId = "user_123";
            var now = DateTime.UtcNow;

            var account = new Account { AccountId = accountId, UserId = userId };
            _accountRepo.Setup(x => x.Query()).Returns(new List<Account> { account }.AsAsyncQueryable());

            var sub1 = new MyService { UserSubId = "sub_1", UserId = userId, ServiceId = "s1", Status = "Active", EndDate = now.AddDays(10) };
            var sub2 = new MyService { UserSubId = "sub_2", UserId = userId, ServiceId = "s2", Status = "Active", EndDate = now.AddDays(20) };

            _myServiceRepo.Setup(x => x.Query()).Returns(new List<MyService> { sub1, sub2 }.AsAsyncQueryable());

            // Act
            var result = (await _service.GetMyActiveSubscriptionsAsync(accountId)).ToList();

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetMyActiveSubscriptionsAsync_ShouldMapAllDtoPropertiesCorrectly_WhenActiveSubscriptionExists()
        {
            // Arrange
            string accountId = "acc_123";
            string userId = "user_123";
            var startDate = DateTime.UtcNow.AddDays(-10);
            var endDate = DateTime.UtcNow.AddDays(20);

            var account = new Account { AccountId = accountId, UserId = userId };
            _accountRepo.Setup(x => x.Query()).Returns(new List<Account> { account }.AsAsyncQueryable());

            var activeSub = new MyService
            {
                UserSubId = "sub_full_dto",
                UserId = userId,
                ServiceId = "svc_vip",
                Status = "Active",
                StartDate = startDate,
                EndDate = endDate
            };

            _myServiceRepo.Setup(x => x.Query()).Returns(new List<MyService> { activeSub }.AsAsyncQueryable());

            // Act
            var result = (await _service.GetMyActiveSubscriptionsAsync(accountId)).ToList();

            // Assert
            result.Should().NotBeNull();
            var dto = result.First();
            dto.UserSubId.Should().Be("sub_full_dto");
            dto.ServiceId.Should().Be("svc_vip");
            dto.Status.Should().Be("Active");
            dto.StartDate.Should().Be(startDate);
            dto.EndDate.Should().Be(endDate);
        }

        #endregion

        #region Abnormal Tests (A)

        [Fact]
        public async Task GetMyActiveSubscriptionsAsync_ShouldReturnEmpty_WhenAccountNotFound()
        {
            // Arrange
            string accountId = "invalid_acc";
            _accountRepo.Setup(x => x.Query()).Returns(new List<Account>().AsAsyncQueryable());

            // Act
            var result = await _service.GetMyActiveSubscriptionsAsync(accountId);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetMyActiveSubscriptionsAsync_ShouldReturnEmpty_WhenAccountUserIdIsNull()
        {
            // Arrange
            string accountId = "acc_unlinked";
            var account = new Account { AccountId = accountId, UserId = null };
            _accountRepo.Setup(x => x.Query()).Returns(new List<Account> { account }.AsAsyncQueryable());

            // Act
            var result = await _service.GetMyActiveSubscriptionsAsync(accountId);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetMyActiveSubscriptionsAsync_ShouldReturnEmpty_WhenAccountUserIdIsEmpty()
        {
            // Arrange
            string accountId = "acc_empty_user";
            var account = new Account { AccountId = accountId, UserId = "" };
            _accountRepo.Setup(x => x.Query()).Returns(new List<Account> { account }.AsAsyncQueryable());

            // Act
            var result = await _service.GetMyActiveSubscriptionsAsync(accountId);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetMyActiveSubscriptionsAsync_ShouldReturnEmpty_WhenUserHasNoSubscriptions()
        {
            // Arrange
            string accountId = "acc_123";
            string userId = "user_123";

            var account = new Account { AccountId = accountId, UserId = userId };
            _accountRepo.Setup(x => x.Query()).Returns(new List<Account> { account }.AsAsyncQueryable());
            _myServiceRepo.Setup(x => x.Query()).Returns(new List<MyService>().AsAsyncQueryable());

            // Act
            var result = await _service.GetMyActiveSubscriptionsAsync(accountId);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        #endregion

        #region Boundary Tests (B)

        [Fact]
        public async Task GetMyActiveSubscriptionsAsync_ShouldFilterOutExpiredSubscriptions_WhenEndDateIsBeforeNow()
        {
            // Arrange
            string accountId = "acc_123";
            string userId = "user_123";
            var now = DateTime.UtcNow;

            var account = new Account { AccountId = accountId, UserId = userId };
            _accountRepo.Setup(x => x.Query()).Returns(new List<Account> { account }.AsAsyncQueryable());

            var expiredSub = new MyService
            {
                UserSubId = "sub_expired",
                UserId = userId,
                Status = "Active",
                StartDate = now.AddDays(-30),
                EndDate = now.AddDays(-1)
            };

            _myServiceRepo.Setup(x => x.Query()).Returns(new List<MyService> { expiredSub }.AsAsyncQueryable());

            // Act
            var result = await _service.GetMyActiveSubscriptionsAsync(accountId);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetMyActiveSubscriptionsAsync_ShouldFilterOutNonActiveStatuses_SuchAsCancelledOrExpired()
        {
            // Arrange
            string accountId = "acc_123";
            string userId = "user_123";
            var now = DateTime.UtcNow;

            var account = new Account { AccountId = accountId, UserId = userId };
            _accountRepo.Setup(x => x.Query()).Returns(new List<Account> { account }.AsAsyncQueryable());

            var cancelledSub = new MyService
            {
                UserSubId = "sub_cancelled",
                UserId = userId,
                Status = "Cancelled",
                EndDate = now.AddDays(10)
            };

            var expiredStatusSub = new MyService
            {
                UserSubId = "sub_expired_status",
                UserId = userId,
                Status = "Expired",
                EndDate = now.AddDays(10)
            };

            _myServiceRepo.Setup(x => x.Query()).Returns(new List<MyService> { cancelledSub, expiredStatusSub }.AsAsyncQueryable());

            // Act
            var result = await _service.GetMyActiveSubscriptionsAsync(accountId);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetMyActiveSubscriptionsAsync_ShouldIncludeSubscription_WhenEndDateIsExactlyNowOrFuture()
        {
            // Arrange
            string accountId = "acc_123";
            string userId = "user_123";
            var now = DateTime.UtcNow;

            var account = new Account { AccountId = accountId, UserId = userId };
            _accountRepo.Setup(x => x.Query()).Returns(new List<Account> { account }.AsAsyncQueryable());

            var futureSub = new MyService
            {
                UserSubId = "sub_future",
                UserId = userId,
                ServiceId = "svc_future",
                Status = "Active",
                EndDate = now.AddHours(1)
            };

            _myServiceRepo.Setup(x => x.Query()).Returns(new List<MyService> { futureSub }.AsAsyncQueryable());

            // Act
            var result = (await _service.GetMyActiveSubscriptionsAsync(accountId)).ToList();

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            result.First().UserSubId.Should().Be("sub_future");
        }

        #endregion
    }
}
