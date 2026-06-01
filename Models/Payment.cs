using System;
using System.Collections.Generic;

namespace RetradeBE.Models;

public partial class Payment
{
    public string PaymentId { get; set; } = null!;

    public string? OrderId { get; set; }

    public string? UserId { get; set; }

    public decimal? Amount { get; set; }

    public string? PaymentMethod { get; set; }

    public string? ProviderTransactionId { get; set; }

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Order? Order { get; set; }

    public virtual User? User { get; set; }
}
