using Microsoft.EntityFrameworkCore;
using RetradeBE.Data;
using RetradeBE.Models;

namespace RetradeBE.Repositories
{
    public class AuctionRepository : IAuctionRepository
    {
        private readonly AppDbContext _context;

        public AuctionRepository(AppDbContext context)
        {
            _context = context;
        }

        public IQueryable<Auction> Query()
        {
            return _context.Auction
                .Include(a => a.Product)
                    .ThenInclude(p => p!.Category)
                .Include(a => a.Product)
                    .ThenInclude(p => p!.ProductImage)
                        .ThenInclude(pi => pi.Image)
                .Include(a => a.Product)
                    .ThenInclude(p => p!.ProductAttribute)
                        .ThenInclude(pa => pa.Attribute)
                .Include(a => a.Seller)
                .Include(a => a.Winner)
                .Include(a => a.Bid)
                    .ThenInclude(b => b.User);
        }

        public IQueryable<Product> QueryEligibleProducts()
        {
            var openStatuses = new[] { "Upcoming", "Ongoing" };
            var now = DateTime.UtcNow;

            return _context.Product
                .Include(p => p.Category)
                .Include(p => p.Seller)
                .Include(p => p.ProductImage)
                    .ThenInclude(pi => pi.Image)
                .Where(p => p.IsDeleted != true
                    && p.Status == "Ready"
                    && p.Category != null && p.Category.Status == "Active"
                    && !p.Auction.Any(a => openStatuses.Contains(a.Status!) && (a.EndTime == null || a.EndTime > now)));
        }

        public async Task<Auction?> GetByIdAsync(string auctionId)
        {
            return await Query().FirstOrDefaultAsync(a => a.AuctionId == auctionId);
        }

        public async Task AddAsync(Auction auction)
        {
            await _context.Auction.AddAsync(auction);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Auction auction)
        {
            _context.Auction.Update(auction);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> HasOpenAuctionForProductAsync(string productId)
        {
            var openStatuses = new[] { "Upcoming", "Ongoing" };
            var now = DateTime.UtcNow;
            return await _context.Auction.AnyAsync(a => a.ProductId == productId
                && openStatuses.Contains(a.Status!)
                && (a.EndTime == null || a.EndTime > now));
        }
    }
}
