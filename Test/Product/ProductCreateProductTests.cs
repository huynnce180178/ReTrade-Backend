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
    public class ProductCreateProductTests
    {
        private readonly Mock<IProductRepository> _productRepository;
        private readonly Mock<IAccountRepository> _accountRepository;
        private readonly Mock<AppDbContext> _context;
        private readonly Mock<INotificationService> _notificationService;
        private readonly IMapper _mapper;
        private readonly ProductService _service;

        public ProductCreateProductTests()
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
        public async Task CreateProductAsync_ShouldCreateProduct_WhenValidSaleProductRequest()
        {
            // Arrange
            string accountId = "acc_123";
            var account = new Account { AccountId = accountId, UserId = "user_123" };
            var category = new Category { CategoryId = "cat_1", Name = "Books", Attributes = new List<Attributes>() };
            var request = new ProductCreateDto
            {
                Name = "C# in Depth",
                Description = "A great book",
                Price = 50,
                StockQuantity = 10,
                CategoryId = "cat_1",
                IsForAuction = false,
                Condition = "New",
                Images = new List<ProductImageDto>
                {
                    new ProductImageDto { ImageUrl = "http://example.com/img.png", IsMain = true }
                }
            };

            _accountRepository.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);
            
            var categories = new List<Category> { category };
            var mockCategoryDbSet = categories.AsMockDbSet();
            _context.Setup(c => c.Category).Returns(mockCategoryDbSet.Object);

            var mockImageDbSet = new List<Image>().AsMockDbSet();
            _context.Setup(c => c.Image).Returns(mockImageDbSet.Object);

            _productRepository.Setup(x => x.AddAsync(It.IsAny<Product>())).Returns(Task.CompletedTask);
            
            // Re-fetch mocked product
            var savedProduct = new Product
            {
                ProductId = "prod_123",
                Name = request.Name,
                Description = request.Description,
                Price = request.Price,
                StockQuantity = request.StockQuantity,
                CategoryId = request.CategoryId,
                SellerId = "user_123",
                Status = "Pending",
                ProductImage = new List<ProductImage>(),
                ProductAttribute = new List<ProductAttribute>()
            };
            _productRepository.Setup(x => x.GetByIdAsync(It.IsAny<string>())).ReturnsAsync(savedProduct);

            // Act
            var result = await _service.CreateProductAsync(accountId, request);

            // Assert
            result.Should().NotBeNull();
            result.Name.Should().Be("C# in Depth");
            result.Status.Should().Be("Pending");

            _productRepository.Verify(x => x.AddAsync(It.Is<Product>(p => p.Name == "C# in Depth")), Times.Once);
        }

        [Fact]
        public async Task CreateProductAsync_ShouldCreateProduct_WhenValidAuctionProductRequest()
        {
            // Arrange
            string accountId = "acc_123";
            var account = new Account { AccountId = accountId, UserId = "user_123" };
            var category = new Category { CategoryId = "cat_1", Name = "Books", Attributes = new List<Attributes>() };
            var request = new ProductCreateDto
            {
                Name = "Rare Stamp",
                Description = "Collector stamp",
                Price = null,
                StockQuantity = null,
                CategoryId = "cat_1",
                IsForAuction = true,
                Condition = "LikeNew",
                Images = new List<ProductImageDto>
                {
                    new ProductImageDto { ImageUrl = "http://example.com/stamp.png", IsMain = true }
                }
            };

            _accountRepository.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);

            var categories = new List<Category> { category };
            var mockCategoryDbSet = categories.AsMockDbSet();
            _context.Setup(c => c.Category).Returns(mockCategoryDbSet.Object);

            var mockImageDbSet = new List<Image>().AsMockDbSet();
            _context.Setup(c => c.Image).Returns(mockImageDbSet.Object);

            _productRepository.Setup(x => x.AddAsync(It.IsAny<Product>())).Returns(Task.CompletedTask);

            // Re-fetch mocked product
            var savedProduct = new Product
            {
                ProductId = "prod_123",
                Name = request.Name,
                Description = request.Description,
                Price = null,
                StockQuantity = 1,
                CategoryId = request.CategoryId,
                SellerId = "user_123",
                Status = "Waiting",
                ProductImage = new List<ProductImage>(),
                ProductAttribute = new List<ProductAttribute>()
            };
            _productRepository.Setup(x => x.GetByIdAsync(It.IsAny<string>())).ReturnsAsync(savedProduct);

            // Act
            var result = await _service.CreateProductAsync(accountId, request);

            // Assert
            result.Should().NotBeNull();
            result.Name.Should().Be("Rare Stamp");
            result.Status.Should().Be("Waiting");

            _productRepository.Verify(x => x.AddAsync(It.Is<Product>(p => p.Name == "Rare Stamp" && p.Status == "Waiting")), Times.Once);
        }

        #endregion

        #region Abnormal Tests (A)

        [Fact]
        public async Task CreateProductAsync_ShouldThrowException_WhenAccountDoesNotExist()
        {
            // Arrange
            string accountId = "invalid_acc";
            _accountRepository.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync((Account?)null);

            // Act
            Func<Task> act = async () => await _service.CreateProductAsync(accountId, new ProductCreateDto());

            // Assert
            await act.Should().ThrowAsync<Exception>().WithMessage("Account does not exist.");
        }

        [Fact]
        public async Task CreateProductAsync_ShouldThrowException_WhenAccountNotLinkedToUser()
        {
            // Arrange
            string accountId = "acc_123";
            var account = new Account { AccountId = accountId, UserId = null }; // not linked

            _accountRepository.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);

            // Act
            Func<Task> act = async () => await _service.CreateProductAsync(accountId, new ProductCreateDto());

            // Assert
            await act.Should().ThrowAsync<Exception>().WithMessage("Account is not linked to user information.");
        }

        [Fact]
        public async Task CreateProductAsync_ShouldThrowException_WhenCategoryDoesNotExist()
        {
            // Arrange
            string accountId = "acc_123";
            var account = new Account { AccountId = accountId, UserId = "user_123" };
            var request = new ProductCreateDto { CategoryId = "invalid_cat" };

            _accountRepository.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);

            var categories = new List<Category>(); // Empty
            var mockCategoryDbSet = categories.AsMockDbSet();
            _context.Setup(c => c.Category).Returns(mockCategoryDbSet.Object);

            // Act
            Func<Task> act = async () => await _service.CreateProductAsync(accountId, request);

            // Assert
            await act.Should().ThrowAsync<Exception>().WithMessage("Category does not exist.");
        }

        [Fact]
        public async Task CreateProductAsync_ShouldThrowException_WhenSaleProductHasInvalidPrice()
        {
            // Arrange
            string accountId = "acc_123";
            var account = new Account { AccountId = accountId, UserId = "user_123" };
            var category = new Category { CategoryId = "cat_1", Name = "Books", Attributes = new List<Attributes>() };
            var request = new ProductCreateDto
            {
                Name = "Books",
                CategoryId = "cat_1",
                IsForAuction = false,
                Price = 0 // Invalid price
            };

            _accountRepository.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);

            var categories = new List<Category> { category };
            var mockCategoryDbSet = categories.AsMockDbSet();
            _context.Setup(c => c.Category).Returns(mockCategoryDbSet.Object);

            // Act
            Func<Task> act = async () => await _service.CreateProductAsync(accountId, request);

            // Assert
            await act.Should().ThrowAsync<Exception>().WithMessage("Product price must be greater than 0.");
        }

        [Fact]
        public async Task CreateProductAsync_ShouldThrowException_WhenProductHasNoImages()
        {
            // Arrange
            string accountId = "acc_123";
            var account = new Account { AccountId = accountId, UserId = "user_123" };
            var category = new Category { CategoryId = "cat_1", Name = "Books", Attributes = new List<Attributes>() };
            var request = new ProductCreateDto
            {
                Name = "Books",
                CategoryId = "cat_1",
                IsForAuction = false,
                Price = 50,
                StockQuantity = 10,
                Images = null // No images
            };

            _accountRepository.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);

            var categories = new List<Category> { category };
            var mockCategoryDbSet = categories.AsMockDbSet();
            _context.Setup(c => c.Category).Returns(mockCategoryDbSet.Object);

            // Act
            Func<Task> act = async () => await _service.CreateProductAsync(accountId, request);

            // Assert
            await act.Should().ThrowAsync<Exception>().WithMessage("Product must have at least one image.");
        }

        [Fact]
        public async Task CreateProductAsync_ShouldThrowException_WhenConditionIsInvalid()
        {
            // Arrange
            string accountId = "acc_123";
            var account = new Account { AccountId = accountId, UserId = "user_123" };
            var category = new Category { CategoryId = "cat_1", Name = "Books", Attributes = new List<Attributes>() };
            var request = new ProductCreateDto
            {
                Name = "Books",
                CategoryId = "cat_1",
                IsForAuction = false,
                Price = 50,
                StockQuantity = 10,
                Condition = "Superb", // Invalid condition
                Images = new List<ProductImageDto>
                {
                    new ProductImageDto { ImageUrl = "http://example.com/img.png" }
                }
            };

            _accountRepository.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);

            var categories = new List<Category> { category };
            var mockCategoryDbSet = categories.AsMockDbSet();
            _context.Setup(c => c.Category).Returns(mockCategoryDbSet.Object);

            // Act
            Func<Task> act = async () => await _service.CreateProductAsync(accountId, request);

            // Assert
            await act.Should().ThrowAsync<Exception>().WithMessage("Invalid product condition.");
        }

        #endregion

        #region Boundary Tests (B)

        [Fact]
        public async Task CreateProductAsync_ShouldThrowException_WhenSaleProductHasInvalidStockQuantity()
        {
            // Arrange
            string accountId = "acc_123";
            var account = new Account { AccountId = accountId, UserId = "user_123" };
            var category = new Category { CategoryId = "cat_1", Name = "Books", Attributes = new List<Attributes>() };
            var request = new ProductCreateDto
            {
                Name = "Books",
                CategoryId = "cat_1",
                IsForAuction = false,
                Price = 50,
                StockQuantity = 0 // Invalid stock
            };

            _accountRepository.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);

            var categories = new List<Category> { category };
            var mockCategoryDbSet = categories.AsMockDbSet();
            _context.Setup(c => c.Category).Returns(mockCategoryDbSet.Object);

            // Act
            Func<Task> act = async () => await _service.CreateProductAsync(accountId, request);

            // Assert
            await act.Should().ThrowAsync<Exception>().WithMessage("Product stock quantity must be greater than 0.");
        }

        #endregion
    }
}
