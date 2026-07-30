using System;
using System.Collections.Generic;
using System.Linq;
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
    public class CategoryUpdateTests
    {
        private readonly Mock<ICategoryRepository> _categoryRepository;
        private readonly Mock<INotificationService> _notificationService;
        private readonly IMapper _mapper;
        private readonly CategoryService _service;

        public CategoryUpdateTests()
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
        public async Task UpdateAsync_ShouldThrowException_WhenCategoryDoesNotExist()
        {
            // Arrange
            string categoryId = "cat_1";
            _categoryRepository.Setup(x => x.GetByIdAsync(categoryId)).ReturnsAsync((Category?)null);

            // Act
            Func<Task> act = async () => await _service.UpdateAsync(categoryId, new CategoryUpdateDto());

            // Assert
            await act.Should().ThrowAsync<Exception>().WithMessage("Category 'cat_1' does not exist.");
        }

        [Fact]
        public async Task UpdateAsync_ShouldUpdateBasicFields_WhenValid()
        {
            // Arrange
            string categoryId = "cat_1";
            var category = new Category { CategoryId = categoryId, Name = "Fiction", Description = "Old Desc", Attributes = new List<Attributes>() };

            _categoryRepository.Setup(x => x.GetByIdAsync(categoryId)).ReturnsAsync(category);
            _categoryRepository.Setup(x => x.UpdateAsync(category)).Returns(Task.CompletedTask);

            var updateDto = new CategoryUpdateDto { Name = "New Fiction", Description = "New Desc" };

            // Act
            var result = await _service.UpdateAsync(categoryId, updateDto);

            // Assert
            result.Should().NotBeNull();
            category.Name.Should().Be("New Fiction");
            category.Description.Should().Be("New Desc");

            _categoryRepository.Verify(x => x.UpdateAsync(category), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_ShouldSoftDeleteMissingAttributes_WhenUpdatedWithFewerAttributes()
        {
            // Arrange
            string categoryId = "cat_1";
            var existingAttr = new Attributes { AttributeId = "attr_1", Name = "Size", DataType = "String", IsDeleted = false };
            var category = new Category
            {
                CategoryId = categoryId,
                Name = "Shirts",
                Attributes = new List<Attributes> { existingAttr }
            };

            _categoryRepository.Setup(x => x.GetByIdAsync(categoryId)).ReturnsAsync(category);
            _categoryRepository.Setup(x => x.UpdateAsync(category)).Returns(Task.CompletedTask);

            // Empty attributes list in update -> soft deletes existing ones
            var updateDto = new CategoryUpdateDto { Attributes = new List<AttributeUpdateDto>() };

            // Act
            await _service.UpdateAsync(categoryId, updateDto);

            // Assert
            existingAttr.IsDeleted.Should().BeTrue();
            _categoryRepository.Verify(x => x.UpdateAsync(category), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_ShouldThrowException_WhenChangingAttributeDataType()
        {
            // Arrange
            string categoryId = "cat_1";
            var existingAttr = new Attributes { AttributeId = "attr_1", Name = "Size", DataType = "String", IsDeleted = false };
            var category = new Category
            {
                CategoryId = categoryId,
                Name = "Shirts",
                Attributes = new List<Attributes> { existingAttr }
            };

            _categoryRepository.Setup(x => x.GetByIdAsync(categoryId)).ReturnsAsync(category);

            var updateDto = new CategoryUpdateDto
            {
                Attributes = new List<AttributeUpdateDto>
                {
                    new AttributeUpdateDto { AttributeId = "attr_1", Name = "Size", DataType = "Number" } // Changing String to Number
                }
            };

            // Act
            Func<Task> act = async () => await _service.UpdateAsync(categoryId, updateDto);

            // Assert
            await act.Should().ThrowAsync<Exception>().WithMessage("Changing the data type of the attribute 'Size' is not allowed. Please delete this attribute and create a new one.");
        }

        [Fact]
        public async Task UpdateAsync_ShouldThrowException_WhenAttributeDoesNotBelongToCategory()
        {
            // Arrange
            string categoryId = "cat_1";
            var category = new Category
            {
                CategoryId = categoryId,
                Name = "Shirts",
                Attributes = new List<Attributes>() // No existing attributes
            };

            _categoryRepository.Setup(x => x.GetByIdAsync(categoryId)).ReturnsAsync(category);

            var updateDto = new CategoryUpdateDto
            {
                Attributes = new List<AttributeUpdateDto>
                {
                    new AttributeUpdateDto { AttributeId = "unrelated_attr", Name = "Size", DataType = "String" }
                }
            };

            // Act
            Func<Task> act = async () => await _service.UpdateAsync(categoryId, updateDto);

            // Assert
            await act.Should().ThrowAsync<Exception>().WithMessage("Attribute 'unrelated_attr' does not belong to this category.");
        }
    }
}
