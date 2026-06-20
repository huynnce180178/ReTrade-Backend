using Microsoft.EntityFrameworkCore;
using RetradeBE.Data;
using RetradeBE.Models;

namespace RetradeBE.Repositories
{
    public class ReviewRepository : IReviewRepository
    {
        private readonly AppDbContext _context;

        public ReviewRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Review?> GetByBuyerOrderAsync(string buyerId, string orderId)
        {
            return await _context.Review
                .AsNoTracking()
                .FirstOrDefaultAsync(review => review.ReviewerId == buyerId && review.OrderId == orderId);
        }

        public async Task AddAsync(Review review)
        {
            await _context.Review.AddAsync(review);
            await _context.SaveChangesAsync();
        }
    }
}
