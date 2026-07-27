using RetradeBE.Models;

namespace RetradeBE.Repositories
{
    public interface IVoucherRepository
    {
        IQueryable<Voucher> Query();
        Task AddAsync(Voucher voucher);
        Task AddRangeAsync(IEnumerable<Voucher> vouchers);
        Task SaveChangesAsync();
    }
}
