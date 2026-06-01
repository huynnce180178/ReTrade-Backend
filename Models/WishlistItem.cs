using System;
using System.Collections.Generic;

namespace RetradeBE.Models;

public partial class WishlistItem
{
    public string WishlistItemId { get; set; } = null!;

    public string? WishlistId { get; set; }

    public string? ProductId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Product? Product { get; set; }

    public virtual Wishlist? Wishlist { get; set; }
}
