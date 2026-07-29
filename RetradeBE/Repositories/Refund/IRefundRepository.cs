using RetradeBE.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RetradeBE.Repositories.Refund
{
    public interface IRefundRepository
    {
        Task<IEnumerable<RefundRequest>> GetAllRefundsWithUserAsync();
        Task<RefundRequest?> GetByIdAsync(string id);
        Task UpdateAsync(RefundRequest refundRequest);
    }
}
