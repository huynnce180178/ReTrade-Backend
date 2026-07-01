using Microsoft.EntityFrameworkCore;
using RetradeBE.Data;
using RetradeBE.Models;

namespace RetradeBE.Repositories
{
    public class ReportRepository : IReportRepository
    {
        private readonly AppDbContext _context;

        public ReportRepository(AppDbContext context)
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
                        .ThenInclude(product => product!.ProductImage);
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
                //.Include(review => review.Report)
                .FirstOrDefaultAsync(review => review.ReviewId == reviewId);
        }

        public async Task<Report?> GetReportByReporterAsync(string reviewId, string reporterId)
        {
            return await _context.Report
                .AsNoTracking()
                .FirstOrDefaultAsync(report => report.TargetId == reviewId && report.ReporterId == reporterId);
        }

        public async Task AddAsync(Review review)
        {
            await _context.Review.AddAsync(review);
            await _context.SaveChangesAsync();
        }

        public async Task AddReportAsync(Report report)
        {
            await _context.Report.AddAsync(report);
            await _context.SaveChangesAsync();
        }
    }
}

