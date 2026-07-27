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
    public class FollowSellerTests
    {
        private readonly Mock<IProfileRepository> _profileRepository;
        private readonly Mock<IAccountRepository> _accountRepository;
        private readonly Mock<IHubContext<SellerHub>> _sellerHub;
        private readonly Mock<IHubClients> _hubClients;
        private readonly Mock<IClientProxy> _clientProxy;
        private readonly Mock<IMapper> _mapper;
        private readonly Mock<AppDbContext> _context;
        private readonly ProfileService _service;

        public FollowSellerTests()
        {
            _profileRepository = new Mock<IProfileRepository>();
            _accountRepository = new Mock<IAccountRepository>();
            _sellerHub = new Mock<IHubContext<SellerHub>>();
            _hubClients = new Mock<IHubClients>();
            _clientProxy = new Mock<IClientProxy>();
            _mapper = new Mock<IMapper>();
            _context = new Mock<AppDbContext>();

            _hubClients.Setup(x => x.Group(It.IsAny<string>())).Returns(_clientProxy.Object);
            _sellerHub.SetupGet(x => x.Clients).Returns(_hubClients.Object);
            _clientProxy
                .Setup(x => x.SendCoreAsync(It.IsAny<string>(), It.IsAny<object?[]>(), default))
                .Returns(Task.CompletedTask);

            _service = new ProfileService(
                _profileRepository.Object,
                _accountRepository.Object,
                _sellerHub.Object,
                _mapper.Object,
                _context.Object);
        }

        [Fact]
        public async Task UTCD01_Follow_seller_successfully()
        {
            var buyerAccountId = "buyer-account";
            var buyerUserId = "buyer-user";
            var sellerUserId = "seller-user";
            var sellerAccountId = "seller-account";

            var buyerAccount = new Account
            {
                AccountId = buyerAccountId,
                UserId = buyerUserId,
                Username = "buyer"
            };

            var sellerUser = new User
            {
                UserId = sellerUserId,
                IsDeleted = false
            };

            var sellerAccount = new Account
            {
                AccountId = sellerAccountId,
                UserId = sellerUserId,
                Username = "seller",
                Status = RetradeBE.Models.Enums.AccountStatusEnum.Active.ToString()
            };

            _profileRepository.Setup(x => x.GetAccountWithUserAsync(buyerAccountId)).ReturnsAsync((Account?)buyerAccount);
            _profileRepository.Setup(x => x.GetUserByIdAsync(sellerUserId)).ReturnsAsync(sellerUser);
            _profileRepository.Setup(x => x.GetPrimaryAccountByUserIdAsync(sellerUserId)).ReturnsAsync((Account?)sellerAccount);
            _accountRepository.Setup(x => x.GetRolesAsync(buyerAccountId)).ReturnsAsync(new List<string> { "User" });
            _accountRepository.Setup(x => x.GetRolesAsync(sellerAccountId)).ReturnsAsync(new List<string> { "Seller" });
            _profileRepository.Setup(x => x.FollowExistsAsync(buyerUserId, sellerUserId)).ReturnsAsync(false);
            _profileRepository.Setup(x => x.AddFollowAsync(It.IsAny<UserFollow>())).Returns(Task.CompletedTask);
            _profileRepository.Setup(x => x.CountFollowersAsync(sellerUserId)).ReturnsAsync(1);

            var result = await _service.FollowSellerAsync(buyerAccountId, sellerUserId);

            result.Should().NotBeNull();
            result!.IsFollowing.Should().BeTrue();
            result.SellerId.Should().Be(sellerUserId);
            result.FollowerId.Should().Be(buyerUserId);
            result.Message.Should().Be("Follow seller successfully.");

            _profileRepository.Verify(x => x.AddFollowAsync(It.Is<UserFollow>(f => f.FollowerId == buyerUserId && f.FollowedUserId == sellerUserId)), Times.Once);
            _hubClients.Verify(x => x.Group(SellerHub.GetSellerGroupName(sellerUserId)), Times.Once);
            _clientProxy.Verify(x => x.SendCoreAsync("SellerFollowChanged", It.IsAny<object?[]>(), default), Times.Once);
        }

        [Fact]
        public async Task UTCD02_Unfollow_seller_successfully()
        {
            var buyerAccountId = "buyer-account";
            var buyerUserId = "buyer-user";
            var sellerUserId = "seller-user";
            var sellerAccountId = "seller-account";

            var buyerAccount = new Account
            {
                AccountId = buyerAccountId,
                UserId = buyerUserId,
                Username = "buyer"
            };

            var sellerUser = new User
            {
                UserId = sellerUserId,
                IsDeleted = false
            };

            var sellerAccount = new Account
            {
                AccountId = sellerAccountId,
                UserId = sellerUserId,
                Username = "seller",
                Status = RetradeBE.Models.Enums.AccountStatusEnum.Active.ToString()
            };

            var follow = new UserFollow
            {
                FollowId = "UF1",
                FollowerId = buyerUserId,
                FollowedUserId = sellerUserId
            };

            _profileRepository.Setup(x => x.GetAccountWithUserAsync(buyerAccountId)).ReturnsAsync((Account?)buyerAccount);
            _profileRepository.Setup(x => x.GetUserByIdAsync(sellerUserId)).ReturnsAsync(sellerUser);
            _profileRepository.Setup(x => x.GetPrimaryAccountByUserIdAsync(sellerUserId)).ReturnsAsync((Account?)sellerAccount);
            _accountRepository.Setup(x => x.GetRolesAsync(buyerAccountId)).ReturnsAsync(new List<string> { "User" });
            _accountRepository.Setup(x => x.GetRolesAsync(sellerAccountId)).ReturnsAsync(new List<string> { "Seller" });
            _profileRepository.Setup(x => x.GetFollowAsync(buyerUserId, sellerUserId)).ReturnsAsync(follow);
            _profileRepository.Setup(x => x.CountFollowersAsync(sellerUserId)).ReturnsAsync(0);
            _profileRepository.Setup(x => x.RemoveFollowAsync(follow)).Returns(Task.CompletedTask);

            var result = await _service.UnfollowSellerAsync(buyerAccountId, sellerUserId);

            result.Should().NotBeNull();
            result!.IsFollowing.Should().BeFalse();
            result.SellerId.Should().Be(sellerUserId);
            result.FollowerId.Should().Be(buyerUserId);
            result.Message.Should().Be("Unfollow seller successfully.");

            _profileRepository.Verify(x => x.RemoveFollowAsync(It.Is<UserFollow>(f => f.FollowId == follow.FollowId)), Times.Once);
            _hubClients.Verify(x => x.Group(SellerHub.GetSellerGroupName(sellerUserId)), Times.Once);
            _clientProxy.Verify(x => x.SendCoreAsync("SellerFollowChanged", It.IsAny<object?[]>(), default), Times.Once);
        }

        [Fact]
        public async Task UTCD03_Follow_seller_with_an_invalid_buyer_account()
        {
            var buyerAccountId = "invalid-buyer";
            var sellerUserId = "seller-user";

            _profileRepository.Setup(x => x.GetAccountWithUserAsync(buyerAccountId)).ReturnsAsync((Account?)null);

            var result = await _service.FollowSellerAsync(buyerAccountId, sellerUserId);

            result.Should().BeNull();
            _profileRepository.Verify(x => x.GetUserByIdAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task UTCD04_Follow_seller_with_an_invalid_seller_account()
        {
            var buyerAccountId = "buyer-account";
            var buyerUserId = "buyer-user";
            var sellerId = "invalid-seller";

            var buyerAccount = new Account
            {
                AccountId = buyerAccountId,
                UserId = buyerUserId,
                Username = "buyer"
            };

            _profileRepository.Setup(x => x.GetAccountWithUserAsync(buyerAccountId)).ReturnsAsync((Account?)buyerAccount);
            _profileRepository.Setup(x => x.GetUserByIdAsync(sellerId)).ReturnsAsync((User?)null);
            _profileRepository.Setup(x => x.GetAccountWithUserAsync(sellerId)).ReturnsAsync((Account?)null);
            _accountRepository.Setup(x => x.GetRolesAsync(buyerAccountId)).ReturnsAsync(new List<string> { "User" });

            var result = await _service.FollowSellerAsync(buyerAccountId, sellerId);

            result.Should().BeNull();
            _profileRepository.Verify(x => x.GetUserByIdAsync(sellerId), Times.Exactly(2));
            _profileRepository.Verify(x => x.GetAccountWithUserAsync(sellerId), Times.Once);
        }

        [Fact]
        public async Task UTCD05_Attempt_to_follow_yourself()
        {
            var buyerAccountId = "buyer-account";
            var buyerUserId = "buyer-user";
            var sellerAccountId = "seller-account";

            var buyerAccount = new Account
            {
                AccountId = buyerAccountId,
                UserId = buyerUserId,
                Username = "buyer"
            };

            var buyerUser = new User
            {
                UserId = buyerUserId,
                IsDeleted = false
            };

            _profileRepository.Setup(x => x.GetAccountWithUserAsync(buyerAccountId)).ReturnsAsync((Account?)buyerAccount);
            _profileRepository.Setup(x => x.GetUserByIdAsync(buyerUserId)).ReturnsAsync(buyerUser);
            _profileRepository.Setup(x => x.GetPrimaryAccountByUserIdAsync(buyerUserId)).ReturnsAsync((Account?)new Account
            {
                AccountId = sellerAccountId,
                UserId = buyerUserId,
                Username = "buyer"
            });
            _accountRepository.Setup(x => x.GetRolesAsync(buyerAccountId)).ReturnsAsync(new List<string> { "User" });

            var act = async () => await _service.FollowSellerAsync(buyerAccountId, buyerUserId);

            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("You cannot follow yourself.");
            _profileRepository.Verify(x => x.AddFollowAsync(It.IsAny<UserFollow>()), Times.Never);
        }

        [Fact]
        public async Task UTCD06_Follow_seller_with_a_null_account_or_seller_ID()
        {
            var buyerAccountId = "buyer-account";
            var buyerUserId = "buyer-user";
            var sellerUserId = "seller-user";
            var sellerAccountId = "seller-account";

            var buyerAccount = new Account
            {
                AccountId = buyerAccountId,
                UserId = buyerUserId,
                Username = "buyer"
            };

            _profileRepository.Setup(x => x.GetAccountWithUserAsync(buyerAccountId)).ReturnsAsync((Account?)buyerAccount);
            _profileRepository.Setup(x => x.GetUserByIdAsync(string.Empty)).ReturnsAsync((User?)null);
            _profileRepository.Setup(x => x.GetAccountWithUserAsync(string.Empty)).ReturnsAsync((Account?)null);
            _accountRepository.Setup(x => x.GetRolesAsync(buyerAccountId)).ReturnsAsync(new List<string> { "User" });

            var resultEmptyAccount = await _service.FollowSellerAsync(string.Empty, sellerUserId);
            var resultEmptySeller = await _service.FollowSellerAsync(buyerAccountId, string.Empty);

            resultEmptyAccount.Should().BeNull();
            resultEmptySeller.Should().BeNull();
        }
    }
}
