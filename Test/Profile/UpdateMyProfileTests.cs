using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Moq;
using RetradeBE.Data;
using RetradeBE.Hubs;
using RetradeBE.Models;
using RetradeBE.Models.DTOs;
using RetradeBE.Repositories;
using RetradeBE.Services;
using Xunit;

namespace Test.Profile
{
    public class UpdateMyProfileTests
    {
        private readonly Mock<IProfileRepository> _profileRepository;
        private readonly Mock<IAccountRepository> _accountRepository;
        private readonly Mock<IHubContext<SellerHub>> _sellerHub;
        private readonly Mock<IMapper> _mapper;
        private readonly Mock<AppDbContext> _context;
        private readonly Mock<ISubscriptionVoucherService> _subscriptionVoucherService;
        private readonly ProfileService _service;

        public UpdateMyProfileTests()
        {
            _profileRepository = new Mock<IProfileRepository>();
            _accountRepository = new Mock<IAccountRepository>();
            _sellerHub = new Mock<IHubContext<SellerHub>>();
            _mapper = new Mock<IMapper>();
            _context = new Mock<AppDbContext>();
            _subscriptionVoucherService = new Mock<ISubscriptionVoucherService>();

            _service = new ProfileService(
                _profileRepository.Object,
                _accountRepository.Object,
                _sellerHub.Object,
                _mapper.Object,
                _context.Object,
                _subscriptionVoucherService.Object);
        }

        [Fact]
        public async Task UTCD01_Update_profile_successfully_with_valid_profile_information()
        {
            var accountId = "A1";
            var userId = "U1";
            var user = new User
            {
                UserId = userId,
                Email = "old@example.com",
                FirstName = "Old",
                LastName = "Name",
                IsDeleted = false
            };
            var account = new Account
            {
                AccountId = accountId,
                UserId = userId,
                Username = "oldusername",
                Status = RetradeBE.Models.Enums.AccountStatusEnum.Active.ToString(),
                CreatedAt = System.DateTime.UtcNow.AddDays(-10),
                UpdatedAt = System.DateTime.UtcNow.AddDays(-5),
                User = user
            };
            var dto = new ProfileUpdateDto
            {
                Username = "newusername",
                Email = "new@example.com",
                FirstName = "New",
                LastName = "Name",
                Phone = "0123456789"
            };
            var addresses = new List<Address>();
            var roles = new List<string> { "User" };

            _profileRepository.Setup(x => x.GetAccountWithUserAsync(accountId)).ReturnsAsync((Account?)account);
            _profileRepository.Setup(x => x.UsernameExistsAsync(dto.Username.Trim(), accountId)).ReturnsAsync(false);
            _profileRepository.Setup(x => x.EmailExistsAsync(dto.Email.Trim(), userId)).ReturnsAsync(false);
            _profileRepository.Setup(x => x.UpdateAccountAsync(account)).Returns(Task.CompletedTask);
            _profileRepository.Setup(x => x.UpdateUserAsync(account.User)).Returns(Task.CompletedTask);
            _profileRepository.Setup(x => x.GetActiveAddressesByUserIdAsync(userId)).ReturnsAsync(addresses);
            _accountRepository.Setup(x => x.GetRolesAsync(accountId)).ReturnsAsync(roles);
            _mapper.Setup(x => x.Map<ProfileDetailDto>(It.IsAny<Account>())).Returns((Account acct) => new ProfileDetailDto
            {
                AccountId = acct.AccountId,
                UserId = acct.UserId,
                Username = acct.Username,
                Email = acct.User.Email,
                FirstName = acct.User.FirstName,
                LastName = acct.User.LastName,
                Phone = acct.User.Phone,
                Status = acct.Status,
                CreatedAt = acct.CreatedAt,
                UpdatedAt = acct.UpdatedAt
            });
            _mapper.Setup(x => x.Map<List<AddressDto>>(addresses)).Returns(new List<AddressDto>());

            var result = await _service.UpdateMyProfileAsync(accountId, dto);

            result.Should().NotBeNull();
            result!.Username.Should().Be(dto.Username);
            result.Email.Should().Be(dto.Email);
            result.FirstName.Should().Be(dto.FirstName);
            result.LastName.Should().Be(dto.LastName);
            result.Phone.Should().Be(dto.Phone);
            result.Roles.Should().BeEquivalentTo(roles);

            _profileRepository.Verify(x => x.UpdateAccountAsync(It.Is<Account>(a => a.AccountId == accountId && a.Username == dto.Username)), Times.Once);
            _profileRepository.Verify(x => x.UpdateUserAsync(It.Is<User>(u => u.UserId == userId && u.Email == dto.Email && u.FirstName == dto.FirstName && u.LastName == dto.LastName && u.Phone == dto.Phone)), Times.Once);
        }

