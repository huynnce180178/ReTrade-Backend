using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using FluentAssertions;
using Moq;
using RetradeBE.Models;
using RetradeBE.Repositories.AccountRole;
using RetradeBE.Services;
using RetradeBE.Services.AccountRole;
using Xunit;

namespace Test.AccountTests
{
    public class AccountRoleRemoveTests
    {
        private readonly Mock<IAccountRoleRepository> _roleRepository;
        private readonly Mock<IAccountService> _accountService;
        private readonly AccountRoleService _service;

        public AccountRoleRemoveTests()
        {
            _roleRepository = new Mock<IAccountRoleRepository>();
            _accountService = new Mock<IAccountService>();
            _service = new AccountRoleService(
                _roleRepository.Object,
                _accountService.Object,
                Mock.Of<AutoMapper.IMapper>());
        }

        [Fact]
        public async Task UTCD01_Remove_role_successfully_with_valid_account_ID_and_role_ID()
        {
            var accountId = "A1";
            var roleId = 1;
            var account = new Account { AccountId = accountId };
            var allRoles = new List<Role> { new Role { RoleId = roleId, Name = "Seller" } };

            _accountService.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);
            _roleRepository.Setup(x => x.GetAllRolesAsync()).ReturnsAsync(allRoles);
            _roleRepository.Setup(x => x.RemoveRoleAsync(accountId, roleId)).ReturnsAsync(true);

            var result = await _service.RemoveRoleAsync(accountId, roleId);

            result.Should().BeTrue();
            _accountService.Verify(x => x.GetByIdAsync(accountId), Times.Once);
            _roleRepository.Verify(x => x.RemoveRoleAsync(accountId, roleId), Times.Once);
        }

        [Fact]
        public async Task UTCD02_Remove_role_with_a_null_account_ID_returns_false()
        {
            var result = await _service.RemoveRoleAsync(null!, 1);

            result.Should().BeFalse();
            _accountService.Verify(x => x.GetByIdAsync(It.IsAny<string>()), Times.Never);
            _roleRepository.Verify(x => x.RemoveRoleAsync(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task UTCD03_Remove_role_with_an_invalid_account_ID_returns_false()
        {
            var accountId = "invalid";
            _accountService.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync((Account?)null);

            var result = await _service.RemoveRoleAsync(accountId, 1);

            result.Should().BeFalse();
            _roleRepository.Verify(x => x.GetAllRolesAsync(), Times.Never);
            _roleRepository.Verify(x => x.RemoveRoleAsync(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task UTCD04_Remove_role_with_a_null_role_ID_returns_false()
        {
            var accountId = "A1";
            _accountService.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(new Account { AccountId = accountId });
            _roleRepository.Setup(x => x.GetAllRolesAsync()).ReturnsAsync(new List<Role>());

            var result = await _service.RemoveRoleAsync(accountId, default);

            result.Should().BeFalse();
            _roleRepository.Verify(x => x.GetAllRolesAsync(), Times.Once);
            _roleRepository.Verify(x => x.RemoveRoleAsync(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task UTCD05_Remove_role_with_an_invalid_role_ID_returns_false()
        {
            var accountId = "A1";
            var roleId = 999;
            _accountService.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(new Account { AccountId = accountId });
            _roleRepository.Setup(x => x.GetAllRolesAsync()).ReturnsAsync(new List<Role> { new Role { RoleId = 1, Name = "Seller" } });

            var result = await _service.RemoveRoleAsync(accountId, roleId);

            result.Should().BeFalse();
            _roleRepository.Verify(x => x.RemoveRoleAsync(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
        }
    }
}
