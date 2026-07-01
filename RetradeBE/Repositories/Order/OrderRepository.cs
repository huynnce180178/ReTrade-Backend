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
                .Include(o => o.Buyer)
                .Include(o => o.Seller)
                .Include(o => o.Product)
                    .ThenInclude(p => p!.ProductImage)
                    .ThenInclude(pi => pi.Image)
                .Include(o => o.Payment)
                .Include(o => o.Review);
        }

        public async Task<Order?> GetByIdAsync(string orderId)
        {
            return await Query()
                .FirstOrDefaultAsync(o => o.OrderId == orderId);
        }

        public async Task<Order?> GetForUpdateAsync(string orderId)
        {
            return await _context.Order
                .Include(o => o.Buyer)
                .Include(o => o.Seller)
                .Include(o => o.Product)
                    .ThenInclude(p => p!.ProductImage)
                    .ThenInclude(pi => pi.Image)
                .Include(o => o.Payment)
                .Include(o => o.Review)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);
        }

        public async Task AddAsync(Order order)
        {
            await _context.Order.AddAsync(order);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Order order)
        {
            _context.Order.Update(order);
            await _context.SaveChangesAsync();
        }
    }
}
