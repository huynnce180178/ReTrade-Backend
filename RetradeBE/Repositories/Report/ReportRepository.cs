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

        public IQueryable<Report> Query()
        {
            return _context.Report
                .AsNoTracking()
                .Include(report => report.Reporter)
                .OrderByDescending(report => report.CreatedAt);
        }

        public async Task<Report?> GetReportByReporterAsync(string reviewId, string reporterId)
        {
            return await _context.Report
                .AsNoTracking()
                .FirstOrDefaultAsync(report =>
                    report.TargetType.ToLower() == "review" &&
                    report.TargetId == reviewId &&
                    report.ReporterId == reporterId);
        }

        public async Task<Report?> GetByIdAsync(string reportId)
        {
            return await _context.Report
                .AsNoTracking()
                .Include(report => report.Reporter)
                .FirstOrDefaultAsync(report => report.ReportId == reportId);
        }

        public async Task<Report?> GetByTargetAndReporterAsync(string targetId, string reporterId, string targetType)
        {
            return await _context.Report
                .AsNoTracking()
                .FirstOrDefaultAsync(report =>
                    report.TargetId == targetId &&
                    report.ReporterId == reporterId &&
                    report.TargetType.ToLower() == targetType.ToLower());
        }
        public async Task<List<Report>> GetReportsByReporterAsync(string reporterId)
        {
            return await _context.Report
                .AsNoTracking()
                .Include(report => report.Reporter)
                .Where(report => report.ReporterId == reporterId)
                .OrderByDescending(report => report.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Report>> GetReportsReceivedByUserAsync(string userId)
        {
            var reviewReports = await _context.Report
                .AsNoTracking()
                .Where(report => report.TargetType.ToLower() == "review")
                .Join(_context.Review.AsNoTracking(),
                    report => report.TargetId,
                    review => review.ReviewId,
                    (report, review) => new { Report = report, Review = review })
                .Where(x => x.Review.ReviewerId == userId) // Chỉ hiển thị cho người viết đánh giá bị báo cáo
                .Select(x => x.Report)
                .Include(report => report.Reporter)
                .OrderByDescending(report => report.CreatedAt)
                .ToListAsync();

            var orderReports = await _context.Report
                .AsNoTracking()
                .Where(report => report.TargetType.ToLower() == "buyer" || report.TargetType.ToLower() == "seller")
                .Join(_context.Order.AsNoTracking(),
                    report => report.TargetId,
                    order => order.OrderId,
                    (report, order) => new { Report = report, Order = order })
                .Where(x => (x.Report.TargetType.ToLower() == "buyer" && x.Order.BuyerId == userId) 
                         || (x.Report.TargetType.ToLower() == "seller" && x.Order.SellerId == userId)) // Nhận báo cáo đúng đối tượng bị tố cáo
                .Select(x => x.Report)
                .Include(report => report.Reporter)
                .OrderByDescending(report => report.CreatedAt)
                .ToListAsync();

            return reviewReports
                .Concat(orderReports)
                .OrderByDescending(report => report.CreatedAt)
                .ToList();
        }

        public async Task<List<Report>> GetReportsForUserAsync(string userId)
        {
            return await _context.Report
                .AsNoTracking()
                .Include(report => report.Reporter)
                .Where(report => report.TargetType.ToLower() == "buyer" || report.TargetType.ToLower() == "seller")
                .Join(_context.Order.AsNoTracking(),
                    report => report.TargetId,
                    order => order.OrderId,
                    (report, order) => new { Report = report, Order = order })
                .Where(x => x.Order.BuyerId == userId || x.Order.SellerId == userId)
                .Select(x => x.Report)
                .OrderByDescending(report => report.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> ExistsAsync(string targetId, string reporterId, string targetType)
        {
            return await _context.Report
                .AsNoTracking()
                .AnyAsync(report =>
                    report.TargetId == targetId &&
                    report.ReporterId == reporterId &&
                    report.TargetType.ToLower() == targetType.ToLower());
        }

        public async Task AddAsync(Report report)
        {
            await _context.Report.AddAsync(report);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Report report)
        {
            _context.Report.Update(report);
            await _context.SaveChangesAsync();
        }

    }
}

