using System.Collections.Generic;
using System.Threading.Tasks;
using RetradeBE.Models;

namespace RetradeBE.Services
{
    public interface ISubscriptionVoucherService
    {
        Task<List<Voucher>> GenerateSubscriptionVouchersAsync(string userId);
    }
}
