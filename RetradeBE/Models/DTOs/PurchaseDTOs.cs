using System.ComponentModel.DataAnnotations;

namespace RetradeBE.Models.DTOs
{
    public class PurchaseListDto
    {
        public string OrderId { get; set; } = null!;
        public string? OrderCode { get; set; }
        public string? ProductId { get; set; }
        public string? ProductName { get; set; }
        public string? ProductImageUrl { get; set; }
        public string? SellerId { get; set; }
        public string? SellerName { get; set; }
        public string? SellerEmail { get; set; }
        public string? SellerPhone { get; set; }
        public int? Quantity { get; set; }
        public decimal? UnitPrice { get; set; }
        public decimal? TotalAmount { get; set; }
        public decimal? ShippingFee { get; set; }
        public decimal? DiscountAmount { get; set; }
        public decimal? FinalAmount { get; set; }
        public string? Status { get; set; }
        public string? TrackingCode { get; set; }
        public string? ShippingProvider { get; set; }
        public DateTime? ExpectedDeliveryTime { get; set; }
        public string? ReturnReason { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool HasReview { get; set; }
    }

    public class PurchaseDetailDto : PurchaseListDto
    {
        public string? BuyerId { get; set; }
        public string? BuyerName { get; set; }
        public string? BuyerEmail { get; set; }
        public string? BuyerPhone { get; set; }
        public string? AddressSnapshot { get; set; }
        public string? VoucherId { get; set; }
        public string? AuctionId { get; set; }
        public string? OfferId { get; set; }
        public List<PaymentSummaryDto> Payments { get; set; } = new();
    }

    public class ReturnPurchaseRequestDto
    {
        [StringLength(500, MinimumLength = 10)]
        public string Reason { get; set; } = null!;
    }
}
