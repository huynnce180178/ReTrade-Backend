using System;
using System.Threading.Tasks;
using AutoMapper;
using FluentAssertions;
using Moq;
using RetradeBE.Mappings;
using RetradeBE.Models;
using RetradeBE.Repositories;
using Xunit;

namespace Test.CategoryTests
{
    public class CategoryDeleteTests
    {
        private readonly Mock<ICategoryRepository> _categoryRepository;
        private readonly IMapper _mapper;
        private readonly CategoryService _service;

        public CategoryDeleteTests()
        {
            _categoryRepository = new Mock<ICategoryRepository>();

            var configuration = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<AutoMapperProfile>();
            }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
            _mapper = configuration.CreateMapper();

            _service = new CategoryService(_categoryRepository.Object, _mapper);
        }

        #region Inactive Tests (Delete Category)

        [Fact]
        public async Task InactiveAsync_ShouldUpdateStatusToInactive_WhenCategoryExists()
        {
            // Arrange
            string categoryId = "cat_1";
            var category = new Category { CategoryId = categoryId, Name = "Books", Status = "Active" };

            _categoryRepository.Setup(x => x.GetByIdAsync(categoryId)).ReturnsAsync(category);
            _categoryRepository.Setup(x => x.UpdateAsync(category)).Returns(Task.CompletedTask);

            // Act
            await _service.InactiveAsync(categoryId);

            // Assert
            category.Status.Should().Be("Inactive");
            _categoryRepository.Verify(x => x.UpdateAsync(category), Times.Once);
        }

        [Fact]
        public async Task InactiveAsync_ShouldThrowException_WhenCategoryDoesNotExist()
        {
            // Arrange
            string categoryId = "nonexistent";
            _categoryRepository.Setup(x => x.GetByIdAsync(categoryId)).ReturnsAsync((Category?)null);

            // Act
            Func<Task> act = async () => await _service.InactiveAsync(categoryId);

            // Assert
            await act.Should().ThrowAsync<Exception>().WithMessage("Category 'nonexistent' does not exist.");
            _categoryRepository.Verify(x => x.UpdateAsync(It.IsAny<Category>()), Times.Never);
        }

        #endregion

        #region Restore Tests

        [Fact]
        public async Task RestoreAsync_ShouldUpdateStatusToActive_WhenCategoryExists()
        {
            // Arrange
            string categoryId = "cat_1";
            var category = new Category { CategoryId = categoryId, Name = "Books", Status = "Inactive" };

            _categoryRepository.Setup(x => x.GetByIdAsync(categoryId)).ReturnsAsync(category);
            _categoryRepository.Setup(x => x.UpdateAsync(category)).Returns(Task.CompletedTask);

            // Act
            await _service.RestoreAsync(categoryId);

            // Assert
            category.Status.Should().Be("Active");
            _categoryRepository.Verify(x => x.UpdateAsync(category), Times.Once);
        }

        [Fact]
        public async Task RestoreAsync_ShouldThrowException_WhenCategoryDoesNotExist()
        {
            // Arrange
            string categoryId = "nonexistent";
            _categoryRepository.Setup(x => x.GetByIdAsync(categoryId)).ReturnsAsync((Category?)null);

            // Act
            Func<Task> act = async () => await _service.RestoreAsync(categoryId);

            // Assert
            await act.Should().ThrowAsync<Exception>().WithMessage("Category 'nonexistent' does not exist.");
            _categoryRepository.Verify(x => x.UpdateAsync(It.IsAny<Category>()), Times.Never);
        }

        #endregion
    }
}
