using RetradeBE.Models.DTOs;
using RetradeBE.Repositories.AccountRole;
using RetradeBE.Repositories;
using AutoMapper;

namespace RetradeBE.Services.AccountRole
{
    public class AccountRoleService : IAccountRoleService
    {
        private readonly IAccountRoleRepository _repository;
        private readonly IAccountService _accountService;
        private readonly IMapper _mapper;

        public AccountRoleService(
            IAccountRoleRepository repository,
            IAccountService accountService,
            IMapper mapper)
        {
            _repository = repository;
            _accountService = accountService;
            _mapper = mapper;
        }

        public async Task<ManageRoleDto?> GetManageRolesAsync(string accountId)
        {
            var account = await _accountService.GetByIdAsync(accountId);

            if (account == null)
                throw new KeyNotFoundException("Account not found.");

            var allRoles = await _repository.GetAllRolesAsync();
            var userRoles = await _repository.GetRolesByAccountIdAsync(accountId);

            return new ManageRoleDto
            {
                AccountId = accountId,
                AllRoles = _mapper.Map<List<RoleDto>>(allRoles),
                AssignedRole = _mapper.Map<List<RoleDto>>(userRoles)
            };
        }

        public async Task<bool> AssignRoleAsync(string accountId, int roleId)
        {
            if (string.IsNullOrEmpty(accountId))
                throw new KeyNotFoundException("Account not found.");

            var account = await _accountService.GetByIdAsync(accountId);
            if (account == null)
                throw new KeyNotFoundException("Account not found.");

            var allRoles = await _repository.GetAllRolesAsync();
            if (!allRoles.Any(r => r.RoleId == roleId))
                throw new KeyNotFoundException("Role not found.");

            var assigned = await _repository.AssignRoleAsync(accountId, roleId);
            if (!assigned)
            {
                throw new InvalidOperationException("Role is already assigned to this account.");
            }

            return true;
        }

        public async Task<bool> RemoveRoleAsync(string accountId, int roleId)
        {
            if (string.IsNullOrEmpty(accountId))
                throw new KeyNotFoundException("Account not found.");

            var account = await _accountService.GetByIdAsync(accountId);
            if (account == null)
                throw new KeyNotFoundException("Account not found.");

            var allRoles = await _repository.GetAllRolesAsync();
            if (!allRoles.Any(r => r.RoleId == roleId))
                throw new KeyNotFoundException("Role not found.");

            var removed = await _repository.RemoveRoleAsync(accountId, roleId);
            if (!removed)
            {
                throw new KeyNotFoundException("Role assignment not found.");
            }

            return true;
        }
    }
}