        [Fact]
        public async Task UTCD02_Update_profile_with_an_invalid_account_ID()
        {
            var accountId = "invalid";
            var dto = new ProfileUpdateDto
            {
                FirstName = "New",
                LastName = "Name"
            };

            _profileRepository.Setup(x => x.GetAccountWithUserAsync(accountId)).ReturnsAsync((Account?)null);

            var result = await _service.UpdateMyProfileAsync(accountId, dto);

            result.Should().BeNull();
            _profileRepository.Verify(x => x.GetAccountWithUserAsync(accountId), Times.Once);
            _profileRepository.Verify(x => x.UpdateAccountAsync(It.IsAny<Account>()), Times.Never);
            _profileRepository.Verify(x => x.UpdateUserAsync(It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public async Task UTCD03_Update_profile_with_a_null_account_ID()
        {
            string? accountId = null;
            var dto = new ProfileUpdateDto
            {
                FirstName = "New",
                LastName = "Name"
            };

            var result = await _service.UpdateMyProfileAsync(accountId!, dto);

            result.Should().BeNull();
            _profileRepository.Verify(x => x.GetAccountWithUserAsync(null!), Times.Once);
        }

        [Fact]
        public async Task UTCD04_Update_profile_with_an_existing_username()
        {
            var accountId = "A1";
            var userId = "U1";
            var user = new User
            {
                UserId = userId,
                Email = "me@example.com",
                FirstName = "Me",
                LastName = "User",
                IsDeleted = false
            };
            var account = new Account
            {
                AccountId = accountId,
                UserId = userId,
                Username = "oldusername",
                User = user
            };
            var dto = new ProfileUpdateDto
            {
                Username = "existingusername"
            };

            _profileRepository.Setup(x => x.GetAccountWithUserAsync(accountId)).ReturnsAsync((Account?)account);
            _profileRepository.Setup(x => x.UsernameExistsAsync(dto.Username.Trim(), accountId)).ReturnsAsync(true);

            var act = async () => await _service.UpdateMyProfileAsync(accountId, dto);

            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Username already exists.");
            _profileRepository.Verify(x => x.UpdateAccountAsync(It.IsAny<Account>()), Times.Never);
            _profileRepository.Verify(x => x.UpdateUserAsync(It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public async Task UTCD05_Update_profile_with_an_existing_email_address()
        {
            var accountId = "A1";
            var userId = "U1";
            var user = new User
            {
                UserId = userId,
                Email = "old@example.com",
                FirstName = "Me",
                LastName = "User",
                IsDeleted = false
            };
            var account = new Account
            {
                AccountId = accountId,
                UserId = userId,
                Username = "oldusername",
                User = user
            };
            var dto = new ProfileUpdateDto
            {
                Email = "existing@example.com"
            };

            _profileRepository.Setup(x => x.GetAccountWithUserAsync(accountId)).ReturnsAsync((Account?)account);
            _profileRepository.Setup(x => x.EmailExistsAsync(dto.Email.Trim(), userId)).ReturnsAsync(true);

            var act = async () => await _service.UpdateMyProfileAsync(accountId, dto);

            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Email already exists.");
            _profileRepository.Verify(x => x.UpdateAccountAsync(It.IsAny<Account>()), Times.Never);
            _profileRepository.Verify(x => x.UpdateUserAsync(It.IsAny<User>()), Times.Never);
        }
    }
}
