using Microsoft.EntityFrameworkCore;
using RetradeBE.Data;
using RetradeBE.Models;

namespace RetradeBE.Repositories
{
    public class AddressRepository : IAddressRepository
    {
        private readonly AppDbContext _context;

        public AddressRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<string?> GetUserIdByAccountIdAsync(string accountId)
        {
            var account = await _context.Account
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.AccountId == accountId && a.IsDeleted != true);

            return account?.UserId;
        }

        public Task<List<Address>> GetActiveByUserIdAsync(string userId)
        {
            return _context.Address
                .Where(a => a.UserId == userId && a.IsDeleted != true)
                .OrderByDescending(a => a.IsDefault == true)
                .ThenByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

        public Task<Address?> GetOwnedActiveAsync(string userId, string addressId)
        {
            return _context.Address.FirstOrDefaultAsync(a =>
                a.AddressId == addressId &&
                a.UserId == userId &&
                a.IsDeleted != true);
        }

        public Task<bool> HasActiveAddressAsync(string userId)
        {
            return _context.Address.AnyAsync(a => a.UserId == userId && a.IsDeleted != true);
        }

        public Task<int> CountAsync()
        {
            return _context.Address.CountAsync();
        }

        public async Task AddAsync(Address address)
        {
            await _context.Address.AddAsync(address);
            await _context.SaveChangesAsync();
        }

        public Task SaveChangesAsync()
        {
            return _context.SaveChangesAsync();
        }
    }
}
