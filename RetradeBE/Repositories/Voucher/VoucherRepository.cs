using RetradeBE.Data;
using RetradeBE.Models;

namespace RetradeBE.Repositories
{
    public class VoucherRepository : IVoucherRepository
    {
        private readonly AppDbContext _context;

        public VoucherRepository(AppDbContext context)
        {
            _context = context;
        }

        public IQueryable<Voucher> Query()
        {
            return _context.Voucher;
        }

        public async Task AddAsync(Voucher voucher)
        {
            await _context.Voucher.AddAsync(voucher);
        }

        public async Task AddRangeAsync(IEnumerable<Voucher> vouchers)
        {
            await _context.Voucher.AddRangeAsync(vouchers);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
