using System;
using System.Collections.Generic;

namespace RetradeBE.Models;

public partial class Wishlist
{
    public string WishlistId { get; set; } = null!;

    public string? UserId { get; set; }

    public string? Status { get; set; }

    public bool? IsDeleted { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual User? User { get; set; }

    public virtual ICollection<WishlistItem> WishlistItems { get; set; } = new List<WishlistItem>();
}
