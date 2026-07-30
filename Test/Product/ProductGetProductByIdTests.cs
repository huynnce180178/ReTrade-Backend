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
using RetradeBE.Repositories;
using RetradeBE.Services;
using Xunit;

namespace Test.ProductTests
{
    public class ProductGetProductByIdTests
    {
        private readonly Mock<IProductRepository> _productRepository;
        private readonly Mock<IAccountRepository> _accountRepository;
        private readonly Mock<AppDbContext> _context;
        private readonly Mock<INotificationService> _notificationService;
        private readonly IMapper _mapper;
        private readonly ProductService _service;

        public ProductGetProductByIdTests()
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
        public async Task GetProductByIdAsync_ShouldReturnMappedDto_WhenProductExists()
        {
            // Arrange
            string productId = "prod_1";
            var product = new Product
            {
                ProductId = productId,
                Name = "Smartphone",
                Description = "High end smartphone",
                Price = 800,
                Status = "Active",
                ProductImage = new List<ProductImage>(),
                ProductAttribute = new List<ProductAttribute>()
            };

            _productRepository.Setup(x => x.GetByIdAsync(productId)).ReturnsAsync(product);

            // Act
            var result = await _service.GetProductByIdAsync(productId);

            // Assert
            result.Should().NotBeNull();
            result!.ProductId.Should().Be(productId);
            result.Name.Should().Be("Smartphone");
            result.Price.Should().Be(800);

            _productRepository.Verify(x => x.GetByIdAsync(productId), Times.Once);
        }

        #endregion

        #region Abnormal Tests (A)

        [Fact]
        public async Task GetProductByIdAsync_ShouldReturnNull_WhenProductDoesNotExist()
        {
            // Arrange
            string productId = "nonexistent";
            _productRepository.Setup(x => x.GetByIdAsync(productId)).ReturnsAsync((Product?)null);

            // Act
            var result = await _service.GetProductByIdAsync(productId);

            // Assert
            result.Should().BeNull();
            _productRepository.Verify(x => x.GetByIdAsync(productId), Times.Once);
        }

        #endregion

        #region Boundary Tests (B)

        [Fact]
        public async Task GetProductByIdAsync_ShouldReturnDtoWithEmptyCollections_WhenProductHasNoImagesOrAttributes()
        {
            // Arrange
            string productId = "prod_empty";
            var product = new Product
            {
                ProductId = productId,
                Name = "Generic Item",
                Price = 10,
                Status = "Draft",
                ProductImage = new List<ProductImage>(),
                ProductAttribute = new List<ProductAttribute>()
            };

            _productRepository.Setup(x => x.GetByIdAsync(productId)).ReturnsAsync(product);

            // Act
            var result = await _service.GetProductByIdAsync(productId);

            // Assert
            result.Should().NotBeNull();
            result!.Images.Should().BeEmpty();
            result.Attributes.Should().BeEmpty();
        }

        #endregion
    }
}
