using RetradeBE.Models;

namespace RetradeBE.Repositories
{
    public interface IAccountRepository
    {
        Task<IEnumerable<Account>> GetAllAsync();
        Task<Account> GetByIdAsync(object id);
        Task<Account?> GetByUsernameAsync(string username);
        Task<List<string>> GetRolesAsync(string accountId);
        Task AssignRoleAsync(string accountId, string roleName);
        Task AddAsync(Account item);
        Task UpdateAsync(Account item);
        Task DeleteAsync(object id);
        Task RestoreAsync(object id);
        Task<int> CountAllAccountsAsync();
    }
}
