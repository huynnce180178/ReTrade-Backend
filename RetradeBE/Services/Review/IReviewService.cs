using RetradeBE.Models.DTOs;
using RetradeBE.Models;

namespace RetradeBE.Services
{
    public interface IReviewService
    {
        Task<Review?> GetByIdForReportAsync(string reviewId);
        Task HideForReportAsync(string reviewId, DateTime updatedAt);
        Task<ReviewResponseDto?> GetByBuyerOrderAsync(string accountId, string buyerId, string orderId, bool isAdmin);
        Task<PagedResultDto<ReviewResponseDto>> GetPublicSellerReviewsAsync(string sellerId, ReviewQueryDto query);
        Task<ReviewSummaryDto> GetPublicSellerReviewSummaryAsync(string sellerId);
        Task<PagedResultDto<ReviewResponseDto>> GetSellerReviewsAsync(string accountId, ReviewQueryDto query);
        Task<ReviewSummaryDto> GetSellerReviewSummaryAsync(string accountId);
        Task<PagedResultDto<ReviewResponseDto>> GetAdminReviewsAsync(ReviewQueryDto query);
        Task<ReviewSummaryDto> GetAdminReviewSummaryAsync(ReviewQueryDto query);
        Task<ReviewResponseDto?> CreateAsync(string accountId, string buyerId, ReviewCreateDto request);
        Task<ReportDto> ReportReviewAsync(string accountId, string reviewId, ReportCreateDto request, bool isAdmin);
    }
}

