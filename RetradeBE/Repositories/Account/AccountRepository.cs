using Microsoft.EntityFrameworkCore;
using RetradeBE.Data;
using RetradeBE.Models;

namespace RetradeBE.Repositories
{
    public class AccountRepository : IAccountRepository
    {
        private readonly AppDbContext _context;

        public AccountRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Account item)
        {
            await _context.Account.AddAsync(item);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(object id)
        {
            var item = await _context.Account.FindAsync(id);
            if (item != null)
            {
                item.IsDeleted = true;
                item.Status = RetradeBE.Models.Enums.AccountStatusEnum.Inactive.ToString();
                _context.Account.Update(item);
                await _context.SaveChangesAsync();
            }
        }

        public async Task RestoreAsync(object id)
        {
            var item = await _context.Account.FindAsync(id);
            if (item != null)
            {
                item.IsDeleted = false;
                item.Status = RetradeBE.Models.Enums.AccountStatusEnum.Active.ToString();
                _context.Account.Update(item);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<int> CountAllAccountsAsync()
        {
            return await _context.Account.CountAsync();
        }

        public async Task<IEnumerable<Account>> GetAllAsync()
        {
            return await _context.Account.ToListAsync();
        }

        public async Task<Account> GetByIdAsync(object id)
        {
            return await _context.Account.FindAsync(id);
        }

        public async Task<Account?> GetByUsernameAsync(string username)
        {
            return await _context.Account.FirstOrDefaultAsync(a => a.Username == username);
        }

        public async Task AssignRoleAsync(string accountId, string roleName)
        {
            if (!Enum.TryParse<RetradeBE.Models.Enums.RoleEnum>(roleName, out var roleEnum))
            {
                roleEnum = (RetradeBE.Models.Enums.RoleEnum)Enum.Parse(typeof(RetradeBE.Models.Enums.RoleEnum), "Buyer");
            }
            int roleId = (int)roleEnum;

            var role = await _context.Role.FindAsync(roleId);
            if (role == null)
            {
                role = new Role { RoleId = roleId, Name = roleName };
                await _context.Role.AddAsync(role);
                await _context.SaveChangesAsync();
            }

            var accountRole = new AccountRole { AccountId = accountId, RoleId = role.RoleId, CreatedAt = DateTime.UtcNow.AddHours(7) };
            await _context.AccountRole.AddAsync(accountRole);
            await _context.SaveChangesAsync();
        }

        public async Task<List<string>> GetRolesAsync(string accountId)
        {
            return await _context.AccountRole
                .Where(ar => ar.AccountId == accountId)
                .Include(ar => ar.Role)
                .Where(ar => ar.Role != null && ar.Role.Name != null)
                .Select(ar => ar.Role!.Name!)
                .ToListAsync();
        }

        public async Task UpdateAsync(Account item)
        {
            _context.Account.Update(item);
            await _context.SaveChangesAsync();
        }
    }
}
