using System;
using System.Collections.Generic;

namespace RetradeBE.Models;

public partial class AuctionDeposit
{
    public string AuctionDepositId { get; set; } = null!;

    public string? AuctionId { get; set; }

    public string? UserId { get; set; }

    public decimal? DepositAmount { get; set; }

    public bool? PolicyAccepted { get; set; }

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<AuctionDepositTransaction> AuctionDepositTransaction { get; set; } = new List<AuctionDepositTransaction>();

    public virtual Auction? Auction { get; set; }

    public virtual User? User { get; set; }
}
