using Microsoft.EntityFrameworkCore;
using RetradeBE.Data;
using RetradeBE.Models;
using RetradeBE.Models.DTOs;
using RetradeBE.Models.Enums;
using RetradeBE.Repositories;
using System;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using AutoMapper.QueryableExtensions;

namespace RetradeBE.Services
{
    public class WishlistService : IWishlistService
    {
        private readonly IWishlistRepository _wishlistRepository;
        private readonly IAccountRepository  _accountRepository;
        private readonly AppDbContext        _context;
        private readonly IMapper             _mapper;

        public WishlistService(
            IWishlistRepository wishlistRepository,
            IAccountRepository  accountRepository,
            AppDbContext        context,
            IMapper             mapper)
        {
            _wishlistRepository = wishlistRepository;
            _accountRepository  = accountRepository;
            _context            = context;
            _mapper             = mapper;
        }

        public async Task<WishlistDetailDto> AddToWishlistAsync(string accountId, AddToWishlistDto dto)
        {
            var userId = await ResolveUserIdAsync(accountId);

            var product = await _context.Product
                .FirstOrDefaultAsync(p => p.ProductId == dto.ProductId && p.IsDeleted != true);

            if (product == null)
                throw new Exception("Product does not exist or has been deleted.");

            if (product.Status != ProductStatusEnum.Accepted.ToString() && product.Status != ProductStatusEnum.Ready.ToString())
                throw new Exception("Product is not available to be added to the wishlist.");

            if (product.SellerId == userId)
                throw new Exception("You cannot add your own product to your wishlist.");

            var wishlist = await GetActiveWishlistAsync(userId);

            var alreadyAdded = await _wishlistRepository.IsProductInWishlistAsync(wishlist.WishlistId, dto.ProductId);
            if (alreadyAdded)
                throw new Exception("Product is already in the wishlist.");

            var item = new WishlistItem
            {
                WishlistItemId = $"WI_{Guid.NewGuid():N}",
                WishlistId     = wishlist.WishlistId,
                ProductId      = dto.ProductId,
                CreatedAt      = DateTime.UtcNow
            };

            await _wishlistRepository.AddItemAsync(item);

            wishlist.UpdatedAt = DateTime.UtcNow;
            _context.Wishlist.Update(wishlist);
            await _context.SaveChangesAsync();

            var updated = await _wishlistRepository.GetOrCreateActiveWishlistAsync(userId);
            return _mapper.Map<WishlistDetailDto>(updated);
        }

        public async Task<WishlistDetailDto> GetWishlistDetailAsync(string accountId)
        {
            var userId   = await ResolveUserIdAsync(accountId);
            var wishlist = await _wishlistRepository.GetOrCreateActiveWishlistAsync(userId);
            return _mapper.Map<WishlistDetailDto>(wishlist);
        }

        public async Task<IQueryable<WishlistItemDto>> GetWishlistItemsQueryAsync(string accountId)
        {
            var userId   = await ResolveUserIdAsync(accountId);
            var wishlist = await _wishlistRepository.GetOrCreateActiveWishlistAsync(userId);
            return _context.WishlistItem
                .Where(wi => wi.WishlistId == wishlist.WishlistId)
                .ProjectTo<WishlistItemDto>(_mapper.ConfigurationProvider);
        }

        public async Task RemoveWishlistItemAsync(string accountId, string wishlistItemId)
        {
            var userId   = await ResolveUserIdAsync(accountId);
            var wishlist = await GetActiveWishlistAsync(userId);

            var item = await _wishlistRepository.GetItemByIdAsync(wishlistItemId);

            if (item == null)
                throw new Exception("Wishlist item not found.");

            if (item.WishlistId != wishlist.WishlistId)
                throw new Exception("You do not have permission to remove this item.");

            await _wishlistRepository.RemoveItemAsync(item);

            wishlist.UpdatedAt = DateTime.UtcNow;
            _context.Wishlist.Update(wishlist);
            await _context.SaveChangesAsync();
        }

        private async Task<string> ResolveUserIdAsync(string accountId)
        {
            var account = await _accountRepository.GetByIdAsync(accountId);

            if (account == null || account.IsDeleted == true)
                throw new Exception("Account does not exist or has been deactivated.");

            if (account.Status != AccountStatusEnum.Active.ToString())
                throw new Exception("Account is not active or has been suspended.");

            if (string.IsNullOrEmpty(account.UserId))
                throw new Exception("Account is not linked to any user profile.");

            return account.UserId;
        }

        private async Task<Wishlist> GetActiveWishlistAsync(string userId)
        {
            var wishlist = await _wishlistRepository.GetOrCreateActiveWishlistAsync(userId);

            if (wishlist.IsDeleted == true)
                throw new Exception("Wishlist has been deleted.");

            if (wishlist.Status != "Active")
                throw new Exception("Wishlist is not currently available.");

            return wishlist;
        }
    }
}
