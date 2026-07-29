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
    public class ProductGetProductsTests
    {
        private readonly Mock<IProductRepository> _productRepository;
        private readonly Mock<IAccountRepository> _accountRepository;
        private readonly Mock<AppDbContext> _context;
        private readonly IMapper _mapper;
        private readonly ProductService _service;

        public ProductGetProductsTests()
        {
            _productRepository = new Mock<IProductRepository>();
            _accountRepository = new Mock<IAccountRepository>();
            _context = new Mock<AppDbContext>();

            var configuration = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<AutoMapperProfile>();
            }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
            _mapper = configuration.CreateMapper();

            _service = new ProductService(
                _productRepository.Object,
                _accountRepository.Object,
                _context.Object,
                _mapper
            );
        }

        #region Normal Tests (N)

        [Fact]
        public async Task GetProductsAsync_ShouldReturnPagedProducts_WhenQueryIsEmpty()
        {
            // Arrange
            var query = new ProductSearchQueryDto();
            var category = new Category { CategoryId = "cat_1", Status = "Active" };
            var products = new List<Product>
            {
                new Product
                {
                    ProductId = "prod_1",
                    Name = "Phone",
                    Description = "Smartphone",
                    Price = 500,
                    Condition = "New",
                    Status = "Active",
                    CategoryId = "cat_1",
                    Category = category,
                    ProductImage = new List<ProductImage>()
                }
            };

            _productRepository.Setup(x => x.Query()).Returns(products.AsAsyncQueryable());

            // Act
            var result = await _service.GetProductsAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.TotalItems.Should().Be(1);
            result.Items.Should().HaveCount(1);
            result.Items[0].ProductId.Should().Be("prod_1");
            result.Items[0].Name.Should().Be("Phone");
        }

        [Fact]
        public async Task GetProductsAsync_ShouldFilterBySearchTerm_WhenSearchTermMatchesNameOrDescription()
        {
            // Arrange
            var query = new ProductSearchQueryDto { SearchTerm = "laptop" };
            var category = new Category { CategoryId = "cat_1", Status = "Active" };
            var products = new List<Product>
            {
                new Product
                {
                    ProductId = "prod_1",
                    Name = "Gaming Laptop",
                    Description = "High performance",
                    Price = 1200,
                    Condition = "New",
                    Status = "Active",
                    CategoryId = "cat_1",
                    Category = category,
                    ProductImage = new List<ProductImage>()
                },
                new Product
                {
                    ProductId = "prod_2",
                    Name = "Smartphone",
                    Description = "Generic phone",
                    Price = 300,
                    Condition = "Used",
                    Status = "Active",
                    CategoryId = "cat_1",
                    Category = category,
                    ProductImage = new List<ProductImage>()
                }
            };

            _productRepository.Setup(x => x.Query()).Returns(products.AsAsyncQueryable());

            // Act
            var result = await _service.GetProductsAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.TotalItems.Should().Be(1);
            result.Items[0].ProductId.Should().Be("prod_1");
            result.Items[0].Name.Should().Be("Gaming Laptop");
        }

        [Fact]
        public async Task GetProductsAsync_ShouldFilterByCategoryAndSubcategories_WhenCategoryIdIsProvided()
        {
            // Arrange
            var query = new ProductSearchQueryDto { CategoryId = "cat_1" };
            var categoryParent = new Category { CategoryId = "cat_1", Status = "Active", ParentId = null };
            var categoryChild = new Category { CategoryId = "cat_2", Status = "Active", ParentId = "cat_1" };
            var products = new List<Product>
            {
                new Product
                {
                    ProductId = "prod_1",
                    Name = "Laptop Book",
                    Price = 100,
                    CategoryId = "cat_2",
                    Category = categoryChild,
                    Status = "Active",
                    ProductImage = new List<ProductImage>()
                },
                new Product
                {
                    ProductId = "prod_2",
                    Name = "Clothes",
                    Price = 20,
                    CategoryId = "cat_other",
                    Category = new Category { CategoryId = "cat_other", Status = "Active", ParentId = null },
                    Status = "Active",
                    ProductImage = new List<ProductImage>()
                }
            };

            _productRepository.Setup(x => x.Query()).Returns(products.AsAsyncQueryable());

            var categories = new List<Category> { categoryParent, categoryChild };
            var mockCategoryDbSet = categories.AsMockDbSet();
            _context.Setup(c => c.Category).Returns(mockCategoryDbSet.Object);

            // Act
            var result = await _service.GetProductsAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.TotalItems.Should().Be(1);
            result.Items[0].ProductId.Should().Be("prod_1");
        }

        [Fact]
        public async Task GetProductsAsync_ShouldFilterByMinAndMaxPrice_WhenPriceRangeIsProvided()
        {
            // Arrange
            var query = new ProductSearchQueryDto { MinPrice = 100, MaxPrice = 500 };
            var category = new Category { CategoryId = "cat_1", Status = "Active" };
            var products = new List<Product>
            {
                new Product { ProductId = "prod_1", Name = "A", Price = 50, Category = category, ProductImage = new List<ProductImage>() },
                new Product { ProductId = "prod_2", Name = "B", Price = 250, Category = category, ProductImage = new List<ProductImage>() },
                new Product { ProductId = "prod_3", Name = "C", Price = 600, Category = category, ProductImage = new List<ProductImage>() }
            };

            _productRepository.Setup(x => x.Query()).Returns(products.AsAsyncQueryable());

            // Act
            var result = await _service.GetProductsAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.TotalItems.Should().Be(1);
            result.Items[0].ProductId.Should().Be("prod_2");
        }

        [Fact]
        public async Task GetProductsAsync_ShouldFilterByCondition_WhenConditionIsProvided()
        {
            // Arrange
            var query = new ProductSearchQueryDto { Condition = "New" };
            var category = new Category { CategoryId = "cat_1", Status = "Active" };
            var products = new List<Product>
            {
                new Product { ProductId = "prod_1", Name = "A", Condition = "New", Category = category, ProductImage = new List<ProductImage>() },
                new Product { ProductId = "prod_2", Name = "B", Condition = "Used", Category = category, ProductImage = new List<ProductImage>() }
            };

            _productRepository.Setup(x => x.Query()).Returns(products.AsAsyncQueryable());

            // Act
            var result = await _service.GetProductsAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.TotalItems.Should().Be(1);
            result.Items[0].ProductId.Should().Be("prod_1");
        }

        [Fact]
        public async Task GetProductsAsync_ShouldFilterByStatus_WhenStatusIsProvided()
        {
            // Arrange
            var query = new ProductSearchQueryDto { Status = "Active" };
            var category = new Category { CategoryId = "cat_1", Status = "Active" };
            var products = new List<Product>
            {
                new Product { ProductId = "prod_1", Name = "A", Status = "Active", Category = category, ProductImage = new List<ProductImage>() },
                new Product { ProductId = "prod_2", Name = "B", Status = "Draft", Category = category, ProductImage = new List<ProductImage>() }
            };

            _productRepository.Setup(x => x.Query()).Returns(products.AsAsyncQueryable());

            // Act
            var result = await _service.GetProductsAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.TotalItems.Should().Be(1);
            result.Items[0].ProductId.Should().Be("prod_1");
        }

        [Fact]
        public async Task GetProductsAsync_ShouldFilterBySellerId_WhenSellerIdIsProvided()
        {
            // Arrange
            var query = new ProductSearchQueryDto { SellerId = "seller_1" };
            var products = new List<Product>
            {
                new Product { ProductId = "prod_1", Name = "A", SellerId = "seller_1", ProductImage = new List<ProductImage>() },
                new Product { ProductId = "prod_2", Name = "B", SellerId = "seller_2", ProductImage = new List<ProductImage>() }
            };

            _productRepository.Setup(x => x.Query()).Returns(products.AsAsyncQueryable());

            // Act
            var result = await _service.GetProductsAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.TotalItems.Should().Be(1);
            result.Items[0].ProductId.Should().Be("prod_1");
        }

        [Fact]
        public async Task GetProductsAsync_ShouldSortByPriceAsc_WhenSortByIsPriceAsc()
        {
            // Arrange
            var query = new ProductSearchQueryDto { SortBy = "price_asc" };
            var category = new Category { CategoryId = "cat_1", Status = "Active" };
            var products = new List<Product>
            {
                new Product { ProductId = "prod_1", Name = "A", Price = 300, Category = category, ProductImage = new List<ProductImage>() },
                new Product { ProductId = "prod_2", Name = "B", Price = 100, Category = category, ProductImage = new List<ProductImage>() }
            };

            _productRepository.Setup(x => x.Query()).Returns(products.AsAsyncQueryable());

            // Act
            var result = await _service.GetProductsAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.Items[0].ProductId.Should().Be("prod_2");
            result.Items[1].ProductId.Should().Be("prod_1");
        }

        [Fact]
        public async Task GetProductsAsync_ShouldApplyPagination_WhenPageAndPageSizeAreProvided()
        {
            // Arrange
            var query = new ProductSearchQueryDto { Page = 2, PageSize = 1 };
            var category = new Category { CategoryId = "cat_1", Status = "Active" };
            var products = new List<Product>
            {
                new Product { ProductId = "prod_1", Name = "A", CreatedAt = DateTime.UtcNow.AddMinutes(-5), Category = category, ProductImage = new List<ProductImage>() },
                new Product { ProductId = "prod_2", Name = "B", CreatedAt = DateTime.UtcNow, Category = category, ProductImage = new List<ProductImage>() }
            };

            _productRepository.Setup(x => x.Query()).Returns(products.AsAsyncQueryable());

            // Act
            var result = await _service.GetProductsAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.TotalItems.Should().Be(2);
            result.TotalPages.Should().Be(2);
            result.Items.Should().HaveCount(1);
            // Default sort is newest first, so prod_2 (newer) is Page 1, prod_1 (older) is Page 2
            result.Items[0].ProductId.Should().Be("prod_1");
        }

        #endregion

        #region Abnormal Tests (A)

        [Fact]
        public async Task GetProductsAsync_ShouldExcludeProductsFromInactiveCategories_WhenSellerIdIsEmptyOrNull()
        {
            // Arrange
            var query = new ProductSearchQueryDto { SellerId = null };
            var products = new List<Product>
            {
                new Product
                {
                    ProductId = "prod_1",
                    Name = "Active Cat Product",
                    Category = new Category { CategoryId = "cat_active", Status = "Active" },
                    ProductImage = new List<ProductImage>()
                },
                new Product
                {
                    ProductId = "prod_2",
                    Name = "Inactive Cat Product",
                    Category = new Category { CategoryId = "cat_inactive", Status = "Inactive" },
                    ProductImage = new List<ProductImage>()
                }
            };

            _productRepository.Setup(x => x.Query()).Returns(products.AsAsyncQueryable());

            // Act
            var result = await _service.GetProductsAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.TotalItems.Should().Be(1);
            result.Items[0].ProductId.Should().Be("prod_1");
        }

        #endregion

        #region Boundary Tests (B)

        [Fact]
        public async Task GetProductsAsync_ShouldFilterByPriorityOnly_WhenIsPriorityOnlyIsTrue()
        {
            // Arrange
            var query = new ProductSearchQueryDto { IsPriorityOnly = true };
            var category = new Category { CategoryId = "cat_1", Status = "Active" };
            var products = new List<Product>
            {
                new Product
                {
                    ProductId = "prod_1",
                    Name = "Priority Product",
                    SellerId = "priority_seller",
                    Status = "Active",
                    CategoryId = "cat_1",
                    Category = category,
                    ProductImage = new List<ProductImage>()
                },
                new Product
                {
                    ProductId = "prod_2",
                    Name = "Regular Product",
                    SellerId = "regular_seller",
                    Status = "Active",
                    CategoryId = "cat_1",
                    Category = category,
                    ProductImage = new List<ProductImage>()
                }
            };

            var myServices = new List<MyService>
            {
                new MyService
                {
                    UserId = "priority_seller",
                    Status = "Active",
                    ServiceId = "SERVICE_PRIORITY_LISTING",
                    EndDate = DateTime.UtcNow.AddDays(5)
                }
            };

            _productRepository.Setup(x => x.Query()).Returns(products.AsAsyncQueryable());

            var mockMyServiceSet = myServices.AsMockDbSet();
            _context.Setup(c => c.MyService).Returns(mockMyServiceSet.Object);

            // Act
            var result = await _service.GetProductsAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.TotalItems.Should().Be(1);
            result.Items[0].ProductId.Should().Be("prod_1");
        }

        #endregion
    }
}
