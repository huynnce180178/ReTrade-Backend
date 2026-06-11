using RetradeBE.Models;

namespace RetradeBE.Repositories.AccountRole
{
    public interface IAccountRoleRepository
    {
        Task<bool> AssignRoleAsync(string accountId, int roleId);
        Task<bool> RemoveRoleAsync(string accountId, int roleId);
        Task<List<Role>> GetRolesByAccountIdAsync(string accountId);
        Task<List<Role>> GetAllRolesAsync();
    }
}
