using RetradeBE.Models.DTOs;

namespace RetradeBE.Services
{
    public interface IReviewService
    {
        Task<ReviewResponseDto?> GetByBuyerOrderAsync(string buyerId, string orderId);
        Task<PagedResultDto<ReviewResponseDto>> GetSellerReviewsAsync(string accountId, ReviewQueryDto query);
        Task<ReviewSummaryDto> GetSellerReviewSummaryAsync(string accountId);
        Task<PagedResultDto<ReviewResponseDto>> GetAdminReviewsAsync(ReviewQueryDto query);
        Task<ReviewSummaryDto> GetAdminReviewSummaryAsync(ReviewQueryDto query);
        Task<ReviewResponseDto?> CreateAsync(string buyerId, ReviewCreateDto request);
        Task<ReportDto> ReportReviewAsync(string accountId, string reviewId, ReportCreateDto request, bool isAdmin);
    }
}

