using RetradeBE.Models;

namespace RetradeBE.Repositories
{
    public interface IReviewRepository
    {
        IQueryable<Review> Query();
        Task<Review?> GetByBuyerOrderAsync(string buyerId, string orderId);
        Task<Review?> GetByIdForReportAsync(string reviewId);
<<<<<<< HEAD
        Task AddAsync(Review review);
=======
        Task<Report?> GetReportByReporterAsync(string reviewId, string reporterId);
        Task AddAsync(Review review);
        Task AddReportAsync(Report report);
>>>>>>> df66243 (feature report)
    }
}
