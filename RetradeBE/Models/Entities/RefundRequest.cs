using System;
using System.Collections.Generic;

namespace RetradeBE.Models;

public partial class RefundRequest
{
    public string RefundRequestId { get; set; } = null!;

    public string? OrderId { get; set; }

    public string? UserId { get; set; }

    public decimal? Amount { get; set; }

    public string? BankName { get; set; }

    public string? BankAccountNumber { get; set; }

    public string? BankAccountHolder { get; set; }

    public string? Note { get; set; }

    public string? Status { get; set; }

    public DateTime? RequestedAt { get; set; }

    public string? RejectReason { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Order? Order { get; set; }

    public virtual User? User { get; set; }
}
