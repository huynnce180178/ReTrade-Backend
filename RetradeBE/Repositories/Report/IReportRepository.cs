using RetradeBE.Models;

namespace RetradeBE.Repositories
{
    public interface IReportRepository
    {
        IQueryable<Report> Query();
        Task<Report?> GetReportByReporterAsync(string reviewId, string reporterId);
        Task<Report?> GetByIdAsync(string reportId);
        Task<Report?> GetByTargetAndReporterAsync(string targetId, string reporterId, string targetType);
        Task<List<Report>> GetReportsByReporterAsync(string reporterId);
        Task<List<Report>> GetReportsReceivedByUserAsync(string userId);
        Task<List<Report>> GetReportsForUserAsync(string userId);
        Task<bool> ExistsAsync(string targetId, string reporterId, string targetType);
        Task AddAsync(Report report);
        Task UpdateAsync(Report report);
    }
}

