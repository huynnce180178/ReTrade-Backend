using System;
using System.Collections.Generic;

namespace RetradeBE.Models;

public partial class Product
{
    public string ProductId { get; set; } = null!;

    public string? SellerId { get; set; }

    public string? CategoryId { get; set; }

    public string? Name { get; set; }

    public string? Description { get; set; }

    public string? Condition { get; set; }

    public decimal? Price { get; set; }

    public int? StockQuantity { get; set; }

    public int? WeightGram { get; set; }

    public int? LengthCm { get; set; }

    public int? WidthCm { get; set; }

    public int? HeightCm { get; set; }

    public string? Status { get; set; }

    public bool? IsDeleted { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Auction> Auction { get; set; } = new List<Auction>();

    public virtual Category? Category { get; set; }

    public virtual ICollection<ChatRoom> ChatRoom { get; set; } = new List<ChatRoom>();

    public virtual ICollection<Offer> Offer { get; set; } = new List<Offer>();

    public virtual ICollection<Order> Order { get; set; } = new List<Order>();

    public virtual ICollection<UserFavorite> UserFavorite { get; set; } = new List<UserFavorite>();

    public virtual ICollection<ProductAttribute> ProductAttribute { get; set; } = new List<ProductAttribute>();

    public virtual ICollection<ProductImage> ProductImage { get; set; } = new List<ProductImage>();

    public virtual User? Seller { get; set; }

    public virtual ICollection<WishlistItem> WishlistItem { get; set; } = new List<WishlistItem>();
}

