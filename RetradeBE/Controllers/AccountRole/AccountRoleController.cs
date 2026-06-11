using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetradeBE.Models.Enums;
using RetradeBE.Services.AccountRole;

namespace RetradeBE.Controllers.AccountRole
{
    [Route("api/account-roles")]
    [ApiController]
    [Authorize(Roles = nameof(RoleEnum.Admin))]
    public class AccountRoleController : ControllerBase
    {
        private readonly IAccountRoleService _accountRoleService;

        public AccountRoleController(IAccountRoleService accountRoleService)
        {
            _accountRoleService = accountRoleService;
        }

        /// Get all roles and assigned roles of an account
        [HttpGet("{accountId}")]
        public async Task<IActionResult> GetManageRoles(string accountId)
        {
            var result = await _accountRoleService.GetManageRolesAsync(accountId);

            if (result == null)
            {
                return NotFound(new
                {
                    Message = "Account not found."
                });
            }

            return Ok(result);
        }

        /// Assign role to account
        [HttpPost("{accountId}/roles/{roleId}")]
        public async Task<IActionResult> AssignRole(string accountId, int roleId)
        {
            var result = await _accountRoleService.AssignRoleAsync(accountId, roleId);

            if (!result)
            {
                return BadRequest(new
                {
                    Message = "Role is already assigned to this account."
                });
            }

            return Ok(new
            {
                Message = "Role assigned successfully."
            });
        }

        /// Remove role from account
        [HttpDelete("{accountId}/roles/{roleId}")]
        public async Task<IActionResult> RemoveRole(string accountId, int roleId)
        {
            var result = await _accountRoleService.RemoveRoleAsync(accountId, roleId);

            if (!result)
            {
                return NotFound(new
                {
                    Message = "Role assignment not found."
                });
            }

            return Ok(new
            {
                Message = "Role removed successfully."
            });
        }
    }
}
