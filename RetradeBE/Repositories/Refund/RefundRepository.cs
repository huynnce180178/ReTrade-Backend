using Microsoft.EntityFrameworkCore;
using RetradeBE.Data;
using RetradeBE.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RetradeBE.Repositories.Refund
{
    public class RefundRepository : IRefundRepository
    {
        private readonly AppDbContext _context;

        public RefundRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<RefundRequest>> GetAllRefundsWithUserAsync()
        {
            return await _context.RefundRequest
                .AsNoTracking()
                .Include(r => r.User)
                .OrderByDescending(r => r.RequestedAt)
                .ToListAsync();
        }

        public async Task<RefundRequest?> GetByIdAsync(string id)
        {
            return await _context.RefundRequest.FindAsync(id);
        }

        public async Task UpdateAsync(RefundRequest refundRequest)
        {
            _context.RefundRequest.Update(refundRequest);
            await _context.SaveChangesAsync();
        }
    }
}
