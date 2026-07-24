using RetradeBE.Models;

namespace RetradeBE.Repositories
{
    public interface IServiceSubscriptionRepository
    {
        IQueryable<ServiceSubscription> Query();
    }
}
