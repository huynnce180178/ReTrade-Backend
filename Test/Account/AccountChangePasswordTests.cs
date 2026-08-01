using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using RetradeBE.Config;
using RetradeBE.Hubs;
using RetradeBE.Models;
using RetradeBE.Models.DTOs;
using RetradeBE.Repositories;
using RetradeBE.Services;
using Xunit;

namespace Test.AccountTests
{
    public class AccountChangePasswordTests
    {
        private readonly Mock<IUserRepository> _userRepo;
        private readonly Mock<IAccountRepository> _accountRepo;
        private readonly Mock<IEmailService> _emailService;
        private readonly MemoryCache _memoryCache;
        private readonly Mock<AutoMapper.IMapper> _mapper;
        private readonly Mock<System.Net.Http.IHttpClientFactory> _httpFactory;
        private readonly Mock<IWebHostEnvironment> _env;
        private readonly Mock<IHubContext<AccountHub>> _hub;
        private readonly Mock<ILogger<AccountService>> _logger;
        private readonly AccountService _service;

        public AccountChangePasswordTests()
        {
            _userRepo = new Mock<IUserRepository>();
            _accountRepo = new Mock<IAccountRepository>();
            _emailService = new Mock<IEmailService>();
            _memoryCache = new MemoryCache(new MemoryCacheOptions());
            _mapper = new Mock<AutoMapper.IMapper>();
            _httpFactory = new Mock<System.Net.Http.IHttpClientFactory>();
            _env = new Mock<IWebHostEnvironment>();
            _env.Setup(x => x.ContentRootPath).Returns(AppContext.BaseDirectory);
            _hub = new Mock<IHubContext<AccountHub>>();
            _logger = new Mock<ILogger<AccountService>>();

            var jwtSettings = new JwtSettings
            {
                SecretKey = "ThisIsAValidJwtSecretKey1234567890",
                Issuer = "TestIssuer",
                Audience = "TestAudience",
                ExpiryMinutes = 60
            };

            _service = new AccountService(
                _accountRepo.Object,
                _userRepo.Object,
                _emailService.Object,
                _memoryCache,
                Options.Create(jwtSettings),
                Options.Create(new GoogleSettings()),
                _httpFactory.Object,
                _mapper.Object,
                _env.Object,
                _hub.Object,
                _logger.Object);
        }

        #region Normal Tests (N)

        [Fact]
        public async Task ChangePasswordAsync_ReturnsSuccess_WithValidOldAndNewPassword()
        {
            // Arrange (UTCID01)
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

            var dto = new ChangePasswordDto
            {
                OldPassword = "OldPassword1!",
                NewPassword = "User1234@"
            };

            // Act
            var result = await _service.ChangePasswordAsync(accountId, dto);

            // Assert
            result.Should().Be("Password changed successfully.");
            _accountRepo.Verify(x => x.UpdateAsync(It.Is<Account>(a => a.AccountId == accountId && a.MustChangePassword == false && a.IsPasswordSet == true)), Times.Once);
        }

        #endregion

        #region Abnormal Tests (A)

        [Fact]
        public async Task ChangePasswordAsync_ReturnsError_WhenOldPasswordIsIncorrect()
        {
            // Arrange (UTCID02)
            var accountId = "A2";
            var account = new Account
            {
                AccountId = accountId,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("CorrectOld1!"),
                IsPasswordSet = true,
                MustChangePassword = true
            };

            _accountRepo.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);

            var dto = new ChangePasswordDto
            {
                OldPassword = "WrongOld1!",
                NewPassword = "User1234@"
            };

            // Act
            var result = await _service.ChangePasswordAsync(accountId, dto);

            // Assert
            result.Should().Be("Old password is incorrect.");
            _accountRepo.Verify(x => x.UpdateAsync(It.IsAny<Account>()), Times.Never);
        }

