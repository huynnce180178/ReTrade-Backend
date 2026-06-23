using System;
using System.Collections.Generic;

namespace RetradeBE.Models;

public partial class Auction
{
    public string AuctionId { get; set; } = null!;

    public string? ProductId { get; set; }

    public string? SellerId { get; set; }

    public decimal? StartingPrice { get; set; }

    public decimal? CurrentPrice { get; set; }

    public decimal? MinIncrement { get; set; }

    public decimal? BuyNowPrice { get; set; }

    public DateTime? StartTime { get; set; }

    public DateTime? EndTime { get; set; }

    public string? Status { get; set; }

    public string? WinnerId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<AuctionDeposit> AuctionDeposit { get; set; } = new List<AuctionDeposit>();

    public virtual ICollection<Bid> Bid { get; set; } = new List<Bid>();

    public virtual ICollection<Order> Order { get; set; } = new List<Order>();

    public virtual Product? Product { get; set; }

    public virtual User? Seller { get; set; }

    public virtual User? Winner { get; set; }
}
