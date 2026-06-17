using RetradeBE.Models;

namespace RetradeBE.Repositories
{
    public interface IReviewRepository
    {
        Task<Review?> GetByBuyerOrderAsync(string buyerId, string orderId);
        Task AddAsync(Review review);
    }
}
