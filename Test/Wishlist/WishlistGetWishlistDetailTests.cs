using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using FluentAssertions;
using Moq;
using RetradeBE.Data;
using RetradeBE.Mappings;
using RetradeBE.Models;
using RetradeBE.Models.DTOs;
using RetradeBE.Models.Enums;
using RetradeBE.Repositories;
using RetradeBE.Services;
using Xunit;

namespace Test.WishlistTests
{
    public class WishlistGetWishlistDetailTests
    {
        private readonly Mock<IWishlistRepository> _wishlistRepository;
        private readonly Mock<IAccountRepository> _accountRepository;
        private readonly Mock<AppDbContext> _context;
        private readonly IMapper _mapper;
        private readonly WishlistService _service;

        public WishlistGetWishlistDetailTests()
        {
            _wishlistRepository = new Mock<IWishlistRepository>();
            _accountRepository = new Mock<IAccountRepository>();
            _context = new Mock<AppDbContext>();

            var configuration = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<AutoMapperProfile>();
            }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
            _mapper = configuration.CreateMapper();

            _service = new WishlistService(
                _wishlistRepository.Object,
                _accountRepository.Object,
                _context.Object,
                _mapper
            );
        }

        #region Normal Tests (N)

        [Fact]
        public async Task GetWishlistDetailAsync_ShouldReturnWishlistDetailDto_WhenWishlistExistWithItems()
        {
            // Arrange (UTCID01)
            string accountId = "acc_001";
            string userId = "usr_001";
            string wishlistId = "wl_001";

            var account = new Account
            {
                AccountId = accountId,
                UserId = userId,
                Status = AccountStatusEnum.Active.ToString(),
                IsDeleted = false
            };
            _accountRepository.Setup(r => r.GetByIdAsync(accountId)).ReturnsAsync(account);

            var wishlist = new Wishlist
            {
                WishlistId = wishlistId,
                UserId = userId,
                Status = "Active",
                IsDeleted = false,
                WishlistItem = new List<WishlistItem>
                {
                    new WishlistItem { WishlistItemId = "item_1", ProductId = "prod_1" },
                    new WishlistItem { WishlistItemId = "item_2", ProductId = "prod_2" }
                }
            };
            _wishlistRepository.Setup(r => r.GetOrCreateActiveWishlistAsync(userId)).ReturnsAsync(wishlist);

            // Act
            var result = await _service.GetWishlistDetailAsync(accountId);

            // Assert
            result.Should().NotBeNull();
            result.WishlistId.Should().Be(wishlistId);
            _accountRepository.Verify(r => r.GetByIdAsync(accountId), Times.Once);
            _wishlistRepository.Verify(r => r.GetOrCreateActiveWishlistAsync(userId), Times.Once);
        }

        [Fact]
        public async Task GetWishlistDetailAsync_ShouldReturnWishlist_WhenWishlistNewlyCreatedForUser()
        {
            // Arrange (UTCID02)
            string accountId = "acc_new_user";
            string userId = "usr_new";
            string newWishlistId = "wl_new_auto";

            var account = new Account
            {
                AccountId = accountId,
                UserId = userId,
                Status = AccountStatusEnum.Active.ToString(),
                IsDeleted = false
            };
            _accountRepository.Setup(r => r.GetByIdAsync(accountId)).ReturnsAsync(account);

            var newlyCreatedWishlist = new Wishlist
            {
                WishlistId = newWishlistId,
                UserId = userId,
                Status = "Active",
                IsDeleted = false,
                WishlistItem = new List<WishlistItem>()
            };
            _wishlistRepository.Setup(r => r.GetOrCreateActiveWishlistAsync(userId)).ReturnsAsync(newlyCreatedWishlist);

            // Act
            var result = await _service.GetWishlistDetailAsync(accountId);

            // Assert
            result.Should().NotBeNull();
            result.WishlistId.Should().Be(newWishlistId);
        }

        #endregion

        #region Abnormal Tests (A)

        [Fact]
        public async Task GetWishlistDetailAsync_ShouldThrowException_WhenAccountNotFound()
        {
            // Arrange (UTCID03)
            string accountId = "non_existing_acc";
            _accountRepository.Setup(r => r.GetByIdAsync(accountId)).ReturnsAsync((Account?)null);

            // Act & Assert
            var act = async () => await _service.GetWishlistDetailAsync(accountId);
            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Account does not exist or has been deactivated.");
        }

        #endregion

        #region Boundary Tests (B)

        [Fact]
        public async Task GetWishlistDetailAsync_ShouldReturnWishlist_WhenWishlistItemsIsNull()
        {
            // Arrange (UTCID04)
            string accountId = "acc_001";
            string userId = "usr_001";
            string wishlistId = "wl_null_items";

            var account = new Account
            {
                AccountId = accountId,
                UserId = userId,
                Status = AccountStatusEnum.Active.ToString(),
                IsDeleted = false
            };
            _accountRepository.Setup(r => r.GetByIdAsync(accountId)).ReturnsAsync(account);

            var wishlistWithNullItems = new Wishlist
            {
                WishlistId = wishlistId,
                UserId = userId,
                Status = "Active",
                IsDeleted = false,
                WishlistItem = null
            };
            _wishlistRepository.Setup(r => r.GetOrCreateActiveWishlistAsync(userId)).ReturnsAsync(wishlistWithNullItems);

            // Act
            var result = await _service.GetWishlistDetailAsync(accountId);

            // Assert
            result.Should().NotBeNull();
            result.WishlistId.Should().Be(wishlistId);
        }

        #endregion
    }
}
