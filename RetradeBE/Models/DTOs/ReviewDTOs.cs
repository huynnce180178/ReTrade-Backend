namespace RetradeBE.Models.DTOs
{
    public class ReviewCreateDto
    {
        public string OrderId { get; set; } = null!;
        public int Rating { get; set; }
        public string? Comment { get; set; }
    }

    public class ReviewQueryDto
    {
        public string? SellerId { get; set; }
        public int? Rating { get; set; }
        public string? Status { get; set; }
        public string? SearchTerm { get; set; }
        public string? SortBy { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 12;
    }

    public class ReportCreateDto
    {
        public string Reason { get; set; } = null!;
        public string? Description { get; set; }
    }

    public class ReportDto
    {
        public string ReportId { get; set; } = null!;
        public string TargetId { get; set; } = null!;
        public string TargetType { get; set; } = null!;
        public string ReporterId { get; set; } = null!;
        public string? ReporterName { get; set; }
        public string? Reason { get; set; }
        public string? Description { get; set; }
        public string? Status { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class ReviewSummaryDto
    {
        public int TotalReviews { get; set; }
        public double AverageRating { get; set; }
        public int ReportedReviews { get; set; }
        public Dictionary<int, int> RatingStats { get; set; } = new();
    }

    public class ReviewResponseDto
    {
        public string ReviewId { get; set; } = null!;
        public string TargetType { get; set; } = "Review";
        public string? ReviewerId { get; set; }
        public string? ReviewerName { get; set; }
        public string? ReviewerEmail { get; set; }
        public string? ReviewerAvatarUrl { get; set; }
        public string? SellerId { get; set; }
        public string? SellerName { get; set; }
        public string? OrderId { get; set; }
        public string? OrderCode { get; set; }
        public string? ProductId { get; set; }
        public string? ProductName { get; set; }
        public string? ProductImageUrl { get; set; }
        public int? Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int ReportCount { get; set; }
        public bool ReportedByCurrentUser { get; set; }
        public string? LatestReportStatus { get; set; }
        public string? LatestReportReason { get; set; }
        public DateTime? LatestReportCreatedAt { get; set; }
        public ReportDto? CurrentUserReport { get; set; }
        public List<ReportDto> Reports { get; set; } = new();
    }
}

