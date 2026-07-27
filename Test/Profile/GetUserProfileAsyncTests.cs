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
    public class GetUserProfileAsyncTests
    {
        private readonly Mock<IProfileRepository> _profileRepository;
        private readonly Mock<IAccountRepository> _accountRepository;
        private readonly Mock<IHubContext<SellerHub>> _sellerHub;
        private readonly Mock<IMapper> _mapper;
        private readonly Mock<AppDbContext> _context;
        private readonly ProfileService _service;

        public GetUserProfileAsyncTests()
        {
            _profileRepository = new Mock<IProfileRepository>();
            _accountRepository = new Mock<IAccountRepository>();
            _sellerHub = new Mock<IHubContext<SellerHub>>();
            _mapper = new Mock<IMapper>();
            _context = new Mock<AppDbContext>();

            _service = new ProfileService(
                _profileRepository.Object,
                _accountRepository.Object,
                _sellerHub.Object,
                _mapper.Object,
                _context.Object);
        }

        [Fact]
        public async Task UTCD01_Get_user_profile_successfully_with_a_valid_active_user_ID()
        {
            var userId = "U1";
            var user = new User
            {
                UserId = userId,
                Email = "active@example.com",
                FirstName = "Active",
                LastName = "User",
                IsDeleted = false
            };
            var account = new Account
            {
                AccountId = "A1",
                UserId = userId,
                Username = "activeuser",
                Status = RetradeBE.Models.Enums.AccountStatusEnum.Active.ToString(),
                CreatedAt = System.DateTime.UtcNow.AddDays(-10),
                UpdatedAt = System.DateTime.UtcNow
            };
            var address = new Address
            {
                AddressId = "Addr1",
                UserId = userId,
                ReceiverName = "Receiver",
                Street = "123 Main St",
                IsDefault = true,
                Status = "Active"
            };
            var addresses = new List<Address> { address };
            var roles = new List<string> { "User" };

            _profileRepository.Setup(x => x.GetUserByIdAsync(userId)).ReturnsAsync(user);
            _profileRepository.Setup(x => x.GetPrimaryAccountByUserIdAsync(userId)).ReturnsAsync((Account?)account);
            _profileRepository.Setup(x => x.GetActiveAddressesByUserIdAsync(userId)).ReturnsAsync(addresses);
            _accountRepository.Setup(x => x.GetRolesAsync(account.AccountId)).ReturnsAsync(roles);

            _mapper.Setup(x => x.Map<ProfileDetailDto>(account)).Returns(new ProfileDetailDto
            {
                AccountId = account.AccountId,
                UserId = account.UserId,
                Username = account.Username,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Phone = user.Phone,
                Status = account.Status,
                CreatedAt = account.CreatedAt,
                UpdatedAt = account.UpdatedAt
            });
            _mapper.Setup(x => x.Map<List<AddressDto>>(addresses)).Returns(new List<AddressDto>
            {
                new AddressDto
                {
                    AddressId = address.AddressId,
                    ReceiverName = address.ReceiverName,
                    StreetAddress = address.Street,
                    IsDefault = address.IsDefault,
                    Status = address.Status
                }
            });
            _mapper.Setup(x => x.Map<AddressDto>(address)).Returns(new AddressDto
            {
                AddressId = address.AddressId,
                ReceiverName = address.ReceiverName,
                StreetAddress = address.Street,
                IsDefault = address.IsDefault,
                Status = address.Status
            });

            var result = await _service.GetUserProfileAsync(userId);

            result.Should().NotBeNull();
            result!.AccountId.Should().Be(account.AccountId);
            result.UserId.Should().Be(userId);
            result.Status.Should().Be(account.Status);
            result.Email.Should().Be(user.Email);
            result.Roles.Should().BeEquivalentTo(roles);
            result.DefaultAddress.Should().NotBeNull();
            result.Addresses.Should().HaveCount(1);

            _profileRepository.Verify(x => x.GetUserByIdAsync(userId), Times.Once);
            _profileRepository.Verify(x => x.GetPrimaryAccountByUserIdAsync(userId), Times.Once);
            _profileRepository.Verify(x => x.GetActiveAddressesByUserIdAsync(userId), Times.Once);
            _accountRepository.Verify(x => x.GetRolesAsync(account.AccountId), Times.Once);
        }

        [Fact]
        public async Task UTCD02_Get_user_profile_with_a_banned_user_account()
        {
            var userId = "U2";
            var user = new User
            {
                UserId = userId,
                Email = "banned@example.com",
                FirstName = "Banned",
                LastName = "User",
                IsDeleted = false
            };
            var account = new Account
            {
                AccountId = "A2",
                UserId = userId,
                Username = "banneduser",
                Status = RetradeBE.Models.Enums.AccountStatusEnum.Ban.ToString(),
                CreatedAt = System.DateTime.UtcNow.AddDays(-20),
                UpdatedAt = System.DateTime.UtcNow
            };
            var address = new Address
            {
                AddressId = "Addr2",
                UserId = userId,
                ReceiverName = "Receiver",
                Street = "456 Oak St",
                IsDefault = true,
                Status = "Active"
            };
            var addresses = new List<Address> { address };
            var roles = new List<string> { "User" };

            _profileRepository.Setup(x => x.GetUserByIdAsync(userId)).ReturnsAsync(user);
            _profileRepository.Setup(x => x.GetPrimaryAccountByUserIdAsync(userId)).ReturnsAsync((Account?)account);
            _profileRepository.Setup(x => x.GetActiveAddressesByUserIdAsync(userId)).ReturnsAsync(addresses);
            _accountRepository.Setup(x => x.GetRolesAsync(account.AccountId)).ReturnsAsync(roles);

            _mapper.Setup(x => x.Map<ProfileDetailDto>(account)).Returns(new ProfileDetailDto
            {
                AccountId = account.AccountId,
                UserId = account.UserId,
                Username = account.Username,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Phone = user.Phone,
                Status = account.Status,
                CreatedAt = account.CreatedAt,
                UpdatedAt = account.UpdatedAt
            });
            _mapper.Setup(x => x.Map<List<AddressDto>>(addresses)).Returns(new List<AddressDto>
            {
                new AddressDto
                {
                    AddressId = address.AddressId,
                    ReceiverName = address.ReceiverName,
                    StreetAddress = address.Street,
                    IsDefault = address.IsDefault,
                    Status = address.Status
                }
            });
            _mapper.Setup(x => x.Map<AddressDto>(address)).Returns(new AddressDto
            {
                AddressId = address.AddressId,
                ReceiverName = address.ReceiverName,
                StreetAddress = address.Street,
                IsDefault = address.IsDefault,
                Status = address.Status
            });

            var result = await _service.GetUserProfileAsync(userId);

            result.Should().NotBeNull();
            result!.AccountId.Should().Be(account.AccountId);
            result.Status.Should().Be(account.Status);
            result.Email.Should().Be(user.Email);
            result.Roles.Should().BeEquivalentTo(roles);
            result.DefaultAddress.Should().NotBeNull();

            _profileRepository.Verify(x => x.GetUserByIdAsync(userId), Times.Once);
            _profileRepository.Verify(x => x.GetPrimaryAccountByUserIdAsync(userId), Times.Once);
            _profileRepository.Verify(x => x.GetActiveAddressesByUserIdAsync(userId), Times.Once);
            _accountRepository.Verify(x => x.GetRolesAsync(account.AccountId), Times.Once);
        }

        [Fact]
        public async Task UTCD03_Get_user_profile_with_an_invalid_user_ID()
        {
            var userId = "invalid";
            _profileRepository.Setup(x => x.GetUserByIdAsync(userId)).ReturnsAsync((User?)null);

            var result = await _service.GetUserProfileAsync(userId);

            result.Should().BeNull();
            _profileRepository.Verify(x => x.GetUserByIdAsync(userId), Times.Once);
            _profileRepository.Verify(x => x.GetPrimaryAccountByUserIdAsync(It.IsAny<string>()), Times.Never);
            _profileRepository.Verify(x => x.GetActiveAddressesByUserIdAsync(It.IsAny<string>()), Times.Never);
            _accountRepository.Verify(x => x.GetRolesAsync(It.IsAny<string>()), Times.Never);
        }
    }
}
