using System.Threading.Tasks;
using Xunit;
using Moq;
using FluentAssertions;
using RetradeBE.Services;
using RetradeBE.Models.DTOs;
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
    public class AccountVerifyTests
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

        public AccountVerifyTests()
        {
            _userRepo = new Mock<IUserRepository>();
            _accountRepo = new Mock<IAccountRepository>();
            _emailService = new Mock<IEmailService>();
            _memoryCache = new MemoryCache(new MemoryCacheOptions());
            _mapper = new Mock<IMapper>();
            _httpFactory = new Mock<System.Net.Http.IHttpClientFactory>();
            _env = new Mock<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>();
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
        public async Task VerifyAsync_ReturnsFalse_WhenOtpIsIncorrect_ButAccountAndUserExist()
        {
            var email = "verifyuser@example.com";
            var correctOtp = "123456";
            var wrongOtp = "654321";
            var user = new User { UserId = "U1", Email = email };
            var account = new Account { AccountId = "A1", UserId = user.UserId, Status = "Pending" };

            _memoryCache.Set(email, correctOtp, TimeSpan.FromMinutes(3));
            _userRepo.Setup(x => x.GetByEmailAsync(email)).ReturnsAsync(user);
            _accountRepo.Setup(x => x.GetAllAsync()).ReturnsAsync(new[] { account });

            var dto = new VerifyDto { Email = email, Otp = wrongOtp };

            var result = await _service.VerifyAsync(dto);

            result.Should().BeFalse();
            _accountRepo.Verify(x => x.UpdateAsync(It.IsAny<Account>()), Times.Never);
            _memoryCache.TryGetValue(email, out string? cachedOtp).Should().BeTrue();
            cachedOtp.Should().Be(correctOtp);
        }

        [Fact]
        public async Task VerifyAsync_ReturnsTrue_WhenOtpIsCorrect_AndAccountExists()
        {
            var email = "verifyuser@example.com";
            var correctOtp = "123456";
            var user = new User { UserId = "U1", Email = email };
            var account = new Account { AccountId = "A1", UserId = user.UserId, Status = "Pending" };

            _memoryCache.Set(email, correctOtp, TimeSpan.FromMinutes(3));
            _userRepo.Setup(x => x.GetByEmailAsync(email)).ReturnsAsync(user);
            _accountRepo.Setup(x => x.GetAllAsync()).ReturnsAsync(new[] { account });
            _accountRepo.Setup(x => x.UpdateAsync(It.IsAny<Account>())).Returns(Task.CompletedTask);

            var dto = new VerifyDto { Email = email, Otp = correctOtp };

            var result = await _service.VerifyAsync(dto);

            result.Should().BeTrue();
            _accountRepo.Verify(x => x.UpdateAsync(It.Is<Account>(a => a.AccountId == account.AccountId && a.Status == RetradeBE.Models.Enums.AccountStatusEnum.Active.ToString())), Times.Once);
            _memoryCache.TryGetValue(email, out object? unused).Should().BeFalse();
        }

    }
}
