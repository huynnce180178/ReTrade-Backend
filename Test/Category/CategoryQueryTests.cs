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
    public class CategoryQueryTests
    {
        private readonly Mock<ICategoryRepository> _categoryRepository;
        private readonly Mock<INotificationService> _notificationService;
        private readonly IMapper _mapper;
        private readonly CategoryService _service;

        public CategoryQueryTests()
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
        public void Query_ShouldReturnProjectedCategoryResponseDtoQueryable()
        {
            // Arrange
            var categories = new List<Category>
            {
                new Category
                {
                    CategoryId = "cat_1",
                    Name = "Electronics",
                    Description = "Gadgets",
                    Status = "Active",
                    Attributes = new List<Attributes>()
                },
                new Category
                {
                    CategoryId = "cat_2",
                    Name = "Clothing",
                    Description = "Apparel",
                    Status = "Inactive",
                    Attributes = new List<Attributes>()
                }
            };

            _categoryRepository.Setup(x => x.Query()).Returns(categories.AsQueryable());

            // Act
            var resultQueryable = _service.Query();
            var resultList = resultQueryable.ToList();

            // Assert
            resultList.Should().NotBeNull();
            resultList.Should().HaveCount(2);

            resultList[0].CategoryId.Should().Be("cat_1");
            resultList[0].Name.Should().Be("Electronics");

            resultList[1].CategoryId.Should().Be("cat_2");
            resultList[1].Name.Should().Be("Clothing");

            _categoryRepository.Verify(x => x.Query(), Times.Once);
        }
    }
}
