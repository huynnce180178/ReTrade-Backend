using Microsoft.EntityFrameworkCore;
using RetradeBE.Data;
using RetradeBE.Models;

namespace RetradeBE.Repositories
{
    public class ProfileRepository : IProfileRepository
    {
        private readonly AppDbContext _context;

        public ProfileRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Account?> GetAccountWithUserAsync(string accountId)
        {
            return await _context.Account
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.AccountId == accountId);
        }

        public async Task<User?> GetUserByIdAsync(string userId)
        {
            return await _context.User.FirstOrDefaultAsync(u => u.UserId == userId);
        }

        public async Task<Account?> GetPrimaryAccountByUserIdAsync(string userId)
        {
            return await _context.Account
                .Where(a => a.UserId == userId && a.IsDeleted != true)
                .OrderBy(a => a.CreatedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<List<Address>> GetActiveAddressesByUserIdAsync(string userId)
        {
            return await _context.Address
                .Where(a => a.UserId == userId && a.IsDeleted != true)
                .OrderByDescending(a => a.IsDefault == true)
                .ThenByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

        public async Task<Address?> GetAddressByIdAsync(string addressId)
        {
            return await _context.Address.FirstOrDefaultAsync(a => a.AddressId == addressId);
        }

        public async Task<int> CountAddressesAsync()
        {
            return await _context.Address.CountAsync();
        }

        public async Task AddAddressAsync(Address address)
        {
            await _context.Address.AddAsync(address);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAddressAsync(Address address)
        {
            _context.Address.Update(address);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateUserAsync(User user)
        {
            _context.User.Update(user);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAccountAsync(Account account)
        {
            _context.Account.Update(account);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> UsernameExistsAsync(string username, string excludeAccountId)
        {
            return await _context.Account.AnyAsync(a => a.Username == username && a.AccountId != excludeAccountId);
        }

        public async Task<bool> EmailExistsAsync(string email, string excludeUserId)
        {
            return await _context.User.AnyAsync(u => u.Email == email && u.UserId != excludeUserId);
        }

        public async Task<bool> FollowExistsAsync(string followerId, string followedUserId)
        {
            return await _context.UserFollow.AnyAsync(f => f.FollowerId == followerId && f.FollowedUserId == followedUserId);
        }

        public async Task<int> CountFollowsAsync()
        {
            return await _context.UserFollow.CountAsync();
        }

        public async Task<int> CountFollowersAsync(string userId)
        {
            return await _context.UserFollow.CountAsync(f => f.FollowedUserId == userId);
        }

        public async Task<int> CountFollowingAsync(string userId)
        {
            return await _context.UserFollow.CountAsync(f => f.FollowerId == userId);
        }

        public async Task<int> CountProductsAsync(string sellerId)
        {
            return await _context.Product.CountAsync(p => p.SellerId == sellerId && p.IsDeleted != true);
        }

        public async Task<double?> GetAverageSellerRatingAsync(string sellerId)
        {
            var ratings = GetSellerReviewsQuery(sellerId);
            if (!await ratings.AnyAsync()) return null;

            return await ratings.AverageAsync(r => r.Rating!.Value);
        }

        public async Task<int> CountSellerReviewsAsync(string sellerId)
        {
            return await GetSellerReviewsQuery(sellerId).CountAsync();
        }

        public async Task<Dictionary<int, int>> GetSellerRatingCountsAsync(string sellerId)
        {
            return await GetSellerReviewsQuery(sellerId)
                .GroupBy(r => r.Rating!.Value)
                .ToDictionaryAsync(group => group.Key, group => group.Count());
        }

        private IQueryable<Review> GetSellerReviewsQuery(string sellerId)
        {
            return _context.Review
                .Where(r => r.Rating != null
                    && (r.SellerId == sellerId || (r.Order != null && r.Order.SellerId == sellerId)));
        }

        public async Task AddFollowAsync(UserFollow follow)
        {
            await _context.UserFollow.AddAsync(follow);
            await _context.SaveChangesAsync();
        }

        public async Task<UserFollow?> GetFollowAsync(string followerId, string followedUserId)
        {
            return await _context.UserFollow
                .FirstOrDefaultAsync(f => f.FollowerId == followerId && f.FollowedUserId == followedUserId);
        }

        public async Task RemoveFollowAsync(UserFollow follow)
        {
            _context.UserFollow.Remove(follow);
            await _context.SaveChangesAsync();
        }
    }
}
