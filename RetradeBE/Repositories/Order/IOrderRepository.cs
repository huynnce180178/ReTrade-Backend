using RetradeBE.Models;

namespace RetradeBE.Repositories
{
    public interface IOrderRepository
    {
        IQueryable<Order> Query();
        Task<Order?> GetByIdAsync(string orderId);
        Task<Order?> GetForUpdateAsync(string orderId);
        Task AddAsync(Order order);
        Task UpdateAsync(Order order);
    }
}
