using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using FluentAssertions;
using Moq;
using RetradeBE.Data;
using RetradeBE.Mappings;
using RetradeBE.Models;
using RetradeBE.Models.Enums;
using RetradeBE.Repositories;
using RetradeBE.Services;
using Xunit;

namespace Test.WishlistTests
{
    public class WishlistRemoveWishlistItemTests
    {
        private readonly Mock<IWishlistRepository> _wishlistRepository;
        private readonly Mock<IAccountRepository> _accountRepository;
        private readonly Mock<AppDbContext> _context;
        private readonly IMapper _mapper;
        private readonly WishlistService _service;

        public WishlistRemoveWishlistItemTests()
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
        public async Task RemoveWishlistItemAsync_ShouldRemoveItem_WhenDataIsValid()
        {
            // Arrange (UTCID01)
            string accountId = "acc_001";
            string userId = "usr_001";
            string wishlistId = "wl_001";
            string wishlistItemId = "item_001";

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
                IsDeleted = false
            };
            _wishlistRepository.Setup(r => r.GetOrCreateActiveWishlistAsync(userId)).ReturnsAsync(wishlist);

            var item = new WishlistItem
            {
                WishlistItemId = wishlistItemId,
                WishlistId = wishlistId,
                ProductId = "prod_001"
            };
            _wishlistRepository.Setup(r => r.GetItemByIdAsync(wishlistItemId)).ReturnsAsync(item);
            _wishlistRepository.Setup(r => r.RemoveItemAsync(item)).Returns(Task.CompletedTask);

            var mockWishlistDbSet = new List<Wishlist> { wishlist }.AsMockDbSet();
            _context.Setup(c => c.Wishlist).Returns(mockWishlistDbSet.Object);
            _context.Setup(c => c.SaveChangesAsync(default)).ReturnsAsync(1);

            // Act
            var act = async () => await _service.RemoveWishlistItemAsync(accountId, wishlistItemId);

            // Assert
            await act.Should().NotThrowAsync();
            _wishlistRepository.Verify(r => r.RemoveItemAsync(item), Times.Once);
            _context.Verify(c => c.SaveChangesAsync(default), Times.Once);
        }

        #endregion

        #region Abnormal Tests (A)

        [Fact]
        public async Task RemoveWishlistItemAsync_ShouldThrowException_WhenAccountNotFound()
        {
            // Arrange (UTCID02)
            string accountId = "non_existing_acc";
            string wishlistItemId = "item_001";
            _accountRepository.Setup(r => r.GetByIdAsync(accountId)).ReturnsAsync((Account?)null);

            // Act & Assert
            var act = async () => await _service.RemoveWishlistItemAsync(accountId, wishlistItemId);
            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Account does not exist or has been deactivated.");
        }

        [Fact]
        public async Task RemoveWishlistItemAsync_ShouldThrowException_WhenItemNotFound()
        {
            // Arrange (UTCID03)
            string accountId = "acc_001";
            string userId = "usr_001";
            string wishlistItemId = "non_existing_item";

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
                WishlistId = "wl_001",
                UserId = userId,
                Status = "Active",
                IsDeleted = false
            };
            _wishlistRepository.Setup(r => r.GetOrCreateActiveWishlistAsync(userId)).ReturnsAsync(wishlist);

            _wishlistRepository.Setup(r => r.GetItemByIdAsync(wishlistItemId)).ReturnsAsync((WishlistItem?)null);

            // Act & Assert
            var act = async () => await _service.RemoveWishlistItemAsync(accountId, wishlistItemId);
            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Wishlist item not found.");
        }

        [Fact]
        public async Task RemoveWishlistItemAsync_ShouldThrowException_WhenItemBelongsToAnotherWishlist()
        {
            // Arrange (UTCID04)
            string accountId = "acc_001";
            string userId = "usr_001";
            string wishlistItemId = "item_001";

            var account = new Account
            {
                AccountId = accountId,
                UserId = userId,
                Status = AccountStatusEnum.Active.ToString(),
                IsDeleted = false
            };
            _accountRepository.Setup(r => r.GetByIdAsync(accountId)).ReturnsAsync(account);

            var userWishlist = new Wishlist
            {
                WishlistId = "wl_my_wishlist",
                UserId = userId,
                Status = "Active",
                IsDeleted = false
            };
            _wishlistRepository.Setup(r => r.GetOrCreateActiveWishlistAsync(userId)).ReturnsAsync(userWishlist);

            var itemFromOtherWishlist = new WishlistItem
            {
                WishlistItemId = wishlistItemId,
                WishlistId = "wl_other_user_wishlist",
                ProductId = "prod_001"
            };
            _wishlistRepository.Setup(r => r.GetItemByIdAsync(wishlistItemId)).ReturnsAsync(itemFromOtherWishlist);

            // Act & Assert
            var act = async () => await _service.RemoveWishlistItemAsync(accountId, wishlistItemId);
            await act.Should().ThrowAsync<Exception>()
                .WithMessage("You do not have permission to remove this item.");
        }

        #endregion

        #region Boundary Tests (B)

        [Fact]
        public async Task RemoveWishlistItemAsync_ShouldHandleBoundary_WhenWishlistItemIdIsEmptyString()
        {
            // Arrange (UTCID05)
            string accountId = "acc_001";
            string userId = "usr_001";
            string wishlistItemId = ""; // Chuỗi rỗng (Boundary)

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
                WishlistId = "wl_001",
                UserId = userId,
                Status = "Active",
                IsDeleted = false
            };
            _wishlistRepository.Setup(r => r.GetOrCreateActiveWishlistAsync(userId)).ReturnsAsync(wishlist);

            _wishlistRepository.Setup(r => r.GetItemByIdAsync(wishlistItemId)).ReturnsAsync((WishlistItem?)null);

            // Act & Assert
            var act = async () => await _service.RemoveWishlistItemAsync(accountId, wishlistItemId);
            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Wishlist item not found.");
        }

        #endregion
    }
}
