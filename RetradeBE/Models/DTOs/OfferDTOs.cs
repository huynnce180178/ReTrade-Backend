using System.ComponentModel.DataAnnotations;

namespace RetradeBE.Models.DTOs
{
    public class MakeOfferRequestDto
    {
        public string ProductId { get; set; } = null!;
        public decimal OfferPrice { get; set; }
        public string? Message { get; set; }
        /// <summary>Hours until this offer expires (default 48h)</summary>
        public int ExpiresInHours { get; set; } = 48;
    }
    public class CounterOfferDto
    {
        public string OfferId { get; set; } = null!;
        public decimal CounterPrice { get; set; }
    }

    public class RespondToOfferDto
    {
        [Required]
        public bool? Accept { get; set; }
    }

    public class OfferDto
    {
        public string OfferId { get; set; } = null!;
        public string? BuyerId { get; set; }
        public string? BuyerName { get; set; }
        public string? ProductId { get; set; }
        public string? ProductName { get; set; }
        public string? ProductImageUrl { get; set; }
        public decimal? OriginalPrice { get; set; }
        public decimal? OfferPrice { get; set; }
        public string? Message { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public string? Status { get; set; }
        public DateTime? CreatedAt { get; set; }
    }

    public class OfferCheckoutRequestDto
    {
        public string OfferId { get; set; } = null!;
        public string AddressId { get; set; } = null!;
        public string? PaymentMethod { get; set; }
    }
}
