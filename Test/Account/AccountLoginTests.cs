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
    public class AccountLoginTests
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

        public AccountLoginTests()
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
        public async Task LoginAsync_ReturnsAuthResponse_WhenLoginWithEmailAndPasswordIsValid()
        {
            var email = "user@example.com";
            var password = "User1234!";
            var user = new User { UserId = "U1", Email = email };
            var account = new Account
            {
                AccountId = "A1",
                UserId = user.UserId,
                Username = "validuser",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                Status = RetradeBE.Models.Enums.AccountStatusEnum.Active.ToString(),
                IsDeleted = false
            };

            _userRepo.Setup(x => x.GetByEmailAsync(email)).ReturnsAsync(user);
            _accountRepo.Setup(x => x.GetAllAsync()).ReturnsAsync(new[] { account });
            _accountRepo.Setup(x => x.GetRolesAsync(account.AccountId)).ReturnsAsync(new List<string> { RetradeBE.Models.Enums.RoleEnum.Buyer.ToString() });
            _userRepo.Setup(x => x.GetByIdAsync(account.UserId)).ReturnsAsync(user);

            var dto = new RetradeBE.Models.DTOs.LoginDto { Username = email, Password = password };
            var result = await _service.LoginAsync(dto);

            result.Should().NotBeNull();
            result.Should().BeOfType<RetradeBE.Models.DTOs.AuthResponseDto>();
            var auth = (RetradeBE.Models.DTOs.AuthResponseDto)result!;
            auth.AccountId.Should().Be(account.AccountId);
            auth.Email.Should().Be(email);
        }

        [Fact]
        public async Task LoginAsync_ReturnsAuthResponse_WhenLoginWithUsernameAndPasswordIsValid()
        {
            var username = "validuser";
            var password = "User1234!";
            var account = new Account
            {
                AccountId = "A2",
                Username = username,
                UserId = "U2",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                Status = RetradeBE.Models.Enums.AccountStatusEnum.Active.ToString(),
                IsDeleted = false
            };
            var user = new User { UserId = account.UserId };

            _accountRepo.Setup(x => x.GetByUsernameAsync(username)).ReturnsAsync(account);
            _accountRepo.Setup(x => x.GetRolesAsync(account.AccountId)).ReturnsAsync(new List<string> { RetradeBE.Models.Enums.RoleEnum.Buyer.ToString() });
            _userRepo.Setup(x => x.GetByIdAsync(account.UserId)).ReturnsAsync(user);

            var dto = new RetradeBE.Models.DTOs.LoginDto { Username = username, Password = password };
            var result = await _service.LoginAsync(dto);

            result.Should().NotBeNull();
            result.Should().BeOfType<RetradeBE.Models.DTOs.AuthResponseDto>();
            var auth = (RetradeBE.Models.DTOs.AuthResponseDto)result!;
            auth.AccountId.Should().Be(account.AccountId);
            auth.Username.Should().Be(username);
        }

        [Fact]
        public async Task LoginAsync_ReturnsNull_WhenEmailDoesNotExist()
        {
            var email = "missing@example.com";
            _userRepo.Setup(x => x.GetByEmailAsync(email)).ReturnsAsync((User?)null);

            var dto = new RetradeBE.Models.DTOs.LoginDto { Username = email, Password = "User1234!" };
            var result = await _service.LoginAsync(dto);

            result.Should().BeNull();
        }

        [Fact]
        public async Task LoginAsync_ReturnsNull_WhenUsernameDoesNotExist()
        {
            var username = "unknownuser";
            _accountRepo.Setup(x => x.GetByUsernameAsync(username)).ReturnsAsync((Account?)null);

            var dto = new RetradeBE.Models.DTOs.LoginDto { Username = username, Password = "User1234!" };
            var result = await _service.LoginAsync(dto);

            result.Should().BeNull();
        }

        [Fact]
        public async Task LoginAsync_ThrowsInvalidOperationException_WhenAccountIsInactive()
        {
            var username = "inactiveuser";
            var account = new Account
            {
                AccountId = "A3",
                Username = username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("User1234!"),
                Status = RetradeBE.Models.Enums.AccountStatusEnum.Inactive.ToString(),
                IsDeleted = false
            };

            _accountRepo.Setup(x => x.GetByUsernameAsync(username)).ReturnsAsync(account);

            var dto = new RetradeBE.Models.DTOs.LoginDto { Username = username, Password = "User1234!" };
            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.LoginAsync(dto));
        }

        [Fact]
        public async Task LoginAsync_ReturnsNull_WhenPasswordIsIncorrect()
        {
            var username = "validuser";
            var account = new Account
            {
                AccountId = "A4",
                Username = username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("CorrectP@ss1"),
                Status = RetradeBE.Models.Enums.AccountStatusEnum.Active.ToString(),
                IsDeleted = false
            };

            _accountRepo.Setup(x => x.GetByUsernameAsync(username)).ReturnsAsync(account);

            var dto = new RetradeBE.Models.DTOs.LoginDto { Username = username, Password = "WrongPass1!" };
            var result = await _service.LoginAsync(dto);

            result.Should().BeNull();
        }

        [Fact]
        public async Task LoginAsync_ReturnsNull_WhenAccountIsDeleted()
        {
            var username = "deleteduser";
            var account = new Account
            {
                AccountId = "A5",
                Username = username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("User1234!"),
                Status = RetradeBE.Models.Enums.AccountStatusEnum.Active.ToString(),
                IsDeleted = true
            };

            _accountRepo.Setup(x => x.GetByUsernameAsync(username)).ReturnsAsync(account);

            var dto = new RetradeBE.Models.DTOs.LoginDto { Username = username, Password = "User1234!" };
            var result = await _service.LoginAsync(dto);

            result.Should().BeNull();
        }

        [Fact]
        public async Task LoginAsync_ReturnsNull_WhenAccountIsPending()
        {
            var username = "pendinguser";
            var account = new Account
            {
                AccountId = "A6",
                Username = username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("User1234!"),
                Status = RetradeBE.Models.Enums.AccountStatusEnum.Pending.ToString(),
                IsDeleted = false
            };

            _accountRepo.Setup(x => x.GetByUsernameAsync(username)).ReturnsAsync(account);

            var dto = new RetradeBE.Models.DTOs.LoginDto { Username = username, Password = "User1234!" };
            var result = await _service.LoginAsync(dto);

            result.Should().BeNull();
        }
    }
}
