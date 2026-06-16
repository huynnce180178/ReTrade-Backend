using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace RetradeBE.Models.DTOs
{
    public class AddToWishlistDto
    {
        [Required(ErrorMessage = "ProductId is required.")]
        [StringLength(50, MinimumLength = 1, ErrorMessage = "ProductId must be between 1 and 50 characters.")]
        public string ProductId { get; set; } = null!;
    }

    public class WishlistItemDto
    {
        public string WishlistItemId { get; set; } = null!;
        public string? ProductId { get; set; }
        public string? ProductName { get; set; }
        public decimal? Price { get; set; }
        public string? Condition { get; set; }
        public string? Status { get; set; }
        public string? MainImageUrl { get; set; }
        public string? SellerName { get; set; }
        public string? SellerId { get; set; }
        public DateTime? AddedAt { get; set; }
    }

    public class WishlistDetailDto
    {
        public string WishlistId { get; set; } = null!;
        public string? UserId { get; set; }
        public string? Status { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<WishlistItemDto> Items { get; set; } = new List<WishlistItemDto>();
    }
}
