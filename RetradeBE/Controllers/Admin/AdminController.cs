using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using RetradeBE.Models.Enums;
using RetradeBE.Services;

namespace RetradeBE.Controllers.Admin
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = nameof(RoleEnum.Admin))]
    public class AdminController : ControllerBase
    {
        private readonly IAccountService _accountService;

        public AdminController(IAccountService accountService)
        {
            _accountService = accountService;
        }

        [HttpGet("user-list")]
        [EnableQuery(PageSize = 20, MaxTop = 100)]
        public IActionResult UserList()
        {
            return Ok(_accountService.QueryUserList());
        }

        [HttpPatch("users/{id}/ban")]
        public async Task<IActionResult> BanUser(string id)
        {
            var account = await _accountService.GetByIdAsync(id);
            if (account == null)
            {
                return NotFound("Account not found.");
            }

            var wasInactive = account.Status == AccountStatusEnum.Inactive.ToString();
            var banned = await _accountService.BanUserAsync(id);
            if (!banned)
            {
                return NotFound("Account not found.");
            }

            var message = wasInactive
                ? "User activated successfully."
                : "User banned successfully.";

            return Ok(new { message });
        }
    }
}