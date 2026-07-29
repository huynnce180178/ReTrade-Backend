using RetradeBE.Models.DTOs;

namespace RetradeBE.Services
{
    public interface IReportService
    {
        Task<ReportDto> ReportReviewAsync(string accountId, string reviewId, ReportCreateDto request);
        Task<ReportDto> ReportBuyerAsync(string accountId, string orderId, ReportCreateDto request);
        Task<ReportDto> ReportSellerAsync(string accountId, string orderId, ReportCreateDto request);
        Task<IQueryable<ReportListDto>> GetAllAsync();
        Task<ReportDetailDto?> GetByIdAsync(string reportId);
        Task<ReportDto?> UpdateStatusAsync(string reportId, ReportStatusUpdateDto request);
        Task<IReadOnlyList<FlaggedUserDto>> GetFlaggedUsersAsync();
        Task<ReportHistoryDto> GetHistoryAsync(string accountId);
    }
}
