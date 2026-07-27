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
    public class AccountChangePasswordTests
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

        public AccountChangePasswordTests()
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
        public async Task ChangePasswordAsync_ReturnsSuccess_WithValidOldAndNewPassword()
        {
            var accountId = "A1";
            var account = new Account
            {
                AccountId = accountId,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("OldPassword1!"),
                IsPasswordSet = true,
                MustChangePassword = true
            };

            _accountRepo.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);
            _accountRepo.Setup(x => x.UpdateAsync(It.IsAny<Account>())).Returns(Task.CompletedTask);

            var dto = new RetradeBE.Models.DTOs.ChangePasswordDto
            {
                OldPassword = "OldPassword1!",
                NewPassword = "NewPassword1@"
            };

            var result = await _service.ChangePasswordAsync(accountId, dto);

            result.Should().Be("Password changed successfully.");
            _accountRepo.Verify(x => x.UpdateAsync(It.Is<Account>(a => a.AccountId == accountId && a.MustChangePassword == false && a.IsPasswordSet == true)), Times.Once);
        }

        [Fact]
        public async Task ChangePasswordAsync_ReturnsError_WhenOldPasswordIsIncorrect()
        {
            var accountId = "A2";
            var account = new Account
            {
                AccountId = accountId,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("CorrectOld1!"),
                IsPasswordSet = true,
                MustChangePassword = true
            };

            _accountRepo.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);

            var dto = new RetradeBE.Models.DTOs.ChangePasswordDto
            {
                OldPassword = "WrongOld1!",
                NewPassword = "NewPassword1@"
            };

            var result = await _service.ChangePasswordAsync(accountId, dto);

            result.Should().Be("Old password is incorrect.");
            _accountRepo.Verify(x => x.UpdateAsync(It.IsAny<Account>()), Times.Never);
        }

        [Fact]
        public async Task ChangePasswordAsync_ReturnsError_WhenAccountNotFound()
        {
            var accountId = "A3";
            _accountRepo.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync((Account?)null);

            var dto = new RetradeBE.Models.DTOs.ChangePasswordDto
            {
                OldPassword = "OldPassword1!",
                NewPassword = "NewPassword1@"
            };

            var result = await _service.ChangePasswordAsync(accountId, dto);

            result.Should().Be("Account not found.");
            _accountRepo.Verify(x => x.UpdateAsync(It.IsAny<Account>()), Times.Never);
        }

        [Fact]
        public async Task ChangePasswordAsync_ReturnsError_WhenNewPasswordMissingSpecialCharacter()
        {
            var accountId = "A4";
            var account = new Account
            {
                AccountId = accountId,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("OldPassword1!"),
                IsPasswordSet = true,
                MustChangePassword = true
            };

            _accountRepo.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);

            var dto = new RetradeBE.Models.DTOs.ChangePasswordDto
            {
                OldPassword = "OldPassword1!",
                NewPassword = "User12345"
            };

            var result = await _service.ChangePasswordAsync(accountId, dto);

            result.Should().Be("Password must contain at least one special character.");
            _accountRepo.Verify(x => x.UpdateAsync(It.IsAny<Account>()), Times.Never);
        }

        [Fact]
        public async Task ChangePasswordAsync_ReturnsError_WhenNewPasswordMissingUppercaseLetter()
        {
            var accountId = "A5";
            var account = new Account
            {
                AccountId = accountId,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("OldPassword1!"),
                IsPasswordSet = true,
                MustChangePassword = true
            };

            _accountRepo.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);

            var dto = new RetradeBE.Models.DTOs.ChangePasswordDto
            {
                OldPassword = "OldPassword1!",
                NewPassword = "user12345@"
            };

            var result = await _service.ChangePasswordAsync(accountId, dto);

            result.Should().Be("Password must contain at least one uppercase letter.");
            _accountRepo.Verify(x => x.UpdateAsync(It.IsAny<Account>()), Times.Never);
        }

        [Fact]
        public async Task ChangePasswordAsync_ReturnsError_WhenNewPasswordTooShort()
        {
            var accountId = "A6";
            var account = new Account
            {
                AccountId = accountId,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("OldPassword1!"),
                IsPasswordSet = true,
                MustChangePassword = true
            };

            _accountRepo.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);

            var dto = new RetradeBE.Models.DTOs.ChangePasswordDto
            {
                OldPassword = "OldPassword1!",
                NewPassword = "User1@"
            };

            var result = await _service.ChangePasswordAsync(accountId, dto);

            result.Should().Be("Password must be at least 8 characters long.");
            _accountRepo.Verify(x => x.UpdateAsync(It.IsAny<Account>()), Times.Never);
        }

        [Fact]
        public async Task ChangePasswordAsync_ReturnsError_WhenNewPasswordTooLong()
        {
            var accountId = "A7";
            var account = new Account
            {
                AccountId = accountId,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("OldPassword1!"),
                IsPasswordSet = true,
                MustChangePassword = true
            };

            _accountRepo.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);

            var longPassword = new string('A', 51) + "@1";
            var dto = new RetradeBE.Models.DTOs.ChangePasswordDto
            {
                OldPassword = "OldPassword1!",
                NewPassword = longPassword
            };

            var result = await _service.ChangePasswordAsync(accountId, dto);

            result.Should().Be("Password exceeds the maximum allowed length.");
            _accountRepo.Verify(x => x.UpdateAsync(It.IsAny<Account>()), Times.Never);
        }
    }
}
