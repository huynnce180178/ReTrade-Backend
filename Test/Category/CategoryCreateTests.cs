using RetradeBE.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using FluentAssertions;
using Moq;
using RetradeBE.Mappings;
using RetradeBE.Models;
using RetradeBE.Models.DTOs;
using RetradeBE.Repositories;
using Xunit;

namespace Test.CategoryTests
{
    public class CategoryCreateTests
    {
        private readonly Mock<ICategoryRepository> _categoryRepository;
        private readonly Mock<INotificationService> _notificationService;
        private readonly IMapper _mapper;
        private readonly CategoryService _service;

        public CategoryCreateTests()
        {
            _categoryRepository = new Mock<ICategoryRepository>();
            _notificationService = new Mock<INotificationService>();

            var configuration = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<AutoMapperProfile>();
            }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
            _mapper = configuration.CreateMapper();

            _service = new CategoryService(_categoryRepository.Object, _mapper, _notificationService.Object);
        }

        [Fact]
        public async Task CreateAsync_ShouldCreateCategoryWithoutAttributes_WhenParentIdIsBlank()
        {
            // Arrange
            var request = new CategoryCreateDto
            {
                Name = "Books",
                Description = "All books",
                Status = "Active",
                ParentId = null
            };

            _categoryRepository.Setup(x => x.AddAsync(It.IsAny<Category>())).Returns(Task.CompletedTask);

            // Act
            var result = await _service.CreateAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Name.Should().Be("Books");
            result.Description.Should().Be("All books");
            result.ParentId.Should().BeNull();

            _categoryRepository.Verify(x => x.AddAsync(It.Is<Category>(c =>
                c.Name == "Books" &&
                c.Description == "All books" &&
                c.ParentId == null &&
                c.Status == "Active"
            )), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_ShouldCreateCategoryWithAttributes_WhenAttributesAreProvided()
        {
            // Arrange
            var request = new CategoryCreateDto
            {
                Name = "Phones",
                Description = "Mobile phones",
                Status = "Active",
                ParentId = null,
                Attributes = new List<AttributeCreateDto>
                {
                    new AttributeCreateDto
                    {
                        Name = "Brand",
                        DataType = "String",
                        IsRequired = true,
                        IsFilterable = true
                    }
                }
            };

            _categoryRepository.Setup(x => x.AddAsync(It.IsAny<Category>())).Returns(Task.CompletedTask);

            // Act
            var result = await _service.CreateAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Name.Should().Be("Phones");
            result.Attributes.Should().HaveCount(1);
            result.Attributes.First().Name.Should().Be("Brand");

            _categoryRepository.Verify(x => x.AddAsync(It.Is<Category>(c =>
                c.Name == "Phones" &&
                c.Attributes.Count == 1 &&
                c.Attributes.First().Name == "Brand" &&
                c.Attributes.First().DataType == "String" &&
                c.Attributes.First().IsRequired == true
            )), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_ShouldUseDefaultStatusActive_WhenStatusIsNotProvided()
        {
            // Arrange
            var request = new CategoryCreateDto
            {
                Name = "Books",
                Description = "All books",
                Status = null
            };

            _categoryRepository.Setup(x => x.AddAsync(It.IsAny<Category>())).Returns(Task.CompletedTask);

            // Act
            var result = await _service.CreateAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Status.Should().Be("Active");

            _categoryRepository.Verify(x => x.AddAsync(It.Is<Category>(c =>
                c.Name == "Books" &&
                c.Status == "Active"
            )), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_ShouldCleanCategoryNameForId_WhenNameContainsSpecialCharactersOrAccents()
        {
            // Arrange
            var request = new CategoryCreateDto
            {
                Name = "Điện Thoại & Máy Tính!",
                Description = "Phones and Computers",
                Status = "Active"
            };

            _categoryRepository.Setup(x => x.AddAsync(It.IsAny<Category>())).Returns(Task.CompletedTask);

            // Act
            var result = await _service.CreateAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.CategoryId.Should().Contain("cat_đien_t");

            _categoryRepository.Verify(x => x.AddAsync(It.Is<Category>(c =>
                c.CategoryId.Contains("cat_đien_t")
            )), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_ShouldAssignDefaultDisplayOrder_WhenAttributeDisplayOrderIsNull()
        {
            // Arrange
            var request = new CategoryCreateDto
            {
                Name = "Books",
                Description = "All books",
                Attributes = new List<AttributeCreateDto>
                {
                    new AttributeCreateDto { Name = "Weight", DataType = "Number", DisplayOrder = null },
                    new AttributeCreateDto { Name = "Pages", DataType = "Number", DisplayOrder = null }
                }
            };

            _categoryRepository.Setup(x => x.AddAsync(It.IsAny<Category>())).Returns(Task.CompletedTask);

            // Act
            var result = await _service.CreateAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Attributes.Should().HaveCount(2);
            result.Attributes![0].DisplayOrder.Should().Be(1);
            result.Attributes![1].DisplayOrder.Should().Be(2);

            _categoryRepository.Verify(x => x.AddAsync(It.Is<Category>(c =>
                c.Attributes.Count == 2 &&
                c.Attributes.ElementAt(0).DisplayOrder == 1 &&
                c.Attributes.ElementAt(1).DisplayOrder == 2
            )), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_ShouldUseSpecifiedDisplayOrder_WhenAttributeDisplayOrderIsProvided()
        {
            // Arrange
            var request = new CategoryCreateDto
            {
                Name = "Books",
                Description = "All books",
                Attributes = new List<AttributeCreateDto>
                {
                    new AttributeCreateDto { Name = "Weight", DataType = "Number", DisplayOrder = 10 },
                    new AttributeCreateDto { Name = "Pages", DataType = "Number", DisplayOrder = 20 }
                }
            };

            _categoryRepository.Setup(x => x.AddAsync(It.IsAny<Category>())).Returns(Task.CompletedTask);

            // Act
            var result = await _service.CreateAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Attributes.Should().HaveCount(2);
            result.Attributes![0].DisplayOrder.Should().Be(10);
            result.Attributes![1].DisplayOrder.Should().Be(20);

            _categoryRepository.Verify(x => x.AddAsync(It.Is<Category>(c =>
                c.Attributes.Count == 2 &&
                c.Attributes.ElementAt(0).DisplayOrder == 10 &&
                c.Attributes.ElementAt(1).DisplayOrder == 20
            )), Times.Once);
        }
    }
}
