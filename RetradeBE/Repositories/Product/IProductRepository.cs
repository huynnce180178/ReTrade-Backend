using RetradeBE.Models;
using System.Threading.Tasks;

namespace RetradeBE.Repositories
{
    public interface IProductRepository
    {
        IQueryable<Product> Query();
        Task<Product?> GetByIdAsync(string productId);
        Task AddAsync(Product product);
        Task UpdateAsync(Product product);
    }
}
