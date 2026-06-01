using System;
using System.Collections.Generic;

namespace RetradeBE.Models;

public partial class Order
{
    public string OrderId { get; set; } = null!;

    public string? OrderCode { get; set; }

    public string? UserId { get; set; }

    public string? AddressSnapshot { get; set; }

    public decimal? TotalAmount { get; set; }

    public decimal? ShippingFee { get; set; }

    public decimal? DiscountAmount { get; set; }

    public decimal? FinalAmount { get; set; }

    public DateTime? ExpectedDeliveryTime { get; set; }

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();

    public virtual User? User { get; set; }
}
