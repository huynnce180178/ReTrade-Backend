using System;
using System.Collections.Generic;

namespace RetradeBE.Models;

public partial class Bid
{
    public string BidId { get; set; } = null!;

    public string? AuctionId { get; set; }

    public string? UserId { get; set; }

    public decimal? BidAmount { get; set; }

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Auction? Auction { get; set; }

    public virtual User? User { get; set; }
}
