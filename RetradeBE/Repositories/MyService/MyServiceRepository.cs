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

        public async Task AddAsync(MyService entity)
        {
            await _context.MyService.AddAsync(entity);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(MyService entity)
        {
            _context.MyService.Update(entity);
            await _context.SaveChangesAsync();
        }
    }
}
