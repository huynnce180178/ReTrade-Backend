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
    public class ReportGetHistoryTests
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

        public ReportGetHistoryTests()
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

        #region Normal Tests (N)

        [Fact]
        public async Task GetHistoryAsync_ShouldReturnSubmittedAndReceivedReports_WhenAccountIsValid()
        {
            // Arrange
            string accountId = "acc_id";
            string userId = "user_id";
            var account = new Account { AccountId = accountId, UserId = userId };

            var submitted = new List<Report>
            {
                new Report { ReportId = "R1", ReporterId = userId, TargetType = "review", Reason = "Spam" }
            };
            var received = new List<Report>
            {
                new Report { ReportId = "R2", ReporterId = "other_user", TargetType = "buyer", Reason = "Scam" }
            };

            _accountService.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);
            _reportRepository.Setup(x => x.GetReportsByReporterAsync(userId)).ReturnsAsync(submitted);
            _reportRepository.Setup(x => x.GetReportsReceivedByUserAsync(userId)).ReturnsAsync(received);

            // Act
            var result = await _service.GetHistoryAsync(accountId);

            // Assert
            result.Should().NotBeNull();
            result.ReportsSubmitted.Should().HaveCount(1);
            result.ReportsSubmitted![0].ReportId.Should().Be("R1");

            result.ReportsReceived.Should().HaveCount(1);
            result.ReportsReceived![0].ReportId.Should().Be("R2");

            _accountService.Verify(x => x.GetByIdAsync(accountId), Times.Once);
            _reportRepository.Verify(x => x.GetReportsByReporterAsync(userId), Times.Once);
            _reportRepository.Verify(x => x.GetReportsReceivedByUserAsync(userId), Times.Once);
        }

        #endregion

        #region Abnormal Tests (A)

        [Fact]
        public async Task GetHistoryAsync_ShouldThrowKeyNotFoundException_WhenAccountDoesNotExist()
        {
            // Arrange
            string accountId = "invalid_acc";
            _accountService.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync((Account?)null);

            // Act
            var act = async () => await _service.GetHistoryAsync(accountId);

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("Account does not exist.");

            _accountService.Verify(x => x.GetByIdAsync(accountId), Times.Once);
            _reportRepository.Verify(x => x.GetReportsByReporterAsync(It.IsAny<string>()), Times.Never);
        }

        #endregion

        #region Boundary Tests (B)

        [Fact]
        public async Task GetHistoryAsync_ShouldReturnEmptyHistory_WhenReporterHasNoSubmittedOrReceivedReports()
        {
            // Arrange
            string accountId = "acc_id";
            string userId = "user_id";
            var account = new Account { AccountId = accountId, UserId = userId };

            _accountService.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);
            _reportRepository.Setup(x => x.GetReportsByReporterAsync(userId)).ReturnsAsync(new List<Report>());
            _reportRepository.Setup(x => x.GetReportsReceivedByUserAsync(userId)).ReturnsAsync(new List<Report>());

            // Act
            var result = await _service.GetHistoryAsync(accountId);

            // Assert
            result.Should().NotBeNull();
            result.ReportsSubmitted.Should().BeEmpty();
            result.ReportsReceived.Should().BeEmpty();

            _accountService.Verify(x => x.GetByIdAsync(accountId), Times.Once);
            _reportRepository.Verify(x => x.GetReportsByReporterAsync(userId), Times.Once);
            _reportRepository.Verify(x => x.GetReportsReceivedByUserAsync(userId), Times.Once);
        }

        #endregion
    }
}
