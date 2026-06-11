using RetradeBE.Models.DTOs;

namespace RetradeBE.Services.AccountRole
{
    public interface IAccountRoleService
    {
        Task<ManageRoleDto?> GetManageRolesAsync(string accountId);

        Task<bool> AssignRoleAsync(string accountId, int roleId);

        Task<bool> RemoveRoleAsync(string accountId, int roleId);
    }
}
