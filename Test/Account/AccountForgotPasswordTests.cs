using System.Threading.Tasks;
using Xunit;
using Moq;
using FluentAssertions;
using RetradeBE.Services;
using RetradeBE.Repositories;
using RetradeBE.Models;
using AutoMapper;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using RetradeBE.Config;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.SignalR;
using RetradeBE.Hubs;
using System;

namespace Test.AccountTests
{
    public class AccountForgotPasswordTests
    {
        private readonly Mock<IUserRepository> _userRepo;
        private readonly Mock<IAccountRepository> _accountRepo;
        private readonly Mock<IEmailService> _emailService;
        private readonly MemoryCache _memoryCache;
        private readonly Mock<IMapper> _mapper;
        private readonly Mock<System.Net.Http.IHttpClientFactory> _httpFactory;
        private readonly Mock<Microsoft.AspNetCore.Hosting.IWebHostEnvironment> _env;
        private readonly Mock<IHubContext<AccountHub>> _hub;
        private readonly Mock<ILogger<RetradeBE.Services.AccountService>> _logger;
        private readonly RetradeBE.Services.AccountService _service;

        public AccountForgotPasswordTests()
        {
            _userRepo = new Mock<IUserRepository>();
            _accountRepo = new Mock<IAccountRepository>();
            _emailService = new Mock<IEmailService>();
            _memoryCache = new MemoryCache(new MemoryCacheOptions());
            _mapper = new Mock<IMapper>();
            _httpFactory = new Mock<System.Net.Http.IHttpClientFactory>();
            _env = new Mock<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>();
            _env.Setup(x => x.ContentRootPath).Returns(AppContext.BaseDirectory);
            _hub = new Mock<IHubContext<AccountHub>>();
            _logger = new Mock<ILogger<RetradeBE.Services.AccountService>>();

            _service = new RetradeBE.Services.AccountService(
                _accountRepo.Object,
                _userRepo.Object,
                _emailService.Object,
                _memoryCache,
                Options.Create(new JwtSettings()),
                Options.Create(new RetradeBE.Config.GoogleSettings()),
                _httpFactory.Object,
                _mapper.Object,
                _env.Object,
                _hub.Object,
                _logger.Object);
        }

        [Fact]
        public async Task ForgotPasswordAsync_ReturnsOtpSent_WhenEmailExists()
        {
            var email = "existing@example.com";
            var user = new User { UserId = "U1", Email = email };
            var account = new Account { AccountId = "A1", UserId = user.UserId, Status = "Active" };

            _userRepo.Setup(x => x.GetByEmailAsync(email)).ReturnsAsync(user);
            _accountRepo.Setup(x => x.GetAllAsync()).ReturnsAsync(new[] { account });
            _emailService.Setup(x => x.SendEmailAsync(email, It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);

            var result = await _service.ForgotPasswordAsync(email);

            result.Should().Be("Password reset OTP has been sent to your email.");
            _emailService.Verify(x => x.SendEmailAsync(email, It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task ForgotPasswordAsync_ReturnsEmailNotFound_WhenEmailDoesNotExist()
        {
            var email = "missing@example.com";
            _userRepo.Setup(x => x.GetByEmailAsync(email)).ReturnsAsync((User?)null);

            var result = await _service.ForgotPasswordAsync(email);

            result.Should().Be("Email not found.");
            _emailService.Verify(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }
    }
}
