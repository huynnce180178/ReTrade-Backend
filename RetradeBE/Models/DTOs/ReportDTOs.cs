namespace RetradeBE.Models.DTOs
{
    public class ReportCreateDto
    {
        public string Reason { get; set; } = null!;
        public string? Description { get; set; }
    }

    public class ReportDto
    {
        public string ReportId { get; set; } = null!;
        public string TargetType { get; set; } = null!;
        public string TargetId { get; set; } = null!;
        public string ReporterId { get; set; } = null!;
        public string? ReporterName { get; set; }
        public string? Reason { get; set; }
        public string? Description { get; set; }
        public string? Status { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class ReportListDto
    {
        public string ReportId { get; set; } = null!;
        public string ReporterId { get; set; } = null!;
        public string? ReporterName { get; set; }
        public string TargetType { get; set; } = null!;
        public string? Reason { get; set; }
        public string? Status { get; set; }
        public DateTime? CreatedAt { get; set; }
    }

    public class ReportStatusUpdateDto
    {
        public string Status { get; set; } = null!;
    }

    public class ReportDetailDto
    {
        public string ReportId { get; set; } = null!;
        public string ReporterId { get; set; } = null!;
        public string? ReporterName { get; set; }
        public string TargetType { get; set; } = null!;
        public string TargetId { get; set; } = null!;
        public string? Reason { get; set; }
        public string? Description { get; set; }
        public string? Status { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public DateTime? CreatedAt { get; set; }

        public ReportReviewDetailDto? Review { get; set; }
        public ReportOrderDetailDto? Order { get; set; }
        public ReportUserDetailDto? Buyer { get; set; }
        public ReportUserDetailDto? Seller { get; set; }
        public ReportProductDetailDto? Product { get; set; }
    }

    public class ReportProductDetailDto
    {
        public string ProductId { get; set; } = null!;
        public string? Name { get; set; }
        public decimal? Price { get; set; }
        public string? Status { get; set; }
        public string? SellerId { get; set; }
        public string? SellerName { get; set; }
        public string? MainImageUrl { get; set; }
        public DateTime? CreatedAt { get; set; }
    }

    public class ReportReviewDetailDto
    {
        public string ReviewId { get; set; } = null!;
        public string? ReviewerId { get; set; }
        public string? SellerId { get; set; }
        public string? OrderId { get; set; }
        public int? Rating { get; set; }
        public string? Comment { get; set; }
        public bool? IsDeleted { get; set; }
        public DateTime? CreatedAt { get; set; }
    }

    public class ReportOrderDetailDto
    {
        public string OrderId { get; set; } = null!;
        public string? OrderCode { get; set; }
        public string? Status { get; set; }
        public string? BuyerId { get; set; }
        public string? SellerId { get; set; }
        public decimal? FinalAmount { get; set; }
        public DateTime? CreatedAt { get; set; }
    }

    public class ReportUserDetailDto
    {
        public string UserId { get; set; } = null!;
        public string? UserName { get; set; }
        public string? AvatarUrl { get; set; }
        public string? Email { get; set; }
        public int? FlagCount { get; set; }
        public bool? IsDeleted { get; set; }
        public string? Status { get; set; }
    }

    public class FlaggedUserDto
    {
        public string UserId { get; set; } = null!;
        public string? UserName { get; set; }
        public string? AvatarUrl { get; set; }
        public int? FlagCount { get; set; }
        public string? Status { get; set; }
        public List<ReportListDto> Reports { get; set; } = new();
    }

    public class ReportHistoryDto
    {
        public List<ReportListDto> ReportsSubmitted { get; set; } = new();
        public List<ReportListDto> ReportsReceived { get; set; } = new();
    }
}
