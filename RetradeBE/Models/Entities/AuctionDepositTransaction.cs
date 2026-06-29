using System;
using System.Collections.Generic;

namespace RetradeBE.Models;

public partial class AuctionDepositTransaction
{
    public string AuctionDepositTransactionId { get; set; } = null!;

    public string? AuctionDepositId { get; set; }

    public string? AuctionId { get; set; }

    public string? UserId { get; set; }

    public string? PaymentId { get; set; }

    public string? TransactionType { get; set; }

    public decimal? Amount { get; set; }

    public string? Status { get; set; }

    public string? ProviderTransactionNo { get; set; }

    public string? Note { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public virtual Auction? Auction { get; set; }

    public virtual AuctionDeposit? AuctionDeposit { get; set; }

    public virtual Payment? Payment { get; set; }

    public virtual User? User { get; set; }
}
