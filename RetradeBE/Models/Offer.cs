using System;
using System.Collections.Generic;

namespace RetradeBE.Models;

public partial class Offer
{
    public string OfferId { get; set; } = null!;

    public string? BuyerId { get; set; }

    public string? ProductId { get; set; }

    public decimal? OfferPrice { get; set; }

    public string? Message { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual User? Buyer { get; set; }

    public virtual ICollection<Order> Order { get; set; } = new List<Order>();

    public virtual Product? Product { get; set; }
}
