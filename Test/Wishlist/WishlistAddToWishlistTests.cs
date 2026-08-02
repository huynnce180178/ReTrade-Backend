using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
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
    public class WishlistAddToWishlistTests
    {
        private readonly Mock<IWishlistRepository> _wishlistRepository;
        private readonly Mock<IAccountRepository> _accountRepository;
        private readonly Mock<AppDbContext> _context;
        private readonly IMapper _mapper;
        private readonly WishlistService _service;

        public WishlistAddToWishlistTests()
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
        public async Task AddToWishlistAsync_ShouldAddProductToWishlist_WhenDataIsValid()
        {
            // Arrange (UTCID01)
            string accountId = "acc_001";
            string userId = "usr_001";
            string sellerId = "usr_seller_999";
            string productId = "prod_001";
            string wishlistId = "wl_001";

            var account = new Account
            {
                AccountId = accountId,
                UserId = userId,
                Status = AccountStatusEnum.Active.ToString(),
                IsDeleted = false
            };
            _accountRepository.Setup(r => r.GetByIdAsync(accountId)).ReturnsAsync(account);

            var productList = new List<RetradeBE.Models.Product>
            {
                new RetradeBE.Models.Product
                {
                    ProductId = productId,
                    SellerId = sellerId,
                    Status = ProductStatusEnum.Accepted.ToString(),
                    IsDeleted = false
                }
            };
            _context.Setup(c => c.Product).Returns(productList.AsMockDbSet().Object);

            var wishlist = new Wishlist
            {
                WishlistId = wishlistId,
                UserId = userId,
                Status = "Active",
                IsDeleted = false,
                WishlistItem = new List<WishlistItem>()
            };

            _wishlistRepository.Setup(r => r.GetOrCreateActiveWishlistAsync(userId)).ReturnsAsync(wishlist);
            _wishlistRepository.Setup(r => r.IsProductInWishlistAsync(wishlistId, productId)).ReturnsAsync(false);
            _wishlistRepository.Setup(r => r.AddItemAsync(It.IsAny<WishlistItem>())).Returns(Task.CompletedTask);

            var mockWishlistDbSet = new List<Wishlist> { wishlist }.AsMockDbSet();
            _context.Setup(c => c.Wishlist).Returns(mockWishlistDbSet.Object);
            _context.Setup(c => c.SaveChangesAsync(default)).ReturnsAsync(1);

            var dto = new AddToWishlistDto { ProductId = productId };

            // Act
            var result = await _service.AddToWishlistAsync(accountId, dto);

            // Assert
            result.Should().NotBeNull();
            _wishlistRepository.Verify(r => r.AddItemAsync(It.Is<WishlistItem>(item =>
                item.WishlistId == wishlistId && item.ProductId == productId)), Times.Once);
            _context.Verify(c => c.SaveChangesAsync(default), Times.Once);
        }

        #endregion

        #region Abnormal Tests (A)

        [Fact]
        public async Task AddToWishlistAsync_ShouldThrowException_WhenAccountNotFoundOrDeactivated()
        {
            // Arrange (UTCID02)
            string accountId = "non_existing_acc";
            _accountRepository.Setup(r => r.GetByIdAsync(accountId)).ReturnsAsync((Account?)null);

            var dto = new AddToWishlistDto { ProductId = "prod_001" };

            // Act & Assert
            var act = async () => await _service.AddToWishlistAsync(accountId, dto);
            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Account does not exist or has been deactivated.");
        }

        [Fact]
        public async Task AddToWishlistAsync_ShouldThrowException_WhenAccountNotActiveOrSuspended()
        {
            // Arrange (UTCID03)
            string accountId = "acc_banned";
            var account = new Account
            {
                AccountId = accountId,
                Status = AccountStatusEnum.Ban.ToString(),
                IsDeleted = false
            };
            _accountRepository.Setup(r => r.GetByIdAsync(accountId)).ReturnsAsync(account);

            var dto = new AddToWishlistDto { ProductId = "prod_001" };

            // Act & Assert
            var act = async () => await _service.AddToWishlistAsync(accountId, dto);
            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Account is not active or has been suspended.");
        }

        [Fact]
        public async Task AddToWishlistAsync_ShouldThrowException_WhenProductNotFoundOrDeleted()
        {
            // Arrange (UTCID04)
            string accountId = "acc_001";
            string userId = "usr_001";
            var account = new Account
            {
                AccountId = accountId,
                UserId = userId,
                Status = AccountStatusEnum.Active.ToString(),
                IsDeleted = false
            };
            _accountRepository.Setup(r => r.GetByIdAsync(accountId)).ReturnsAsync(account);

            var emptyProducts = new List<RetradeBE.Models.Product>().AsMockDbSet();
            _context.Setup(c => c.Product).Returns(emptyProducts.Object);

            var dto = new AddToWishlistDto { ProductId = "non_existing_product" };

            // Act & Assert
            var act = async () => await _service.AddToWishlistAsync(accountId, dto);
            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Product does not exist or has been deleted.");
        }

        [Fact]
        public async Task AddToWishlistAsync_ShouldThrowException_WhenProductNotAccepted()
        {
            // Arrange (UTCID05)
            string accountId = "acc_001";
            string userId = "usr_001";
            string productId = "prod_pending";
            var account = new Account
            {
                AccountId = accountId,
                UserId = userId,
                Status = AccountStatusEnum.Active.ToString(),
                IsDeleted = false
            };
            _accountRepository.Setup(r => r.GetByIdAsync(accountId)).ReturnsAsync(account);

            var productList = new List<RetradeBE.Models.Product>
            {
                new RetradeBE.Models.Product
                {
                    ProductId = productId,
                    SellerId = "seller_1",
                    Status = ProductStatusEnum.Pending.ToString(),
                    IsDeleted = false
                }
            };
            _context.Setup(c => c.Product).Returns(productList.AsMockDbSet().Object);

            var dto = new AddToWishlistDto { ProductId = productId };

            // Act & Assert
            var act = async () => await _service.AddToWishlistAsync(accountId, dto);
            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Product is not available to be added to the wishlist.");
        }

        [Fact]
        public async Task AddToWishlistAsync_ShouldThrowException_WhenUserIsSellerOfProduct()
        {
            // Arrange (UTCID06)
            string accountId = "acc_001";
            string userId = "usr_owner";
            string productId = "prod_own";
            var account = new Account
            {
                AccountId = accountId,
                UserId = userId,
                Status = AccountStatusEnum.Active.ToString(),
                IsDeleted = false
            };
            _accountRepository.Setup(r => r.GetByIdAsync(accountId)).ReturnsAsync(account);

            var productList = new List<RetradeBE.Models.Product>
            {
                new RetradeBE.Models.Product
                {
                    ProductId = productId,
                    SellerId = userId,
                    Status = ProductStatusEnum.Accepted.ToString(),
                    IsDeleted = false
                }
            };
            _context.Setup(c => c.Product).Returns(productList.AsMockDbSet().Object);

            var dto = new AddToWishlistDto { ProductId = productId };

            // Act & Assert
            var act = async () => await _service.AddToWishlistAsync(accountId, dto);
            await act.Should().ThrowAsync<Exception>()
                .WithMessage("You cannot add your own product to your wishlist.");
        }

        #endregion

        #region Boundary Tests (B)

        [Fact]
        public async Task AddToWishlistAsync_ShouldHandleBoundary_WhenProductIsDeletedPropertyIsNull()
        {
            // Arrange (UTCID08)
            string accountId = "acc_001";
            string userId = "usr_001";
            string productId = "prod_null_isdeleted";
            string wishlistId = "wl_001";

            var account = new Account
            {
                AccountId = accountId,
                UserId = userId,
                Status = AccountStatusEnum.Active.ToString(),
                IsDeleted = false
            };
            _accountRepository.Setup(r => r.GetByIdAsync(accountId)).ReturnsAsync(account);

            var productList = new List<RetradeBE.Models.Product>
            {
                new RetradeBE.Models.Product
                {
                    ProductId = productId,
                    SellerId = "seller_999",
                    Status = ProductStatusEnum.Accepted.ToString(),
                    IsDeleted = null
                }
            };
            _context.Setup(c => c.Product).Returns(productList.AsMockDbSet().Object);

            var wishlist = new Wishlist
            {
                WishlistId = wishlistId,
                UserId = userId,
                Status = "Active",
                IsDeleted = false
            };

            _wishlistRepository.Setup(r => r.GetOrCreateActiveWishlistAsync(userId)).ReturnsAsync(wishlist);
            _wishlistRepository.Setup(r => r.IsProductInWishlistAsync(wishlistId, productId)).ReturnsAsync(false);
            _wishlistRepository.Setup(r => r.AddItemAsync(It.IsAny<WishlistItem>())).Returns(Task.CompletedTask);

            var mockWishlistDbSet = new List<Wishlist> { wishlist }.AsMockDbSet();
            _context.Setup(c => c.Wishlist).Returns(mockWishlistDbSet.Object);
            _context.Setup(c => c.SaveChangesAsync(default)).ReturnsAsync(1);

            var dto = new AddToWishlistDto { ProductId = productId };

            // Act
            var result = await _service.AddToWishlistAsync(accountId, dto);

            // Assert
            result.Should().NotBeNull();
            _wishlistRepository.Verify(r => r.AddItemAsync(It.Is<WishlistItem>(item =>
                item.WishlistId == wishlistId && item.ProductId == productId)), Times.Once);
        }

        #endregion
    }
}
