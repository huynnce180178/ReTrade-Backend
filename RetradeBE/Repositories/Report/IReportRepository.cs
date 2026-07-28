using RetradeBE.Models;

namespace RetradeBE.Repositories
{
    public interface IReportRepository
    {
        IQueryable<Report> Query();
        Task<Report?> GetReportByReporterAsync(string reviewId, string reporterId);
        Task AddReportAsync(Report report);
    }
}

