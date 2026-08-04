using Microsoft.EntityFrameworkCore;
using RetradeBE.Data;
using RetradeBE.Models;
using System.Linq;
using System.Threading.Tasks;

namespace RetradeBE.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly AppDbContext _context;

        public ProductRepository(AppDbContext context)
        {
            _context = context;
        }

        public IQueryable<Product> Query()
        {
            return _context.Product
                .Include(p => p.Category)
                .Include(p => p.Seller)
                .Include(p => p.ProductImage)
                    .ThenInclude(pi => pi.Image)
                .Include(p => p.ProductAttribute)
                    .ThenInclude(pa => pa.Attribute);
        }

        public async Task<Product?> GetByIdAsync(string productId)
        {
            return await _context.Product
                .Include(p => p.Category)
                .Include(p => p.Seller)
                .Include(p => p.ProductImage)
                    .ThenInclude(pi => pi.Image)
                .Include(p => p.ProductAttribute)
                    .ThenInclude(pa => pa.Attribute)
                .FirstOrDefaultAsync(p => p.ProductId == productId && p.IsDeleted != true);
        }

        public async Task AddAsync(Product product)
        {
            await _context.Product.AddAsync(product);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Product product)
        {
            _context.Product.Update(product);
            await _context.SaveChangesAsync();
        }
    }
}
