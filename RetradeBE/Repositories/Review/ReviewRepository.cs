using Microsoft.EntityFrameworkCore;
using RetradeBE.Data;
using RetradeBE.Models;

namespace RetradeBE.Repositories
{
    public class ReviewRepository : IReviewRepository
    {
        private readonly AppDbContext _context;

<<<<<<< HEAD
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
                    .ThenInclude(order => order!.Buyer)
                .Include(review => review.Order)
                    .ThenInclude(order => order!.Seller)
                .Include(review => review.Order)
                    .ThenInclude(order => order!.Product)
                        .ThenInclude(product => product!.ProductImage)
                            .ThenInclude(productImage => productImage.Image);
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
                .FirstOrDefaultAsync(review => review.ReviewId == reviewId);
        }
=======
        public ReviewRepository(AppDbContext context) => _context = context;

        public IQueryable<Review> Query() => _context.Review
            .AsNoTracking()
            .Include(review => review.Reviewer)
            .Include(review => review.Seller)
            .Include(review => review.Order).ThenInclude(order => order!.Buyer)
            .Include(review => review.Order).ThenInclude(order => order!.Seller)
            .Include(review => review.Order).ThenInclude(order => order!.Product).ThenInclude(product => product!.ProductImage);

        public Task<Review?> GetByBuyerOrderAsync(string buyerId, string orderId) => _context.Review
            .AsNoTracking()
            .FirstOrDefaultAsync(review => review.ReviewerId == buyerId && review.OrderId == orderId);

        public Task<Review?> GetByIdForReportAsync(string reviewId) => _context.Review
            .Include(review => review.Order)
            .FirstOrDefaultAsync(review => review.ReviewId == reviewId);

        public Task<Report?> GetReportByReporterAsync(string reviewId, string reporterId) => _context.Report
            .AsNoTracking()
            .FirstOrDefaultAsync(report => report.TargetId == reviewId && report.ReporterId == reporterId);
>>>>>>> df66243 (feature report)

        public async Task AddAsync(Review review)
        {
            await _context.Review.AddAsync(review);
            await _context.SaveChangesAsync();
        }
<<<<<<< HEAD
=======

        public async Task UpdateAsync(Review review)
        {
            _context.Review.Update(review);
            await _context.SaveChangesAsync();
        }

        public async Task AddReportAsync(Report report)
        {
            await _context.Report.AddAsync(report);
            await _context.SaveChangesAsync();
        }
>>>>>>> df66243 (feature report)
    }
}
