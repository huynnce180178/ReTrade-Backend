using Microsoft.AspNetCore.SignalR;
using RetradeBE.Hubs;
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
        private readonly IHubContext<AccountHub> _accountHub;
        private readonly INotificationService _notificationService;

        public AccountRoleService(
            IAccountRoleRepository repository,
            IAccountService accountService,
            IMapper mapper,
            IHubContext<AccountHub> accountHub,
            INotificationService notificationService)
        {
            _repository = repository;
            _accountService = accountService;
            _mapper = mapper;
            _accountHub = accountHub;
            _notificationService = notificationService;
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
            var targetRole = allRoles.FirstOrDefault(r => r.RoleId == roleId);
            if (targetRole == null)
                throw new KeyNotFoundException("Role not found.");

            var assigned = await _repository.AssignRoleAsync(accountId, roleId);
            if (!assigned)
            {
                throw new InvalidOperationException("Role is already assigned to this account.");
            }

            try
            {
                if (!string.IsNullOrWhiteSpace(account.UserId))
                {
                    await _notificationService.CreateAndSendAsync(new CreateNotificationDto
                    {
                        UserId = account.UserId,
                        Title = "Cập nhật phân quyền tài khoản",
                        Message = $"Tài khoản của bạn đã được quản trị viên cấp vai trò '{targetRole.Name}'.",
                        Type = "System",
                        ReferenceId = accountId
                    });
                }
            }
            catch
            {
                // Non-blocking notification error handling
            }

            await _accountHub.Clients
                .Group(AccountHub.GetAccountGroupName(accountId))
                .SendAsync("ForceLogout", $"Role '{targetRole.Name}' has been assigned to your account. Please log in again.");

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
            var targetRole = allRoles.FirstOrDefault(r => r.RoleId == roleId);
            if (targetRole == null)
                throw new KeyNotFoundException("Role not found.");

            var removed = await _repository.RemoveRoleAsync(accountId, roleId);
            if (!removed)
            {
                throw new KeyNotFoundException("Role assignment not found.");
            }

            try
            {
                if (!string.IsNullOrWhiteSpace(account.UserId))
                {
                    await _notificationService.CreateAndSendAsync(new CreateNotificationDto
                    {
                        UserId = account.UserId,
                        Title = "Account Role Update",
                        Message = $"Role '{targetRole.Name}' has been removed from your account by an administrator.",
                        Type = "System",
                        ReferenceId = accountId
                    });
                }
            }
            catch
            {
                // Non-blocking notification error handling
            }

            await _accountHub.Clients
                .Group(AccountHub.GetAccountGroupName(accountId))
                .SendAsync("ForceLogout", $"Role '{targetRole.Name}' has been removed from your account. Please log in again.");

            return true;
        }
    }
}
