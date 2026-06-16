using Microsoft.EntityFrameworkCore;
using RetradeBE.Data;
using RetradeBE.Models;
using System;
using System.Threading.Tasks;

namespace RetradeBE.Repositories
{
    public class WishlistRepository : IWishlistRepository
    {
        private readonly AppDbContext _context;

        public WishlistRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Wishlist> GetOrCreateActiveWishlistAsync(string userId)
        {
            var wishlist = await _context.Wishlist
                .Include(w => w.WishlistItem)
                    .ThenInclude(wi => wi.Product)
                        .ThenInclude(p => p!.ProductImage)
                            .ThenInclude(pi => pi.Image)
                .Include(w => w.WishlistItem)
                    .ThenInclude(wi => wi.Product)
                        .ThenInclude(p => p!.Seller)
                .FirstOrDefaultAsync(w => w.UserId == userId && w.IsDeleted != true);

            if (wishlist != null)
                return wishlist;

            var newWishlist = new Wishlist
            {
                WishlistId = $"WL_{Guid.NewGuid():N}",
                UserId     = userId,
                Status     = "Active",
                IsDeleted  = false,
                CreatedAt  = DateTime.UtcNow,
                UpdatedAt  = DateTime.UtcNow
            };

            await _context.Wishlist.AddAsync(newWishlist);
            await _context.SaveChangesAsync();
            return newWishlist;
        }

        public async Task<bool> IsProductInWishlistAsync(string wishlistId, string productId)
        {
            return await _context.WishlistItem
                .AnyAsync(wi => wi.WishlistId == wishlistId && wi.ProductId == productId);
        }

        public async Task AddItemAsync(WishlistItem item)
        {
            await _context.WishlistItem.AddAsync(item);
            await _context.SaveChangesAsync();
        }

        public async Task<WishlistItem?> GetItemByIdAsync(string wishlistItemId)
        {
            return await _context.WishlistItem
                .Include(wi => wi.Product)
                    .ThenInclude(p => p!.ProductImage)
                        .ThenInclude(pi => pi.Image)
                .Include(wi => wi.Product)
                    .ThenInclude(p => p!.Seller)
                .FirstOrDefaultAsync(wi => wi.WishlistItemId == wishlistItemId);
        }

        public async Task<WishlistItem?> GetItemByProductAsync(string wishlistId, string productId)
        {
            return await _context.WishlistItem
                .FirstOrDefaultAsync(wi => wi.WishlistId == wishlistId && wi.ProductId == productId);
        }

        public async Task RemoveItemAsync(WishlistItem item)
        {
            _context.WishlistItem.Remove(item);
            await _context.SaveChangesAsync();
        }
    }
}
