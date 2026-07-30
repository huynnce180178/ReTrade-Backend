using System;

namespace RetradeBE.Models.DTOs
{
    public class RejectRefundRequestDto
    {
        public string Reason { get; set; } = string.Empty;
    }

    public class AdminRefundResponseDto
    {
        public string RefundRequestId { get; set; } = string.Empty;
        public string? UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public decimal? Amount { get; set; }
        public string? Note { get; set; }
        public string? Status { get; set; }
        public string? RejectReason { get; set; }
        public DateTime? RequestedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? BankName { get; set; }
        public string? BankAccountNumber { get; set; }
        public string? BankAccountHolder { get; set; }
    }
}
