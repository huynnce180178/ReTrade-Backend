using RetradeBE.Models;

namespace RetradeBE.Repositories
{
    public interface IMyVoucherRepository
    {
        IQueryable<MyVoucher> Query();
        Task<int> CountByUserIdAsync(string userId);
        Task AddAsync(MyVoucher myVoucher);
        Task AddRangeAsync(IEnumerable<MyVoucher> myVouchers);
        Task SaveChangesAsync();
    }
}
