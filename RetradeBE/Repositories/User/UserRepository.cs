using Microsoft.EntityFrameworkCore;
using RetradeBE.Data;
using RetradeBE.Models;

namespace RetradeBE.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly AppDbContext _context;

        public UserRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(User item)
        {
            await _context.User.AddAsync(item);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(object id)
        {
            var item = await _context.User.FindAsync(id);
            if (item != null)
            {
                item.IsDeleted = true;
                _context.User.Update(item);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<int> CountAllUsersAsync()
        {
            return await _context.User.CountAsync();
        }

        public async Task<IEnumerable<User>> GetAllAsync()
        {
            return await _context.User.ToListAsync();
        }

        public async Task<User> GetByIdAsync(object id)
        {
            return await _context.User.FindAsync(id);
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _context.User.FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task UpdateAsync(User item)
        {
            _context.User.Update(item);
            await _context.SaveChangesAsync();
        }
    }
}
