using RetradeBE.Models.DTOs;
using RetradeBE.Repositories.AccountRole;
using RetradeBE.Repositories;
using AutoMapper;

namespace RetradeBE.Services.AccountRole
{
    public class AccountRoleService : IAccountRoleService
    {
        private readonly IAccountRoleRepository _repository;
        private readonly IAccountRepository _accountRepository;
        private readonly IMapper _mapper;

        public AccountRoleService(
            IAccountRoleRepository repository,
            IAccountRepository accountRepository,
            IMapper mapper)
        {
            _repository = repository;
            _accountRepository = accountRepository;
            _mapper = mapper;
        }

        public async Task<ManageRoleDto?> GetManageRolesAsync(string accountId)
        {
            var account = await _accountRepository.GetByIdAsync(accountId);

            if (account == null)
                return null;

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
            return await _repository.AssignRoleAsync(accountId, roleId);
        }

        public async Task<bool> RemoveRoleAsync(string accountId, int roleId)
        {
            return await _repository.RemoveRoleAsync(accountId, roleId);
        }
    }
}
