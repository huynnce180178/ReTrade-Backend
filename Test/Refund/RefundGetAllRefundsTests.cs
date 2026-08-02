using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using RetradeBE.Models;
using RetradeBE.Models.DTOs;
using RetradeBE.Repositories.Refund;
using RetradeBE.Services;
using RetradeBE.Services.Refund;
using Xunit;

namespace Test.RefundTests
{
    public class RefundGetAllRefundsTests
    {
        private readonly Mock<IRefundRepository> _refundRepository;
        private readonly Mock<INotificationService> _notificationService;
        private readonly RefundService _service;

        public RefundGetAllRefundsTests()
        {
            _refundRepository = new Mock<IRefundRepository>();
            _notificationService = new Mock<INotificationService>();

            _service = new RefundService(
                _refundRepository.Object,
                _notificationService.Object
            );
        }

        #region Normal Tests (N)

        [Fact]
        public async Task GetAllRefundsAsync_ShouldReturnMappedResponseDtos_WhenRefundRequestsExist()
        {
            // Arrange
            var user = new User
            {
                UserId = "user_1",
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@example.com"
            };

            var refunds = new List<RefundRequest>
            {
                new RefundRequest
                {
                    RefundRequestId = "ref_1",
                    UserId = "user_1",
                    User = user,
                    Amount = 100000m,
                    Note = "Refund 1 note",
                    Status = "Pending",
                    RequestedAt = DateTime.UtcNow,
                    BankName = "Bank A",
                    BankAccountNumber = "12345",
                    BankAccountHolder = "John Doe"
                }
            };

            _refundRepository.Setup(r => r.GetAllRefundsWithUserAsync()).ReturnsAsync(refunds);

            // Act
            var result = await _service.GetAllRefundsAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(1);

            var first = result.First();
            first.RefundRequestId.Should().Be("ref_1");
            first.UserId.Should().Be("user_1");
            first.UserName.Should().Be("John Doe");
            first.UserEmail.Should().Be("john.doe@example.com");
            first.Amount.Should().Be(100000m);
            first.Note.Should().Be("Refund 1 note");
            first.Status.Should().Be("Pending");
            first.BankName.Should().Be("Bank A");
            first.BankAccountNumber.Should().Be("12345");
            first.BankAccountHolder.Should().Be("John Doe");

            _refundRepository.Verify(r => r.GetAllRefundsWithUserAsync(), Times.Once);
        }

        [Fact]
        public async Task GetAllRefundsAsync_ShouldReturnEmptyList_WhenNoRefundRequestsExist()
        {
            // Arrange
            _refundRepository.Setup(r => r.GetAllRefundsWithUserAsync()).ReturnsAsync(new List<RefundRequest>());

            // Act
            var result = await _service.GetAllRefundsAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();

            _refundRepository.Verify(r => r.GetAllRefundsWithUserAsync(), Times.Once);
        }

        #endregion

        #region Boundary Tests (B)

        [Fact]
        public async Task GetAllRefundsAsync_ShouldReturnDtoWithEmptyUserNameAndEmail_WhenUserIsNull()
        {
            // Arrange
            var refunds = new List<RefundRequest>
            {
                new RefundRequest
                {
                    RefundRequestId = "ref_no_user",
                    UserId = "user_missing",
                    User = null, // User info is missing
                    Amount = 50000m,
                    Status = "Approved"
                }
            };

            _refundRepository.Setup(r => r.GetAllRefundsWithUserAsync()).ReturnsAsync(refunds);

            // Act
            var result = await _service.GetAllRefundsAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(1);

            var first = result.First();
            first.RefundRequestId.Should().Be("ref_no_user");
            first.UserName.Should().BeEmpty();
            first.UserEmail.Should().BeEmpty();

            _refundRepository.Verify(r => r.GetAllRefundsWithUserAsync(), Times.Once);
        }

        #endregion
    }
}
