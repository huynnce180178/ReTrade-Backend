using RetradeBE.Models;
using System.Threading.Tasks;

namespace RetradeBE.Repositories
{
    public interface IAdminProductRepository
    {
        IQueryable<Product> Query();
        Task<Product?> GetByIdAsync(string productId);
        Task UpdateAsync(Product product);
    }
}
