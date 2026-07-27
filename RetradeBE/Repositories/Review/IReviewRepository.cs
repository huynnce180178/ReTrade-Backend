using RetradeBE.Models;

namespace RetradeBE.Repositories
{
    public interface IReviewRepository
    {
        IQueryable<Review> Query();
        Task<Review?> GetByBuyerOrderAsync(string buyerId, string orderId);
        Task<Review?> GetByIdForReportAsync(string reviewId);
        Task<Report?> GetReportByReporterAsync(string reviewId, string reporterId);
        Task AddAsync(Review review);
        Task AddReportAsync(Report report);
    }
}
