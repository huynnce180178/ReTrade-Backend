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
    public class RefundRejectRefundTests
    {
        private readonly Mock<IRefundRepository> _refundRepository;
        private readonly Mock<INotificationService> _notificationService;
        private readonly RefundService _service;

        public RefundRejectRefundTests()
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
        public async Task RejectRefundAsync_ShouldMarkRefundAsRejectedAndSendNotification_WhenRequestIsPending()
        {
            // Arrange
            var refundId = "ref_100";
            var userId = "user_200";

            var refund = new RefundRequest
            {
                RefundRequestId = refundId,
                UserId = userId,
                Amount = 250000m,
                Status = "Pending"
            };

            var dto = new RejectRefundRequestDto
            {
                Reason = "Receipt is invalid or illegible."
            };

            _refundRepository.Setup(r => r.GetByIdAsync(refundId)).ReturnsAsync(refund);
            _refundRepository.Setup(r => r.UpdateAsync(It.IsAny<RefundRequest>())).Returns(Task.CompletedTask);
            _notificationService.Setup(n => n.CreateAndSendAsync(It.IsAny<CreateNotificationDto>())).ReturnsAsync(new NotificationDto());

            // Act
            var result = await _service.RejectRefundAsync(refundId, dto);

            // Assert
            result.Success.Should().BeTrue();
            result.Message.Should().Be("Refund request rejected successfully.");
            refund.Status.Should().Be("Rejected");
            refund.RejectReason.Should().Be("Receipt is invalid or illegible.");

            _refundRepository.Verify(r => r.GetByIdAsync(refundId), Times.Once);
            _refundRepository.Verify(r => r.UpdateAsync(It.Is<RefundRequest>(r => r.RefundRequestId == refundId && r.Status == "Rejected" && r.RejectReason == "Receipt is invalid or illegible.")), Times.Once);
            _notificationService.Verify(n => n.CreateAndSendAsync(It.Is<CreateNotificationDto>(nDto => nDto.UserId == userId && nDto.ReferenceId == refundId)), Times.Once);
        }
        #endregion

        #region Abnormal Tests (A)
        [Fact]
        public async Task RejectRefundAsync_ShouldReturnFailure_WhenRefundNotFoundOrStatusNotPending()
        {
            var dto = new RejectRefundRequestDto { Reason = "Duplicate request" };

            // Scenario 1: Refund request not found
            _refundRepository.Setup(r => r.GetByIdAsync("invalid_ref")).ReturnsAsync((RefundRequest?)null);
            var resultNotFound = await _service.RejectRefundAsync("invalid_ref", dto);

            resultNotFound.Success.Should().BeFalse();
            resultNotFound.Message.Should().Be("Refund request not found.");

            // Scenario 2: Refund request status is not Pending (e.g. Processed or Rejected)
            var processedRefund = new RefundRequest { RefundRequestId = "ref_processed", Status = "Processed" };
            _refundRepository.Setup(r => r.GetByIdAsync("ref_processed")).ReturnsAsync(processedRefund);
            var resultNotPending = await _service.RejectRefundAsync("ref_processed", dto);

            resultNotPending.Success.Should().BeFalse();
            resultNotPending.Message.Should().Be("Only pending refund requests can be rejected.");

            _refundRepository.Verify(r => r.UpdateAsync(It.IsAny<RefundRequest>()), Times.Never);
        }
        #endregion

        #region Boundary Tests (B)
        [Fact]
        public async Task RejectRefundAsync_ShouldCompleteSuccessfully_EvenIfSendNotificationFails()
        {
            // Arrange
            var refundId = "ref_300";
            var userId = "user_300";

            var refund = new RefundRequest
            {
                RefundRequestId = refundId,
                UserId = userId,
                Amount = 100000m,
                Status = "Pending"
            };

            var dto = new RejectRefundRequestDto { Reason = "Policy violation" };

            _refundRepository.Setup(r => r.GetByIdAsync(refundId)).ReturnsAsync(refund);
            _refundRepository.Setup(r => r.UpdateAsync(It.IsAny<RefundRequest>())).Returns(Task.CompletedTask);
            _notificationService.Setup(n => n.CreateAndSendAsync(It.IsAny<CreateNotificationDto>())).ThrowsAsync(new Exception("Notification service failed"));

            // Act
            var result = await _service.RejectRefundAsync(refundId, dto);

            // Assert
            result.Success.Should().BeTrue();
            result.Message.Should().Be("Refund request rejected successfully.");
            refund.Status.Should().Be("Rejected");
            refund.RejectReason.Should().Be("Policy violation");

            _refundRepository.Verify(r => r.UpdateAsync(It.IsAny<RefundRequest>()), Times.Once);
        }
        #endregion
    }
}
