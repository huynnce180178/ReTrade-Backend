using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RetradeBE.Data;
using RetradeBE.Models;

namespace RetradeBE.Repositories
{
    public class MyVoucherRepository : IMyVoucherRepository
    {
        private readonly AppDbContext _context;

        public MyVoucherRepository(AppDbContext context)
        {
            _context = context;
        }

        public IQueryable<MyVoucher> Query()
        {
            return _context.MyVoucher;
        }

        public async Task<int> CountByUserIdAsync(string userId)
        {
            return await _context.MyVoucher.AsNoTracking().CountAsync(mv => mv.UserId == userId);
        }

        public async Task AddAsync(MyVoucher myVoucher)
        {
            await _context.MyVoucher.AddAsync(myVoucher);
        }

        public async Task AddRangeAsync(IEnumerable<MyVoucher> myVouchers)
        {
            await _context.MyVoucher.AddRangeAsync(myVouchers);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
