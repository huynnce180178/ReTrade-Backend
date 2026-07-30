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
using RetradeBE.Repositories;
using RetradeBE.Services;
using Xunit;

namespace Test.ProductTests
{
    public class ProductDeleteProductTests
    {
        private readonly Mock<IProductRepository> _productRepository;
        private readonly Mock<IAccountRepository> _accountRepository;
        private readonly Mock<AppDbContext> _context;
        private readonly Mock<INotificationService> _notificationService;
        private readonly IMapper _mapper;
        private readonly ProductService _service;

        public ProductDeleteProductTests()
        {
            _productRepository = new Mock<IProductRepository>();
            _accountRepository = new Mock<IAccountRepository>();
            _context = new Mock<AppDbContext>();
            _notificationService = new Mock<INotificationService>();

            var configuration = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<AutoMapperProfile>();
            }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
            _mapper = configuration.CreateMapper();

            _service = new ProductService(
                _productRepository.Object,
                _accountRepository.Object,
                _context.Object,
                _mapper,
                _notificationService.Object
            );
        }

        #region Normal Tests (N)

        [Fact]
        public async Task DeleteProductAsync_ShouldSoftDeleteProduct_WhenUserIsOwner()
        {
            // Arrange
            string productId = "prod_1";
            string accountId = "acc_123";
            var account = new Account { AccountId = accountId, UserId = "user_123", AccountRole = new List<AccountRole>() };
            var product = new Product { ProductId = productId, SellerId = "user_123", IsDeleted = false };

            _accountRepository.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);

            var productsList = new List<Product> { product };
            var mockProductDbSet = productsList.AsMockDbSet();
            _context.Setup(c => c.Product).Returns(mockProductDbSet.Object);

            // Act
            await _service.DeleteProductAsync(productId, accountId);

            // Assert
            product.IsDeleted.Should().BeTrue();
            _context.Verify(c => c.SaveChangesAsync(default), Times.Once);
        }

        [Fact]
        public async Task DeleteProductAsync_ShouldSoftDeleteProduct_WhenUserIsAdmin()
        {
            // Arrange
            string productId = "prod_1";
            string accountId = "acc_admin";
            var adminRole = new Role { Name = "Admin" };
            var account = new Account
            {
                AccountId = accountId,
                UserId = "user_admin",
                AccountRole = new List<AccountRole>
                {
                    new AccountRole { Role = adminRole }
                }
            };
            var product = new Product { ProductId = productId, SellerId = "some_user", IsDeleted = false };

            _accountRepository.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);

            var productsList = new List<Product> { product };
            var mockProductDbSet = productsList.AsMockDbSet();
            _context.Setup(c => c.Product).Returns(mockProductDbSet.Object);

            // Act
            await _service.DeleteProductAsync(productId, accountId);

            // Assert
            product.IsDeleted.Should().BeTrue();
            _context.Verify(c => c.SaveChangesAsync(default), Times.Once);
        }

        #endregion

        #region Abnormal Tests (A)

        [Fact]
        public async Task DeleteProductAsync_ShouldThrowException_WhenAccountDoesNotExist()
        {
            // Arrange
            string productId = "prod_1";
            string accountId = "invalid_acc";
            _accountRepository.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync((Account?)null);

            // Act
            Func<Task> act = async () => await _service.DeleteProductAsync(productId, accountId);

            // Assert
            await act.Should().ThrowAsync<Exception>().WithMessage("Account does not exist.");
        }

        [Fact]
        public async Task DeleteProductAsync_ShouldThrowException_WhenProductDoesNotExist()
        {
            // Arrange
            string productId = "invalid_prod";
            string accountId = "acc_123";
            var account = new Account { AccountId = accountId, UserId = "user_123", AccountRole = new List<AccountRole>() };

            _accountRepository.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);

            var productsList = new List<Product>(); // Empty
            var mockProductDbSet = productsList.AsMockDbSet();
            _context.Setup(c => c.Product).Returns(mockProductDbSet.Object);

            // Act
            Func<Task> act = async () => await _service.DeleteProductAsync(productId, accountId);

            // Assert
            await act.Should().ThrowAsync<Exception>().WithMessage("Product does not exist.");
        }

        [Fact]
        public async Task DeleteProductAsync_ShouldThrowException_WhenUserIsNotOwnerNorAdmin()
        {
            // Arrange
            string productId = "prod_1";
            string accountId = "acc_other";
            var account = new Account { AccountId = accountId, UserId = "user_other", AccountRole = new List<AccountRole>() };
            var product = new Product { ProductId = productId, SellerId = "user_owner", IsDeleted = false };

            _accountRepository.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);

            var productsList = new List<Product> { product };
            var mockProductDbSet = productsList.AsMockDbSet();
            _context.Setup(c => c.Product).Returns(mockProductDbSet.Object);

            // Act
            Func<Task> act = async () => await _service.DeleteProductAsync(productId, accountId);

            // Assert
            await act.Should().ThrowAsync<Exception>().WithMessage("You do not have permission to delete this product.");
        }

        #endregion

        #region Boundary Tests (B)

        [Fact]
        public async Task DeleteProductAsync_ShouldThrowException_WhenProductIsAlreadyDeleted()
        {
            // Arrange
            string productId = "prod_1";
            string accountId = "acc_123";
            var account = new Account { AccountId = accountId, UserId = "user_123", AccountRole = new List<AccountRole>() };
            var product = new Product { ProductId = productId, SellerId = "user_123", IsDeleted = true }; // Already deleted

            _accountRepository.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);

            var productsList = new List<Product> { product };
            var mockProductDbSet = productsList.AsMockDbSet();
            _context.Setup(c => c.Product).Returns(mockProductDbSet.Object);

            // Act
            Func<Task> act = async () => await _service.DeleteProductAsync(productId, accountId);

            // Assert
            await act.Should().ThrowAsync<Exception>().WithMessage("Product does not exist.");
        }

        #endregion
    }
}