        [Fact]
        public async Task ChangePasswordAsync_ReturnsError_WhenNewPasswordMissingSpecialCharacter()
        {
            // Arrange (UTCID03)
            var accountId = "A3";
            var account = new Account
            {
                AccountId = accountId,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("OldPassword1!"),
                IsPasswordSet = true,
                MustChangePassword = true
            };

            _accountRepo.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);

            var dto = new ChangePasswordDto
            {
                OldPassword = "OldPassword1!",
                NewPassword = "User12345"
            };

            // Act
            var result = await _service.ChangePasswordAsync(accountId, dto);

            // Assert
            result.Should().Be("Password must contain at least one special character.");
            _accountRepo.Verify(x => x.UpdateAsync(It.IsAny<Account>()), Times.Never);
        }

        [Fact]
        public async Task ChangePasswordAsync_ReturnsError_WhenNewPasswordMissingUppercaseLetter()
        {
            // Arrange (UTCID04)
            var accountId = "A4";
            var account = new Account
            {
                AccountId = accountId,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("OldPassword1!"),
                IsPasswordSet = true,
                MustChangePassword = true
            };

            _accountRepo.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);

            var dto = new ChangePasswordDto
            {
                OldPassword = "OldPassword1!",
                NewPassword = "user12345@"
            };

            // Act
            var result = await _service.ChangePasswordAsync(accountId, dto);

            // Assert
            result.Should().Be("Password must contain at least one uppercase letter.");
            _accountRepo.Verify(x => x.UpdateAsync(It.IsAny<Account>()), Times.Never);
        }

        #endregion

        #region Boundary Tests (B)

        [Fact]
        public async Task ChangePasswordAsync_ReturnsError_WhenNewPasswordTooShort()
        {
            // Arrange (UTCID05)
            var accountId = "A5";
            var account = new Account
            {
                AccountId = accountId,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("OldPassword1!"),
                IsPasswordSet = true,
                MustChangePassword = true
            };

            _accountRepo.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);

            var dto = new ChangePasswordDto
            {
                OldPassword = "OldPassword1!",
                NewPassword = "User12@"
            };

            // Act
            var result = await _service.ChangePasswordAsync(accountId, dto);

            // Assert
            result.Should().Be("Password must be at least 8 characters long.");
            _accountRepo.Verify(x => x.UpdateAsync(It.IsAny<Account>()), Times.Never);
        }

        [Fact]
        public async Task ChangePasswordAsync_ReturnsSuccess_With50CharacterValidPassword()
        {
            // Arrange (UTCID06)
            var accountId = "A6";
            var account = new Account
            {
                AccountId = accountId,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("OldPassword1!"),
                IsPasswordSet = true,
                MustChangePassword = true
            };

            _accountRepo.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);
            _accountRepo.Setup(x => x.UpdateAsync(It.IsAny<Account>())).Returns(Task.CompletedTask);

            var pass50 = new string('A', 48) + "1!";
            var dto = new ChangePasswordDto
            {
                OldPassword = "OldPassword1!",
                NewPassword = pass50
            };

            // Act
            var result = await _service.ChangePasswordAsync(accountId, dto);

            // Assert
            result.Should().Be("Password changed successfully.");
            _accountRepo.Verify(x => x.UpdateAsync(It.Is<Account>(a => a.AccountId == accountId)), Times.Once);
        }

        [Fact]
        public async Task ChangePasswordAsync_ReturnsError_WhenNewPasswordTooLong()
        {
            // Arrange (UTCID07)
            var accountId = "A7";
            var account = new Account
            {
                AccountId = accountId,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("OldPassword1!"),
                IsPasswordSet = true,
                MustChangePassword = true
            };

            _accountRepo.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);

            var pass51 = new string('A', 49) + "1!";
            var dto = new ChangePasswordDto
            {
                OldPassword = "OldPassword1!",
                NewPassword = pass51
            };

            // Act
            var result = await _service.ChangePasswordAsync(accountId, dto);

            // Assert
            result.Should().Be("Password exceeds the maximum allowed length.");
            _accountRepo.Verify(x => x.UpdateAsync(It.IsAny<Account>()), Times.Never);
        }

        #endregion
    }
}
