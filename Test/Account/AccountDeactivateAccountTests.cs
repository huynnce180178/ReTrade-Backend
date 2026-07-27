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
using Microsoft.AspNetCore.SignalR.Protocol;
using RetradeBE.Hubs;
using System;

namespace Test.AccountTests
{
    public class AccountDeactivateAccountTests
    {
        private readonly Mock<IUserRepository> _userRepo;
        private readonly Mock<IAccountRepository> _accountRepo;
        private readonly Mock<IEmailService> _emailService;
        private readonly MemoryCache _memoryCache;
        private readonly Mock<IMapper> _mapper;
        private readonly Mock<System.Net.Http.IHttpClientFactory> _httpFactory;
        private readonly Mock<Microsoft.AspNetCore.Hosting.IWebHostEnvironment> _env;
        private readonly Mock<IHubContext<AccountHub>> _hub;
        private readonly Mock<IHubClients> _hubClients;
        private readonly Mock<IClientProxy> _clientProxy;
        private readonly Mock<ILogger<RetradeBE.Services.AccountService>> _logger;
        private readonly RetradeBE.Services.AccountService _service;

        public AccountDeactivateAccountTests()
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
            _hubClients = new Mock<IHubClients>();
            _clientProxy = new Mock<IClientProxy>();
            _hubClients.Setup(x => x.Group(It.IsAny<string>())).Returns(_clientProxy.Object);
            _hub.SetupGet(x => x.Clients).Returns(_hubClients.Object);

            _logger = new Mock<ILogger<RetradeBE.Services.AccountService>>();

            var jwtSettings = new JwtSettings
            {
                SecretKey = "ThisIsAValidJwtSecretKey1234567890",
                Issuer = "TestIssuer",
                Audience = "TestAudience",
                ExpiryMinutes = 60
            };

            _service = new RetradeBE.Services.AccountService(
                _accountRepo.Object,
                _userRepo.Object,
                _emailService.Object,
                _memoryCache,
                Options.Create(jwtSettings),
                Options.Create(new RetradeBE.Config.GoogleSettings()),
                _httpFactory.Object,
                _mapper.Object,
                _env.Object,
                _hub.Object,
                _logger.Object);
        }

        [Fact]
        public async Task DeactivateMyAccountAsync_ReturnsTrue_WhenActiveAccountIdIsValid()
        {
            var accountId = "A1";
            var account = new Account
            {
                AccountId = accountId,
                Status = RetradeBE.Models.Enums.AccountStatusEnum.Active.ToString(),
                UserId = "U1",
                Username = "activeuser"
            };

            _accountRepo.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);
            _accountRepo.Setup(x => x.UpdateAsync(It.IsAny<Account>())).Returns(Task.CompletedTask);
            _userRepo.Setup(x => x.GetByIdAsync(account.UserId)).ReturnsAsync(new User { UserId = account.UserId, Email = "user@example.com", FirstName = "Test" });
            _emailService.Setup(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);
            _clientProxy.Setup(x => x.SendCoreAsync(It.IsAny<string>(), It.IsAny<object?[]>(), default)).Returns(Task.CompletedTask);

            var result = await _service.DeactivateMyAccountAsync(accountId);

            result.Should().BeTrue();
            _accountRepo.Verify(x => x.UpdateAsync(It.Is<Account>(a => a.AccountId == accountId && a.Status == RetradeBE.Models.Enums.AccountStatusEnum.Inactive.ToString())), Times.Once);
            _hubClients.Verify(x => x.Group(It.IsAny<string>()), Times.Once);
            _clientProxy.Verify(x => x.SendCoreAsync("ForceLogout", It.IsAny<object?[]>(), default), Times.Once);
        }

        [Fact]
        public async Task DeactivateMyAccountAsync_ReturnsFalse_WhenAccountIdIsInvalid()
        {
            var accountId = "invalid";
            _accountRepo.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync((Account?)null);

            var result = await _service.DeactivateMyAccountAsync(accountId);

            result.Should().BeFalse();
            _accountRepo.Verify(x => x.UpdateAsync(It.IsAny<Account>()), Times.Never);
            _hubClients.Verify(x => x.Group(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task DeactivateMyAccountAsync_ReturnsFalse_WhenAccountIsAlreadyInactive()
        {
            var accountId = "A2";
            var account = new Account
            {
                AccountId = accountId,
                Status = RetradeBE.Models.Enums.AccountStatusEnum.Inactive.ToString(),
                UserId = "U2",
                Username = "inactiveuser"
            };

            _accountRepo.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);

            var result = await _service.DeactivateMyAccountAsync(accountId);

            result.Should().BeFalse();
            _accountRepo.Verify(x => x.UpdateAsync(It.IsAny<Account>()), Times.Never);
            _hubClients.Verify(x => x.Group(It.IsAny<string>()), Times.Never);
        }
    }
}
