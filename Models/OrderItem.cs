using System;
using System.Collections.Generic;

namespace RetradeBE.Models;

public partial class OrderItem
{
    public string OrderItemId { get; set; } = null!;

    public string? OrderId { get; set; }

    public string? ProductId { get; set; }

    public int? Quantity { get; set; }

    public decimal? UnitPrice { get; set; }

    public decimal? TotalPrice { get; set; }

    public virtual Order? Order { get; set; }

    public virtual Product? Product { get; set; }
}
