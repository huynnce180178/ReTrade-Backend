using Microsoft.EntityFrameworkCore;
using RetradeBE.Data;
using RetradeBE.Models;

namespace RetradeBE.Repositories.AccountRole
{
    public class AccountRoleRepository : IAccountRoleRepository
    {
        private readonly AppDbContext _context;

        public AccountRoleRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<bool> AssignRoleAsync(string accountId, int roleId)
        {
            var exists = await _context.AccountRole
                .AnyAsync(ar => ar.AccountId == accountId && ar.RoleId == roleId);

            if (exists)
                return false;

            var accountRole = new Models.AccountRole
            {
                AccountId = accountId,
                RoleId = roleId,
                CreatedAt = DateTime.UtcNow
            };

            await _context.AccountRole.AddAsync(accountRole);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> RemoveRoleAsync(string accountId, int roleId)
        {
            var accountRole = await _context.AccountRole
                .FirstOrDefaultAsync(ar =>
                    ar.AccountId == accountId &&
                    ar.RoleId == roleId);

            if (accountRole == null)
                return false;

            _context.AccountRole.Remove(accountRole);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<List<Role>> GetRolesByAccountIdAsync(string accountId)
        {
            return await _context.AccountRole
                .Where(ar => ar.AccountId == accountId)
                .Select(ar => ar.Role)
                .ToListAsync();
        }

        async Task<List<Role>> IAccountRoleRepository.GetRolesByAccountIdAsync(string accountId)
        {
            return await _context.AccountRole
        .Where(ar => ar.AccountId == accountId)
        .Select(ar => ar.Role)
        .ToListAsync();
        }
        public async Task<List<Role>> GetAllRolesAsync()
        {
            return await _context.Role.ToListAsync();
        }
    }
}
