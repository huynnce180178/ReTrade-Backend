using RetradeBE.Data;
using RetradeBE.Models;

namespace RetradeBE.Repositories
{
    public class ServiceSubscriptionRepository : IServiceSubscriptionRepository
    {
        private readonly AppDbContext _context;

        public ServiceSubscriptionRepository(AppDbContext context)
        {
            _context = context;
        }

        public IQueryable<ServiceSubscription> Query()
        {
            return _context.ServiceSubscription;
        }
    }
}
