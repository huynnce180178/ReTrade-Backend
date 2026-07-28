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
                .Include(report => report.Reporter);
        }

        public async Task<Report?> GetReportByReporterAsync(string reviewId, string reporterId)
        {
            return await _context.Report
                .AsNoTracking()
                .FirstOrDefaultAsync(report =>
                    report.TargetType == "Review" &&
                    report.TargetId == reviewId &&
                    report.ReporterId == reporterId);
        }

        public async Task AddReportAsync(Report report)
        {
            await _context.Report.AddAsync(report);
            await _context.SaveChangesAsync();
        }
    }
}

