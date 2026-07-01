using System;
using System.Collections.Generic;

namespace RetradeBE.Models;

public partial class Order
{
    public string OrderId { get; set; } = null!;

    public string? OrderCode { get; set; }

    public string? BuyerId { get; set; }

    public string? SellerId { get; set; }

    public string? ProductId { get; set; }

    public int? Quantity { get; set; }

    public decimal? UnitPrice { get; set; }

    public string? VoucherId { get; set; }

    public string? AddressSnapshot { get; set; }

    public string? AuctionId { get; set; }

    public string? OfferId { get; set; }

    public string? TrackingCode { get; set; }

    public string? ShippingProvider { get; set; }

    public decimal? TotalAmount { get; set; }

    public decimal? ShippingFee { get; set; }

    public decimal? DiscountAmount { get; set; }

    public decimal? FinalAmount { get; set; }

    public DateTime? ExpectedDeliveryTime { get; set; }

    public string? ReturnReason { get; set; }

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Auction? Auction { get; set; }

    public virtual Offer? Offer { get; set; }

    public virtual ICollection<Payment> Payment { get; set; } = new List<Payment>();

    public virtual Product? Product { get; set; }

    public virtual ICollection<RefundRequest> RefundRequest { get; set; } = new List<RefundRequest>();

    public virtual ICollection<Review> Review { get; set; } = new List<Review>();

    public virtual User? Seller { get; set; }

    public virtual User? Buyer { get; set; }

    public virtual Voucher? Voucher { get; set; }
}


