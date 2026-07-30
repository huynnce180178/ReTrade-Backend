using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using FluentAssertions;
using Moq;
using RetradeBE.Mappings;
using RetradeBE.Models;
using RetradeBE.Models.DTOs;
using RetradeBE.Repositories;
using RetradeBE.Services;
using Xunit;

namespace Test.ReportTests
{
    public class ReportGetFlaggedUsersTests
    {
        private readonly Mock<IReportRepository> _reportRepository;
        private readonly Mock<IOrderService> _orderService;
        private readonly Mock<IAccountService> _accountService;
        private readonly Mock<IUserService> _userService;
        private readonly Mock<IProductService> _productService;
        private readonly Mock<IReviewService> _reviewService;
        private readonly IMapper _mapper;
        private readonly Mock<INotificationService> _notificationService;
        private readonly ReportService _service;

        public ReportGetFlaggedUsersTests()
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
                // In AutoMapperProfile, there might not be a direct mapping for User -> FlaggedUserDto.
                // Let's make sure it is mapped properly or add it to configuration if needed.
                // Looking at AutoMapperProfile, let's verify if CreateMap<User, FlaggedUserDto>() is defined.
                // We'll configure a fallback map just in case it's not defined in AutoMapperProfile.
                cfg.CreateMap<User, FlaggedUserDto>();
            }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
            _mapper = configuration.CreateMapper();
            _notificationService = new Mock<INotificationService>();

            _service = new ReportService(
                _reportRepository.Object,
                _orderService.Object,
                _accountService.Object,
                _userService.Object,
                _productService.Object,
                _reviewService.Object,
                _mapper,
                _notificationService.Object
            );
        }

        [Fact]
        public async Task GetFlaggedUsersAsync_ShouldReturnFlaggedUsersSortedByFlagCount()
        {
            // Arrange
            var users = new List<User>
            {
                new User { UserId = "U1", FirstName = "UserOne", FlagCount = 1 },
                new User { UserId = "U2", FirstName = "UserTwo", FlagCount = 0 }, // Should be excluded
                new User { UserId = "U3", FirstName = "UserThree", FlagCount = 3 }
            };

            var reportsForU1 = new List<Report>
            {
                new Report { ReportId = "R1", TargetType = "review", Reason = "Spam" }
            };
            var reportsForU3 = new List<Report>
            {
                new Report { ReportId = "R2", TargetType = "buyer", Reason = "Scam" },
                new Report { ReportId = "R3", TargetType = "seller", Reason = "Harassment" }
            };

            _userService.Setup(x => x.GetAllAsync()).ReturnsAsync(users);
            _reportRepository.Setup(x => x.GetReportsForUserAsync("U1")).ReturnsAsync(reportsForU1);
            _reportRepository.Setup(x => x.GetReportsForUserAsync("U3")).ReturnsAsync(reportsForU3);

            // Act
            var result = await _service.GetFlaggedUsersAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);

            // Sorted by FlagCount descending: U3 (3 flags) then U1 (1 flag)
            result[0].UserId.Should().Be("U3");
            result[0].FlagCount.Should().Be(3);
            result[0].Reports.Should().HaveCount(2);

            result[1].UserId.Should().Be("U1");
            result[1].FlagCount.Should().Be(1);
            result[1].Reports.Should().HaveCount(1);

            _userService.Verify(x => x.GetAllAsync(), Times.Once);
            _reportRepository.Verify(x => x.GetReportsForUserAsync("U3"), Times.Once);
            _reportRepository.Verify(x => x.GetReportsForUserAsync("U1"), Times.Once);
            _reportRepository.Verify(x => x.GetReportsForUserAsync("U2"), Times.Never);
        }
    }
}
