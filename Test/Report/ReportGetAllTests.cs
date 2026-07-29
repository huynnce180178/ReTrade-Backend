using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using FluentAssertions;
using Moq;
using RetradeBE.Mappings;
using RetradeBE.Models;
using RetradeBE.Repositories;
using RetradeBE.Services;
using Xunit;

namespace Test.ReportTests
{
    public class ReportGetAllTests
    {
        private readonly Mock<IReportRepository> _reportRepository;
        private readonly Mock<IOrderService> _orderService;
        private readonly Mock<IAccountService> _accountService;
        private readonly Mock<IUserService> _userService;
        private readonly Mock<IProductService> _productService;
        private readonly Mock<IReviewService> _reviewService;
        private readonly IMapper _mapper;
        private readonly ReportService _service;

        public ReportGetAllTests()
        {
            _reportRepository = new Mock<IReportRepository>();
            _orderService = new Mock<IOrderService>();
            _accountService = new Mock<IAccountService>();
            _userService = new Mock<IUserService>();
            _productService = new Mock<IProductService>();
            _reviewService = new Mock<IReviewService>();

            var configuration = new AutoMapper.MapperConfiguration(cfg =>
            {
                cfg.AddProfile<AutoMapperProfile>();
            }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
            _mapper = configuration.CreateMapper();

            _service = new ReportService(
                _reportRepository.Object,
                _orderService.Object,
                _accountService.Object,
                _userService.Object,
                _productService.Object,
                _reviewService.Object,
                _mapper
            );
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnProjectedReportListDtoQueryable()
        {
            // Arrange
            var reporter = new User
            {
                UserId = "user_1",
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@example.com"
            };

            var reportsList = new List<Report>
            {
                new Report
                {
                    ReportId = "R1",
                    ReporterId = "user_1",
                    Reporter = reporter,
                    TargetType = "review",
                    TargetId = "review_1",
                    Reason = "Spam",
                    Status = "Pending"
                },
                new Report
                {
                    ReportId = "R2",
                    ReporterId = "user_1",
                    Reporter = reporter,
                    TargetType = "buyer",
                    TargetId = "order_1",
                    Reason = "Abuse",
                    Status = "Accepted"
                }
            };

            _reportRepository.Setup(x => x.Query()).Returns(reportsList.AsQueryable());

            // Act
            var resultQueryable = await _service.GetAllAsync();
            var result = resultQueryable.ToList();

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);

            result[0].ReportId.Should().Be("R1");
            result[0].ReporterName.Should().Be("John Doe");
            result[0].Reason.Should().Be("Spam");

            result[1].ReportId.Should().Be("R2");
            result[1].ReporterName.Should().Be("John Doe");
            result[1].Reason.Should().Be("Abuse");

            _reportRepository.Verify(x => x.Query(), Times.Once);
        }
    }
}
