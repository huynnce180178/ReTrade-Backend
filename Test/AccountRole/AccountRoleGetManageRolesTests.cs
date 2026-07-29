using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using FluentAssertions;
using Moq;
using RetradeBE.Mappings;
using RetradeBE.Models;
using RetradeBE.Models.DTOs;
using RetradeBE.Repositories.AccountRole;
using RetradeBE.Services;
using RetradeBE.Services.AccountRole;
using Xunit;

namespace Test.AccountRoleTests
{
    public class AccountRoleGetManageRolesTests
    {
        private readonly Mock<IAccountRoleRepository> _accountRoleRepository;
        private readonly Mock<IAccountService> _accountService;
        private readonly IMapper _mapper;
        private readonly AccountRoleService _service;

        public AccountRoleGetManageRolesTests()
        {
            _accountRoleRepository = new Mock<IAccountRoleRepository>();
            _accountService = new Mock<IAccountService>();

            // Setup real AutoMapper configuration passing NullLoggerFactory as per rules
            var configuration = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<AutoMapperProfile>();
            }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
            _mapper = configuration.CreateMapper();

            _service = new AccountRoleService(
                _accountRoleRepository.Object,
                _accountService.Object,
                _mapper
            );
        }

        #region Normal Tests (N)

        [Fact]
        public async Task GetManageRolesAsync_ShouldReturnManageRoleDtoWithRoles_WhenAccountExists()
        {
            // Arrange
            string accountId = "acc_123";
            var account = new Account { AccountId = accountId, UserId = "user_123" };
            var allRoles = new List<Role>
            {
                new Role { RoleId = 1, Name = "Admin" },
                new Role { RoleId = 2, Name = "User" }
            };
            var userRoles = new List<Role>
            {
                new Role { RoleId = 2, Name = "User" }
            };

            _accountService.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync((Account?)account);
            _accountRoleRepository.Setup(x => x.GetAllRolesAsync()).ReturnsAsync(allRoles);
            _accountRoleRepository.Setup(x => x.GetRolesByAccountIdAsync(accountId)).ReturnsAsync(userRoles);

            // Act
            var result = await _service.GetManageRolesAsync(accountId);

            // Assert
            result.Should().NotBeNull();
            result!.AccountId.Should().Be(accountId);
            result.AllRoles.Should().HaveCount(2);
            result.AllRoles[0].RoleId.Should().Be(1);
            result.AllRoles[0].Name.Should().Be("Admin");
            result.AssignedRole.Should().HaveCount(1);
            result.AssignedRole[0].RoleId.Should().Be(2);
            result.AssignedRole[0].Name.Should().Be("User");

            _accountService.Verify(x => x.GetByIdAsync(accountId), Times.Once);
            _accountRoleRepository.Verify(x => x.GetAllRolesAsync(), Times.Once);
            _accountRoleRepository.Verify(x => x.GetRolesByAccountIdAsync(accountId), Times.Once);
        }

        #endregion

        #region Abnormal Tests (A)

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task GetManageRolesAsync_ShouldThrowKeyNotFoundException_WhenAccountIdIsNullOrWhiteSpace(string? accountId)
        {
            // Act
            Func<Task> act = async () => await _service.GetManageRolesAsync(accountId!);

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("Account does not exist.");
            _accountRoleRepository.Verify(x => x.GetAllRolesAsync(), Times.Never);
            _accountRoleRepository.Verify(x => x.GetRolesByAccountIdAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GetManageRolesAsync_ShouldThrowKeyNotFoundException_WhenAccountDoesNotExist()
        {
            // Arrange
            string accountId = "nonexistent_acc";
            _accountService.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync((Account?)null);

            // Act
            Func<Task> act = async () => await _service.GetManageRolesAsync(accountId);

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("Account does not exist.");
            _accountService.Verify(x => x.GetByIdAsync(accountId), Times.Once);
            _accountRoleRepository.Verify(x => x.GetAllRolesAsync(), Times.Never);
            _accountRoleRepository.Verify(x => x.GetRolesByAccountIdAsync(It.IsAny<string>()), Times.Never);
        }

        #endregion

        #region Boundary Tests (B)

        [Fact]
        public async Task GetManageRolesAsync_ShouldReturnEmptyAssignedRoles_WhenAccountHasNoRolesAssigned()
        {
            // Arrange
            string accountId = "acc_no_roles";
            var account = new Account { AccountId = accountId, UserId = "user_123" };
            var allRoles = new List<Role>
            {
                new Role { RoleId = 1, Name = "Admin" }
            };
            var userRoles = new List<Role>(); // Empty assigned roles

            _accountService.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync((Account?)account);
            _accountRoleRepository.Setup(x => x.GetAllRolesAsync()).ReturnsAsync(allRoles);
            _accountRoleRepository.Setup(x => x.GetRolesByAccountIdAsync(accountId)).ReturnsAsync(userRoles);

            // Act
            var result = await _service.GetManageRolesAsync(accountId);

            // Assert
            result.Should().NotBeNull();
            result!.AccountId.Should().Be(accountId);
            result.AllRoles.Should().HaveCount(1);
            result.AssignedRole.Should().BeEmpty();

            _accountService.Verify(x => x.GetByIdAsync(accountId), Times.Once);
            _accountRoleRepository.Verify(x => x.GetAllRolesAsync(), Times.Once);
            _accountRoleRepository.Verify(x => x.GetRolesByAccountIdAsync(accountId), Times.Once);
        }

        [Fact]
        public async Task GetManageRolesAsync_ShouldReturnEmptyAllRolesAndAssignedRoles_WhenNoRolesExistInSystem()
        {
            // Arrange
            string accountId = "acc_123";
            var account = new Account { AccountId = accountId, UserId = "user_123" };
            var allRoles = new List<Role>(); // Empty system roles
            var userRoles = new List<Role>(); // Empty assigned roles

            _accountService.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync((Account?)account);
            _accountRoleRepository.Setup(x => x.GetAllRolesAsync()).ReturnsAsync(allRoles);
            _accountRoleRepository.Setup(x => x.GetRolesByAccountIdAsync(accountId)).ReturnsAsync(userRoles);

            // Act
            var result = await _service.GetManageRolesAsync(accountId);

            // Assert
            result.Should().NotBeNull();
            result!.AccountId.Should().Be(accountId);
            result.AllRoles.Should().BeEmpty();
            result.AssignedRole.Should().BeEmpty();

            _accountService.Verify(x => x.GetByIdAsync(accountId), Times.Once);
            _accountRoleRepository.Verify(x => x.GetAllRolesAsync(), Times.Once);
            _accountRoleRepository.Verify(x => x.GetRolesByAccountIdAsync(accountId), Times.Once);
        }

        #endregion
    }
}
