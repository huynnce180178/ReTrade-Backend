using Microsoft.EntityFrameworkCore;
using RetradeBE.Data;
using RetradeBE.Models;

namespace RetradeBE.Repositories
{
    public class UserSearchRepository : IUserSearchRepository
    {
        private readonly AppDbContext _context;

        public UserSearchRepository(AppDbContext context)
        {
            _context = context;
        }

        public IQueryable<UserSearch> Query()
        {
            return _context.UserSearch.AsQueryable();
        }

        public async Task<List<UserSearch>> GetHistoryByUserIdAsync(string userId, int limit)
        {
            return await _context.UserSearch
                .Include(s => s.Category)
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.CreatedAt)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<UserSearch?> GetRecentDuplicateAsync(string userId, string keyword)
        {
            return await _context.UserSearch
                .Where(s => s.UserId == userId
                         && s.Keyword == keyword
                         && s.CreatedAt > DateTime.UtcNow.AddMinutes(-5))
                .FirstOrDefaultAsync();
        }

        public async Task<UserSearch?> GetByIdAndUserIdAsync(string searchId, string userId)
        {
            return await _context.UserSearch
                .FirstOrDefaultAsync(s => s.SearchId == searchId && s.UserId == userId);
        }

        public async Task<List<UserSearch>> GetAllByUserIdAsync(string userId)
        {
            return await _context.UserSearch
                .Where(s => s.UserId == userId)
                .ToListAsync();
        }

        public async Task AddAsync(UserSearch userSearch)
        {
            await _context.UserSearch.AddAsync(userSearch);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(UserSearch userSearch)
        {
            _context.UserSearch.Update(userSearch);
            await _context.SaveChangesAsync();
        }

        public async Task RemoveAsync(UserSearch userSearch)
        {
            _context.UserSearch.Remove(userSearch);
            await _context.SaveChangesAsync();
        }

        public async Task RemoveRangeAsync(IEnumerable<UserSearch> userSearches)
        {
            _context.UserSearch.RemoveRange(userSearches);
            await _context.SaveChangesAsync();
        }
    }
}
