using RetradeBE.Models;

namespace RetradeBE.Repositories
{
    public interface IReviewRepository
    {
        IQueryable<Review> Query();
        Task<Review?> GetByBuyerOrderAsync(string buyerId, string orderId);
        Task<Review?> GetByIdForReportAsync(string reviewId);
        Task AddAsync(Review review);
        Task UpdateAsync(Review review);
    }
}
