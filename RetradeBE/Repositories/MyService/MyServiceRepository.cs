using RetradeBE.Data;
using RetradeBE.Models;

namespace RetradeBE.Repositories
{
    public class MyServiceRepository : IMyServiceRepository
    {
        private readonly AppDbContext _context;

        public MyServiceRepository(AppDbContext context)
        {
            _context = context;
        }

        public IQueryable<MyService> Query()
        {
            return _context.MyService;
        }
    }
}
