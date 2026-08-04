using RetradeBE.Models;

namespace RetradeBE.Repositories
{
    public interface IMyServiceRepository
    {
        IQueryable<MyService> Query();
        Task AddAsync(MyService entity);
        Task UpdateAsync(MyService entity);
    }
}
