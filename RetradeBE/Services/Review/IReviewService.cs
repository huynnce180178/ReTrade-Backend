using RetradeBE.Models.DTOs;
using RetradeBE.Models;

namespace RetradeBE.Services
{
    public interface IReviewService
    {
        Task<Review?> GetByIdForReportAsync(string reviewId);
        Task HideForReportAsync(string reviewId, DateTime updatedAt);
        Task<ReviewResponseDto?> GetByBuyerOrderAsync(string buyerId, string orderId);
        Task<PagedResultDto<ReviewResponseDto>> GetSellerReviewsAsync(string accountId, ReviewQueryDto query);
        Task<ReviewSummaryDto> GetSellerReviewSummaryAsync(string accountId);
        Task<PagedResultDto<ReviewResponseDto>> GetAdminReviewsAsync(ReviewQueryDto query);
        Task<ReviewSummaryDto> GetAdminReviewSummaryAsync(ReviewQueryDto query);
        Task<ReviewResponseDto?> CreateAsync(string buyerId, ReviewCreateDto request);
        Task<ReportDto> ReportReviewAsync(string accountId, string reviewId, ReportCreateDto request, bool isAdmin);
    }
}

