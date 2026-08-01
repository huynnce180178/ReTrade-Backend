using System;
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
    public class RefundApproveRefundTests
    {
        private readonly Mock<IRefundRepository> _refundRepository;
        private readonly Mock<INotificationService> _notificationService;
        private readonly RefundService _service;

        public RefundApproveRefundTests()
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
        public async Task ApproveRefundAsync_ShouldMarkRefundAsProcessedAndSendNotification_WhenRequestIsPending()
        {
            // Arrange
            var refundId = "ref_100";
            var userId = "user_200";

            var refund = new RefundRequest
            {
                RefundRequestId = refundId,
                UserId = userId,
                Amount = 150000m,
                Status = "Pending"
            };

            _refundRepository.Setup(r => r.GetByIdAsync(refundId)).ReturnsAsync(refund);
            _refundRepository.Setup(r => r.UpdateAsync(It.IsAny<RefundRequest>())).Returns(Task.CompletedTask);
            _notificationService.Setup(n => n.CreateAndSendAsync(It.IsAny<CreateNotificationDto>())).ReturnsAsync(new NotificationDto());

            // Act
            var result = await _service.ApproveRefundAsync(refundId);

            // Assert
            result.Success.Should().BeTrue();
            result.Message.Should().Be("Refund marked as processed.");
            refund.Status.Should().Be("Processed");

            _refundRepository.Verify(r => r.GetByIdAsync(refundId), Times.Once);
            _refundRepository.Verify(r => r.UpdateAsync(It.Is<RefundRequest>(r => r.RefundRequestId == refundId && r.Status == "Processed")), Times.Once);
            _notificationService.Verify(n => n.CreateAndSendAsync(It.Is<CreateNotificationDto>(dto => dto.UserId == userId && dto.ReferenceId == refundId)), Times.Once);
        }
        #endregion

        #region Abnormal Tests (A)
        [Fact]
        public async Task ApproveRefundAsync_ShouldReturnFailure_WhenRefundNotFoundOrStatusNotPending()
        {
            // Scenario 1: Refund request not found
            _refundRepository.Setup(r => r.GetByIdAsync("invalid_ref")).ReturnsAsync((RefundRequest?)null);
            var resultNotFound = await _service.ApproveRefundAsync("invalid_ref");

            resultNotFound.Success.Should().BeFalse();
            resultNotFound.Message.Should().Be("Refund request not found.");

            // Scenario 2: Refund request status is not Pending (e.g. Processed or Rejected)
            var processedRefund = new RefundRequest { RefundRequestId = "ref_processed", Status = "Processed" };
            _refundRepository.Setup(r => r.GetByIdAsync("ref_processed")).ReturnsAsync(processedRefund);
            var resultNotPending = await _service.ApproveRefundAsync("ref_processed");

            resultNotPending.Success.Should().BeFalse();
            resultNotPending.Message.Should().Be("Only pending refund requests can be processed.");

            _refundRepository.Verify(r => r.UpdateAsync(It.IsAny<RefundRequest>()), Times.Never);
        }
        #endregion

        #region Boundary Tests (B)
        [Fact]
        public async Task ApproveRefundAsync_ShouldCompleteSuccessfully_EvenIfSendNotificationFails()
        {
            // Arrange
            var refundId = "ref_300";
            var userId = "user_300";

            var refund = new RefundRequest
            {
                RefundRequestId = refundId,
                UserId = userId,
                Amount = 200000m,
                Status = "Pending"
            };

            _refundRepository.Setup(r => r.GetByIdAsync(refundId)).ReturnsAsync(refund);
            _refundRepository.Setup(r => r.UpdateAsync(It.IsAny<RefundRequest>())).Returns(Task.CompletedTask);
            _notificationService.Setup(n => n.CreateAndSendAsync(It.IsAny<CreateNotificationDto>())).ThrowsAsync(new Exception("Notification service failed"));

            // Act
            var result = await _service.ApproveRefundAsync(refundId);

            // Assert
            result.Success.Should().BeTrue();
            result.Message.Should().Be("Refund marked as processed.");
            refund.Status.Should().Be("Processed");

            _refundRepository.Verify(r => r.UpdateAsync(It.IsAny<RefundRequest>()), Times.Once);
        }
        #endregion
    }
}
