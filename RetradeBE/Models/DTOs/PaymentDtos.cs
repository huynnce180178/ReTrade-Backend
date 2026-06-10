using System.ComponentModel.DataAnnotations;

namespace RetradeBE.Models.DTOs;

public class CreateVnPayPaymentRequestDto
{
    public string? OrderId { get; set; }

    /// <summary>
    /// ServiceId của gói subscription (nếu thanh toán nâng cấp gói, không cùng lúc với OrderId)
    /// </summary>
    public string? ServiceId { get; set; }

    [Required]
    [Range(1000, double.MaxValue)]
    public decimal Amount { get; set; }

    [Required]
    [MaxLength(255)]
    public string OrderDescription { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? BankCode { get; set; }

    [MaxLength(10)]
    public string? Locale { get; set; }
}

public class CreateVnPayPaymentResponseDto
{
    public string PaymentId { get; set; } = string.Empty;

    public string PaymentUrl { get; set; } = string.Empty;
}

public class VnPayReturnResponseDto
{
    public bool IsSuccess { get; set; }

    public string PaymentId { get; set; } = string.Empty;

    public string? OrderId { get; set; }

    public string Status { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public string? TransactionNo { get; set; }

    public string? TransactionStatus { get; set; }

    public string? ResponseCode { get; set; }

    public decimal Amount { get; set; }
}
