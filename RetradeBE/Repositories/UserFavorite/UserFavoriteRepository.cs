using Microsoft.EntityFrameworkCore;
using RetradeBE.Data;
using RetradeBE.Models;

namespace RetradeBE.Repositories
{
    public class UserFavoriteRepository : IUserFavoriteRepository
    {
        private readonly AppDbContext _context;

        public UserFavoriteRepository(AppDbContext context)
        {
            _context = context;
        }

        public IQueryable<UserFavorite> Query()
        {
            return _context.UserFavorite.AsQueryable();
        }

        public async Task<List<UserFavorite>> GetFavoritesByUserIdAsync(string userId)
        {
            return await _context.UserFavorite
                .Include(f => f.Category)
                    .ThenInclude(c => c.CategoryImage)
                        .ThenInclude(ci => ci.Image)
                .Where(f => f.UserId == userId)
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();
        }

        public async Task<UserFavorite?> GetByUserIdAndCategoryIdAsync(string userId, string categoryId)
        {
            return await _context.UserFavorite
                .FirstOrDefaultAsync(f => f.UserId == userId && f.CategoryId == categoryId);
        }

        public async Task<int> CountByUserIdAsync(string userId)
        {
            return await _context.UserFavorite.CountAsync(f => f.UserId == userId);
        }

        public async Task AddAsync(UserFavorite favorite)
        {
            await _context.UserFavorite.AddAsync(favorite);
            await _context.SaveChangesAsync();
        }

        public async Task RemoveAsync(UserFavorite favorite)
        {
            _context.UserFavorite.Remove(favorite);
            await _context.SaveChangesAsync();
        }
    }
}
