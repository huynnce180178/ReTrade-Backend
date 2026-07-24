using RetradeBE.Models;

namespace RetradeBE.Repositories
{
    public interface IMyServiceRepository
    {
        IQueryable<MyService> Query();
    }
}
