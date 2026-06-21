namespace RetradeBE.Models.DTOs
{
    public class OrderSearchQueryDto
    {
        public string? Status { get; set; }
        public string? SearchTerm { get; set; }
        public decimal? MinTotal { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? SortBy { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 12;
    }

    public class OrderStatusUpdateDto
    {
        public string Status { get; set; } = null!;
        public string? TrackingCode { get; set; }
        public string? ShippingProvider { get; set; }
        public DateTime? ExpectedDeliveryTime { get; set; }
    }

    public class OrderListDto
    {
        public string OrderId { get; set; } = null!;
        public string? OrderCode { get; set; }
        public string? ProductId { get; set; }
        public string? ProductName { get; set; }
        public string? ProductImageUrl { get; set; }
        public string? BuyerId { get; set; }
        public string? BuyerName { get; set; }
        public string? BuyerEmail { get; set; }
        public string? BuyerPhone { get; set; }
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
    }

    public class PaymentSummaryDto
    {
        public string PaymentId { get; set; } = null!;
        public decimal? Amount { get; set; }
        public string? PaymentMethod { get; set; }
        public string? ProviderTransactionId { get; set; }
        public string? Status { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class OrderDetailDto : OrderListDto
    {
        public string? AddressSnapshot { get; set; }
        public string? VoucherId { get; set; }
        public string? AuctionId { get; set; }
        public string? OfferId { get; set; }
        public List<PaymentSummaryDto> Payments { get; set; } = new();
    }

    public class SellerSalesStatisticsDto
    {
        public int PeriodDays { get; set; }
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public int TotalOrders { get; set; }
        public int AwaitingPaymentOrders { get; set; }
        public int PendingOrders { get; set; }
        public int ConfirmedOrders { get; set; }
        public int ShippingOrders { get; set; }
        public int DeliveredOrders { get; set; }
        public int CompletedOrders { get; set; }
        public int ReturnRequestedOrders { get; set; }
        public int ReturnRejectedOrders { get; set; }
        public int DeliveryFailedOrders { get; set; }
        public int ReturnedOrders { get; set; }
        public int CancelledOrders { get; set; }
        public int SoldItems { get; set; }
        public decimal GrossSales { get; set; }
        public decimal ShippingCollected { get; set; }
        public decimal DiscountGiven { get; set; }
        public decimal NetSales { get; set; }
        public List<SellerSalesTrendPointDto> RevenueTrend { get; set; } = new();
    }

    public class SellerSalesTrendPointDto
    {
        public string Label { get; set; } = null!;
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int OrderCount { get; set; }
        public decimal Revenue { get; set; }
    }
}
