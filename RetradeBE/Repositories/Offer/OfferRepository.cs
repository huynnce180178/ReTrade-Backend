using Microsoft.EntityFrameworkCore;
using RetradeBE.Data;
using RetradeBE.Models;

namespace RetradeBE.Repositories
{
    public class OfferRepository : IOfferRepository
    {
        private readonly AppDbContext _context;

        public OfferRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Offer>> GetAllAsync()
        {
            return await _context.Offer
                .Include(o => o.Buyer)
                .Include(o => o.Product)
                .ToListAsync();
        }

        public async Task<Offer?> GetByIdAsync(string offerId)
        {
            return await _context.Offer
                .Include(o => o.Buyer)
                .Include(o => o.Product)
                    .ThenInclude(p => p.ProductImage)
                        .ThenInclude(pi => pi.Image)
                .FirstOrDefaultAsync(o => o.OfferId == offerId);
        }

        public async Task AddAsync(Offer offer)
        {
            await _context.Offer.AddAsync(offer);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Offer offer)
        {
            _context.Offer.Update(offer);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Offer offer)
        {
            _context.Offer.Remove(offer);
            await _context.SaveChangesAsync();
        }
        public async Task<List<Offer>> GetOffersBySellerAsync(string sellerUserId)
        {
            return await _context.Offer
                .Include(o => o.Buyer)
                .Include(o => o.Product)
                    .ThenInclude(p => p.ProductImage)
                        .ThenInclude(pi => pi.Image)
                .Where(o =>
                    o.Product != null &&
                    o.Product.SellerId == sellerUserId)
                .ToListAsync();
        }
    }
}
