using Microsoft.EntityFrameworkCore;
using RetradeBE.Data;
using RetradeBE.Models;

namespace RetradeBE.Repositories
{
    public class OrderRepository : IOrderRepository
    {
        private readonly AppDbContext _context;

        public OrderRepository(AppDbContext context)
        {
            _context = context;
        }

        public IQueryable<Order> Query()
        {
            return _context.Order
                .AsNoTracking()
                .Include(o => o.User)
                .Include(o => o.Seller)
                .Include(o => o.Product)
                    .ThenInclude(p => p!.ProductImage)
                    .ThenInclude(pi => pi.Image)
                .Include(o => o.Payment);
        }

        public async Task<Order?> GetByIdAsync(string orderId)
        {
            return await Query()
                .FirstOrDefaultAsync(o => o.OrderId == orderId);
        }
    }
}
