using System;
using System.Collections.Generic;

namespace RetradeBE.Models.DTOs
{
    public class AuctionCreateDto
    {
        public string ProductId { get; set; } = null!;
        public decimal StartingPrice { get; set; }
        public decimal MinIncrement { get; set; }
        public decimal? BuyNowPrice { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
    }

    public class AuctionUpdateDto
    {
        public decimal StartingPrice { get; set; }
        public decimal MinIncrement { get; set; }
        public decimal? BuyNowPrice { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
    }

    public class AuctionQueryDto
    {
        public string? SearchTerm { get; set; }
        public string? Status { get; set; }
        public string? SellerId { get; set; }
        public string? SortBy { get; set; }
        public bool IncludeEnded { get; set; } = false;
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 12;
    }

    public class AuctionListDto
    {
        public string AuctionId { get; set; } = null!;
        public string? ProductId { get; set; }
        public string? ProductName { get; set; }
        public string? ProductImageUrl { get; set; }
        public string? CategoryName { get; set; }
        public string? Condition { get; set; }
        public string? SellerId { get; set; }
        public string? SellerName { get; set; }
        public decimal? StartingPrice { get; set; }
        public decimal? CurrentPrice { get; set; }
        public decimal? HighestBid { get; set; }
        public decimal? MinIncrement { get; set; }
        public decimal? BuyNowPrice { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string? Status { get; set; }
        public int BidCount { get; set; }
        public DateTime? CreatedAt { get; set; }
    }

    public class AuctionDetailDto : AuctionListDto
    {
        public string? ProductDescription { get; set; }
        public int? StockQuantity { get; set; }
        public int? WeightGram { get; set; }
        public int? LengthCm { get; set; }
        public int? WidthCm { get; set; }
        public int? HeightCm { get; set; }
        public string? WinnerId { get; set; }
        public string? WinnerName { get; set; }
        public List<ProductImageDto> Images { get; set; } = new List<ProductImageDto>();
        public List<ProductAttributeValueDto> Attributes { get; set; } = new List<ProductAttributeValueDto>();
        public List<AuctionBidSummaryDto> RecentBids { get; set; } = new List<AuctionBidSummaryDto>();
    }

    public class AuctionDepositDto
    {
        public string? AuctionDepositId { get; set; }
        public string? AuctionId { get; set; }
        public string? UserId { get; set; }
        public decimal? DepositAmount { get; set; }
        public bool PolicyAccepted { get; set; }
        public string? Status { get; set; }
        public DateTime? CreatedAt { get; set; }
        public decimal MaxBidAmount { get; set; }
        public bool CanBid { get; set; }
    }

    public class AuctionDepositTransactionDto
    {
        public string AuctionDepositTransactionId { get; set; } = string.Empty;
        public string AuctionDepositId { get; set; } = string.Empty;
        public string AuctionId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string? UserName { get; set; }
        public string? ProductName { get; set; }
        public string? ProductImageUrl { get; set; }
        public string? PaymentId { get; set; }
        public string TransactionType { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? ProviderTransactionNo { get; set; }
        public string? Note { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }

    public class AuctionDepositPaymentRequestDto
    {
        public decimal DepositAmount { get; set; }
        public bool PolicyAccepted { get; set; }
        public string? BankCode { get; set; }
        public string? Locale { get; set; }
    }

    public class AuctionBidCreateDto
    {
        public decimal BidAmount { get; set; }
    }

    public class AuctionBidResultDto
    {
        public AuctionDetailDto Auction { get; set; } = new AuctionDetailDto();
        public bool AuctionEnded { get; set; }
        public string? OrderId { get; set; }
        public string? Message { get; set; }
    }

    public class AuctionBidSummaryDto
    {
        public string? BidId { get; set; }
        public string? UserId { get; set; }
        public string? BidderName { get; set; }
        public decimal? BidAmount { get; set; }
        public string? Status { get; set; }
        public DateTime? CreatedAt { get; set; }
    }

    public class UserBidHistoryDto
    {
        public string BidId { get; set; } = string.Empty;
        public decimal BidAmount { get; set; }
        public string BidStatus { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string AuctionId { get; set; } = string.Empty;
        public string AuctionStatus { get; set; } = string.Empty;
        public string ProductId { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string ProductImageUrl { get; set; } = string.Empty;
        public decimal CurrentPrice { get; set; }
        public DateTime EndTime { get; set; }
    }
}
