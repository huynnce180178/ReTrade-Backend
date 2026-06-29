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

        public IQueryable<Review> Query()
        {
            return _context.Review
                .AsNoTracking()
                .Include(review => review.Reviewer)
                .Include(review => review.Seller)
                .Include(review => review.Order)
                    .ThenInclude(order => order!.User)
                .Include(review => review.Order)
                    .ThenInclude(order => order!.Seller)
                .Include(review => review.Order)
                    .ThenInclude(order => order!.Product)
                        .ThenInclude(product => product!.ProductImage)
                            .ThenInclude(productImage => productImage.Image)
                .Include(review => review.ReviewReport)
                    .ThenInclude(report => report.Reporter)
                .Include(review => review.ReviewReport)
                    .ThenInclude(report => report.ReviewedByNavigation);
        }

        public async Task<Review?> GetByBuyerOrderAsync(string buyerId, string orderId)
        {
            return await _context.Review
                .AsNoTracking()
                .FirstOrDefaultAsync(review => review.ReviewerId == buyerId && review.OrderId == orderId);
        }

        public async Task<Review?> GetByIdForReportAsync(string reviewId)
        {
            return await _context.Review
                .Include(review => review.Order)
                .Include(review => review.ReviewReport)
                .FirstOrDefaultAsync(review => review.ReviewId == reviewId);
        }

        public async Task<ReviewReport?> GetReportByReviewReporterAsync(string reviewId, string reporterId)
        {
            return await _context.ReviewReport
                .AsNoTracking()
                .FirstOrDefaultAsync(report => report.ReviewId == reviewId && report.ReporterId == reporterId);
        }

        public async Task AddAsync(Review review)
        {
            await _context.Review.AddAsync(review);
            await _context.SaveChangesAsync();
        }

        public async Task AddReportAsync(ReviewReport report)
        {
            await _context.ReviewReport.AddAsync(report);
            await _context.SaveChangesAsync();
        }
    }
}
