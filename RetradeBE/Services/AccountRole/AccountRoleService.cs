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
            if (string.IsNullOrWhiteSpace(accountId))
            {
                throw new System.Collections.Generic.KeyNotFoundException("Account does not exist.");
            }

            var account = await _accountService.GetByIdAsync(accountId);

            if (account == null)
            {
                throw new System.Collections.Generic.KeyNotFoundException("Account does not exist.");
            }

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
                return false;

            var account = await _accountService.GetByIdAsync(accountId);
            if (account == null)
                return false;

            var allRoles = await _repository.GetAllRolesAsync();
            if (!allRoles.Any(r => r.RoleId == roleId))
                return false;

            return await _repository.AssignRoleAsync(accountId, roleId);
        }

        public async Task<bool> RemoveRoleAsync(string accountId, int roleId)
        {
            if (string.IsNullOrEmpty(accountId))
                return false;

            var account = await _accountService.GetByIdAsync(accountId);
            if (account == null)
                return false;

            var allRoles = await _repository.GetAllRolesAsync();
            if (!allRoles.Any(r => r.RoleId == roleId))
                return false;

            return await _repository.RemoveRoleAsync(accountId, roleId);
        }
    }
}
