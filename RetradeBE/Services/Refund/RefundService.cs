using RetradeBE.Models.DTOs;
using RetradeBE.Models.Enums;
using RetradeBE.Repositories.Refund;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RetradeBE.Services.Refund
{
    public class RefundService : IRefundService
    {
        private readonly IRefundRepository _refundRepository;
        private readonly INotificationService _notificationService;

        public RefundService(IRefundRepository refundRepository, INotificationService notificationService)
        {
            _refundRepository = refundRepository;
            _notificationService = notificationService;
        }

        public async Task<IEnumerable<AdminRefundResponseDto>> GetAllRefundsAsync()
        {
            var refunds = await _refundRepository.GetAllRefundsWithUserAsync();

            return refunds.Select(r => new AdminRefundResponseDto
            {
                RefundRequestId = r.RefundRequestId,
                UserId = r.UserId,
                UserName = r.User != null ? (r.User.FirstName + " " + r.User.LastName).Trim() : string.Empty,
                UserEmail = r.User != null ? r.User.Email : string.Empty,
                Amount = r.Amount,
                Note = r.Note,
                Status = r.Status,
                RejectReason = r.RejectReason,
                RequestedAt = r.RequestedAt,
                UpdatedAt = r.UpdatedAt,
                BankName = r.BankName,
                BankAccountNumber = r.BankAccountNumber,
                BankAccountHolder = r.BankAccountHolder
            });
        }

        public async Task<(bool Success, string Message)> ApproveRefundAsync(string id)
        {
            var refund = await _refundRepository.GetByIdAsync(id);
            if (refund == null) return (false, "Refund request not found.");

            if (refund.Status != "Pending")
                return (false, "Only pending refund requests can be processed.");

            refund.Status = "Processed";
            refund.UpdatedAt = DateTime.UtcNow.AddHours(7);

            await _refundRepository.UpdateAsync(refund);

            try
            {
                await _notificationService.CreateAndSendAsync(new CreateNotificationDto
                {
                    UserId = refund.UserId ?? string.Empty,
                    Title = "Refund Processed",
                    Message = $"Your refund request for {refund.Amount:N0} VND has been processed and transferred to your bank account.",
                    Type = nameof(NotificationTypeEnum.Payment),
                    ReferenceId = refund.RefundRequestId
                });
            }
            catch { }

            return (true, "Refund marked as processed.");
        }

        public async Task<(bool Success, string Message)> RejectRefundAsync(string id, RejectRefundRequestDto dto)
        {
            var refund = await _refundRepository.GetByIdAsync(id);
            if (refund == null) return (false, "Refund request not found.");

            if (refund.Status != "Pending")
                return (false, "Only pending refund requests can be rejected.");

            refund.Status = "Rejected";
            refund.RejectReason = dto.Reason;
            refund.UpdatedAt = DateTime.UtcNow.AddHours(7);

            await _refundRepository.UpdateAsync(refund);

            try
            {
                await _notificationService.CreateAndSendAsync(new CreateNotificationDto
                {
                    UserId = refund.UserId ?? string.Empty,
                    Title = "Refund Rejected",
                    Message = $"Your refund request for {refund.Amount:N0} VND has been rejected. Reason: {dto.Reason}",
                    Type = nameof(NotificationTypeEnum.Payment),
                    ReferenceId = refund.RefundRequestId
                });
            }
            catch { }

            return (true, "Refund request rejected successfully.");
        }
    }
}
