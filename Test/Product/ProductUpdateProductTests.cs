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
using RetradeBE.Repositories;
using RetradeBE.Services;
using Xunit;

namespace Test.ProductTests
{
    public class ProductUpdateProductTests
    {
        private readonly Mock<IProductRepository> _productRepository;
        private readonly Mock<IAccountRepository> _accountRepository;
        private readonly Mock<AppDbContext> _context;
        private readonly Mock<INotificationService> _notificationService;
        private readonly IMapper _mapper;
        private readonly ProductService _service;

        public ProductUpdateProductTests()
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
        public async Task UpdateProductAsync_ShouldUpdateFields_WhenRequestIsValid()
        {
            // Arrange
            string productId = "prod_1";
            string accountId = "acc_123";
            var account = new Account { AccountId = accountId, UserId = "user_123" };
            var product = new Product
            {
                ProductId = productId,
                SellerId = "user_123",
                Name = "Old Name",
                Description = "Old Desc",
                Status = "Pending",
                ProductImage = new List<ProductImage>(),
                ProductAttribute = new List<ProductAttribute>()
            };

            _accountRepository.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);

            var productsList = new List<Product> { product };
            var mockProductDbSet = productsList.AsMockDbSet();
            _context.Setup(c => c.Product).Returns(mockProductDbSet.Object);

            var mockImageDbSet = new List<Image>().AsMockDbSet();
            _context.Setup(c => c.Image).Returns(mockImageDbSet.Object);
            var mockProductImageDbSet = new List<ProductImage>().AsMockDbSet();
            _context.Setup(c => c.ProductImage).Returns(mockProductImageDbSet.Object);

            _productRepository.Setup(x => x.GetByIdAsync(productId)).ReturnsAsync(product);

            var updateDto = new ProductUpdateDto
            {
                Name = "New Name",
                Description = "New Desc",
                Condition = "Good"
            };

            // Act
            var result = await _service.UpdateProductAsync(productId, accountId, updateDto);

            // Assert
            result.Should().NotBeNull();
            product.Name.Should().Be("New Name");
            product.Description.Should().Be("New Desc");
            product.Condition.Should().Be("Good");

            _context.Verify(c => c.SaveChangesAsync(default), Times.Once);
        }

        #endregion

        #region Abnormal Tests (A)

        [Fact]
        public async Task UpdateProductAsync_ShouldThrowException_WhenAccountDoesNotExist()
        {
            // Arrange
            string productId = "prod_1";
            string accountId = "invalid_acc";
            _accountRepository.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync((Account?)null);

            // Act
            Func<Task> act = async () => await _service.UpdateProductAsync(productId, accountId, new ProductUpdateDto());

            // Assert
            await act.Should().ThrowAsync<Exception>().WithMessage("Account does not exist.");
        }

        [Fact]
        public async Task UpdateProductAsync_ShouldThrowException_WhenProductDoesNotExist()
        {
            // Arrange
            string productId = "invalid_prod";
            string accountId = "acc_123";
            var account = new Account { AccountId = accountId, UserId = "user_123" };

            _accountRepository.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);

            var productsList = new List<Product>(); // Empty
            var mockProductDbSet = productsList.AsMockDbSet();
            _context.Setup(c => c.Product).Returns(mockProductDbSet.Object);

            // Act
            Func<Task> act = async () => await _service.UpdateProductAsync(productId, accountId, new ProductUpdateDto());

            // Assert
            await act.Should().ThrowAsync<Exception>().WithMessage("Product does not exist.");
        }

        [Fact]
        public async Task UpdateProductAsync_ShouldThrowException_WhenUserNotOwner()
        {
            // Arrange
            string productId = "prod_1";
            string accountId = "acc_123";
            var account = new Account { AccountId = accountId, UserId = "user_123" };
            var product = new Product
            {
                ProductId = productId,
                SellerId = "owner_user", // Different owner
                Name = "Product",
                Status = "Pending",
                ProductImage = new List<ProductImage>(),
                ProductAttribute = new List<ProductAttribute>()
            };

            _accountRepository.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);

            var productsList = new List<Product> { product };
            var mockProductDbSet = productsList.AsMockDbSet();
            _context.Setup(c => c.Product).Returns(mockProductDbSet.Object);

            // Act
            Func<Task> act = async () => await _service.UpdateProductAsync(productId, accountId, new ProductUpdateDto());

            // Assert
            await act.Should().ThrowAsync<Exception>().WithMessage("You do not have permission to edit this product.");
        }

        [Fact]
        public async Task UpdateProductAsync_ShouldThrowException_WhenConditionIsInvalid()
        {
            // Arrange
            string productId = "prod_1";
            string accountId = "acc_123";
            var account = new Account { AccountId = accountId, UserId = "user_123" };
            var product = new Product
            {
                ProductId = productId,
                SellerId = "user_123",
                Name = "Product",
                Status = "Pending",
                ProductImage = new List<ProductImage>(),
                ProductAttribute = new List<ProductAttribute>()
            };

            _accountRepository.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);

            var productsList = new List<Product> { product };
            var mockProductDbSet = productsList.AsMockDbSet();
            _context.Setup(c => c.Product).Returns(mockProductDbSet.Object);

            var updateDto = new ProductUpdateDto { Condition = "Superb" }; // Invalid

            // Act
            Func<Task> act = async () => await _service.UpdateProductAsync(productId, accountId, updateDto);

            // Assert
            await act.Should().ThrowAsync<Exception>().WithMessage("Invalid product condition.");
        }

        #endregion

        #region Boundary Tests (B)

        [Fact]
        public async Task UpdateProductAsync_ShouldRevertAuctionProductToWaiting_WhenProductIsAuctionType()
        {
            // Arrange
            string productId = "prod_1";
            string accountId = "acc_123";
            var account = new Account { AccountId = accountId, UserId = "user_123" };
            var product = new Product
            {
                ProductId = productId,
                SellerId = "user_123",
                Name = "Product",
                Status = "Ready", // Auction ready status
                ProductImage = new List<ProductImage>(),
                ProductAttribute = new List<ProductAttribute>()
            };

            _accountRepository.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);

            var productsList = new List<Product> { product };
            var mockProductDbSet = productsList.AsMockDbSet();
            _context.Setup(c => c.Product).Returns(mockProductDbSet.Object);

            _productRepository.Setup(x => x.GetByIdAsync(productId)).ReturnsAsync(product);

            var updateDto = new ProductUpdateDto { Name = "New Name" };

            // Act
            await _service.UpdateProductAsync(productId, accountId, updateDto);

            // Assert
            product.Status.Should().Be("Waiting");
            product.Price.Should().BeNull();
            product.StockQuantity.Should().Be(1);
        }

        #endregion
    }
}
