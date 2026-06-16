using RetradeBE.Models;

namespace RetradeBE.Repositories
{
    public interface IOrderRepository
    {
        IQueryable<Order> Query();
        Task<Order?> GetByIdAsync(string orderId);
    }
}
