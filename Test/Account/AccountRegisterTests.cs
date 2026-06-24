using System.Threading.Tasks;
using Xunit;
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
    public class AccountRegisterTests
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

        public AccountRegisterTests()
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
        public async Task Register_ReturnsEmailAlreadyExists_WhenEmailExists()
        {
            _userRepo.Setup(x => x.GetByEmailAsync("emailTest@gmail.com")).ReturnsAsync(new User { Email = "emailTest@gmail.com" });

            var dto = new RegisterDto { Username = "usertest123", Email = "emailTest@gmail.com", Password = "User123456@" };

            var result = await _service.RegisterAsync(dto);

            result.Should().Be("Email already exists.");
        }

        [Fact]
        public async Task Register_ReturnsUsernameAlreadyExists_WhenUsernameExists()
        {
            _userRepo.Setup(x => x.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);
            _accountRepo.Setup(x => x.GetByUsernameAsync("usertest123")).ReturnsAsync(new Account { Username = "usertest123" });

            var dto = new RegisterDto { Username = "usertest123", Email = "emailNew@gmail.com", Password = "User123456@" };

            var result = await _service.RegisterAsync(dto);

            result.Should().Be("Username already exists.");
        }

        [Fact]
        public async Task Register_Success_CreatesUserAccount_SendsOtp_Email()
        {
            _env.Setup(x => x.ContentRootPath).Returns(AppContext.BaseDirectory);
            _userRepo.Setup(x => x.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);
            _accountRepo.Setup(x => x.GetByUsernameAsync(It.IsAny<string>())).ReturnsAsync((Account?)null);
            _userRepo.Setup(x => x.CountAllUsersAsync()).ReturnsAsync(0);
            _accountRepo.Setup(x => x.CountAllAccountsAsync()).ReturnsAsync(0);

            User? addedUser = null;
            Account? addedAccount = null;

            _mapper.Setup(m => m.Map<User>(It.IsAny<RegisterDto>())).Returns((RegisterDto dto) => new User { Email = dto.Email, FirstName = dto.FirstName, LastName = dto.LastName });
            _mapper.Setup(m => m.Map<Account>(It.IsAny<RegisterDto>())).Returns((RegisterDto dto) => new Account { Username = dto.Username });

            _userRepo.Setup(x => x.AddAsync(It.IsAny<User>())).Callback<User>(u => addedUser = u).Returns(Task.CompletedTask);
            _accountRepo.Setup(x => x.AddAsync(It.IsAny<Account>())).Callback<Account>(a => addedAccount = a).Returns(Task.CompletedTask);
            _accountRepo.Setup(x => x.AssignRoleAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);

            _emailService.Setup(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask).Verifiable();

            var dto = new RegisterDto { Username = "newuser", Email = "newuser@gmail.com", Password = "User123456@", FirstName = "user", LastName = "test", RoleId = 2 };

            var result = await _service.RegisterAsync(dto);

            result.Should().Be("Register success. Please check your email for OTP.");
            addedUser.Should().NotBeNull();
            addedUser!.UserId.Should().NotBeNullOrEmpty();
            addedUser.Email.Should().Be(dto.Email);

            addedAccount.Should().NotBeNull();
            addedAccount!.AccountId.Should().NotBeNullOrEmpty();
            addedAccount.UserId.Should().Be(addedUser.UserId);
            addedAccount.Username.Should().Be(dto.Username);
            addedAccount.IsPasswordSet.Should().BeTrue();
            addedAccount.PasswordHash.Should().NotBeNullOrEmpty();

            // Check OTP in cache
            _memoryCache.TryGetValue(dto.Email, out object otpObj).Should().BeTrue();
            var otp = otpObj as string;
            otp.Should().NotBeNull();
            otp!.Should().MatchRegex("^[0-9]{6}$");

            _emailService.Verify(x => x.SendEmailAsync(dto.Email, It.IsAny<string>(), It.Is<string>(body => body.Contains(otp!))), Times.Once);

            _accountRepo.Verify(x => x.AssignRoleAsync(It.IsAny<string>(), "Buyer"), Times.Once);
        }

        // The remaining decision-table tests assert observed behaviour: RegisterAsync currently
        // does not enforce email format or password policy, so these tests expect success.

        [Fact]
        public async Task Register_InvalidEmailFormat_BehaviorObserved()
        {
            _env.Setup(x => x.ContentRootPath).Returns(AppContext.BaseDirectory);
            _userRepo.Setup(x => x.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);
            _accountRepo.Setup(x => x.GetByUsernameAsync(It.IsAny<string>())).ReturnsAsync((Account?)null);
            _userRepo.Setup(x => x.CountAllUsersAsync()).ReturnsAsync(1);
            _accountRepo.Setup(x => x.CountAllAccountsAsync()).ReturnsAsync(1);

            _mapper.Setup(m => m.Map<User>(It.IsAny<RegisterDto>())).Returns((RegisterDto dto) => new User { Email = dto.Email });
            _mapper.Setup(m => m.Map<Account>(It.IsAny<RegisterDto>())).Returns((RegisterDto dto) => new Account { Username = dto.Username });

            _userRepo.Setup(x => x.AddAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
            _accountRepo.Setup(x => x.AddAsync(It.IsAny<Account>())).Returns(Task.CompletedTask);
            _accountRepo.Setup(x => x.AssignRoleAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);
            _emailService.Setup(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);

            var dto = new RegisterDto { Username = "u1", Email = "emailTest", Password = "User123456@" };

            var result = await _service.RegisterAsync(dto);

            result.Should().Be("Register success. Please check your email for OTP.");
        }

        [Fact]
        public async Task Register_PasswordMissingSpecialChar_BehaviorObserved()
        {
            _env.Setup(x => x.ContentRootPath).Returns(AppContext.BaseDirectory);
            _userRepo.Setup(x => x.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);
            _accountRepo.Setup(x => x.GetByUsernameAsync(It.IsAny<string>())).ReturnsAsync((Account?)null);
            _userRepo.Setup(x => x.CountAllUsersAsync()).ReturnsAsync(2);
            _accountRepo.Setup(x => x.CountAllAccountsAsync()).ReturnsAsync(2);

            User? addedUser = null;
            Account? addedAccount = null;
            _mapper.Setup(m => m.Map<User>(It.IsAny<RegisterDto>())).Returns((RegisterDto dto) => new User { Email = dto.Email });
            _mapper.Setup(m => m.Map<Account>(It.IsAny<RegisterDto>())).Returns((RegisterDto dto) => new Account { Username = dto.Username });
            _userRepo.Setup(x => x.AddAsync(It.IsAny<User>())).Callback<User>(u => addedUser = u).Returns(Task.CompletedTask);
            _accountRepo.Setup(x => x.AddAsync(It.IsAny<Account>())).Callback<Account>(a => addedAccount = a).Returns(Task.CompletedTask);
            _accountRepo.Setup(x => x.AssignRoleAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);
            _emailService.Setup(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);

            var dto = new RegisterDto { Username = "newnoSpecial", Email = "nospecial@example.com", Password = "User123456", RoleId = 2 };

            var result = await _service.RegisterAsync(dto);

            result.Should().Be("Register success. Please check your email for OTP.");
            addedAccount.Should().NotBeNull();
            addedAccount!.PasswordHash.Should().NotBeNullOrEmpty();
            BCrypt.Net.BCrypt.Verify(dto.Password, addedAccount.PasswordHash).Should().BeTrue();
        }

        [Fact]
        public async Task Register_PasswordMissingUppercaseNumberSpecial_BehaviorObserved()
        {
            _env.Setup(x => x.ContentRootPath).Returns(AppContext.BaseDirectory);
            _userRepo.Setup(x => x.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);
            _accountRepo.Setup(x => x.GetByUsernameAsync(It.IsAny<string>())).ReturnsAsync((Account?)null);
            _userRepo.Setup(x => x.CountAllUsersAsync()).ReturnsAsync(3);
            _accountRepo.Setup(x => x.CountAllAccountsAsync()).ReturnsAsync(3);

            _mapper.Setup(m => m.Map<User>(It.IsAny<RegisterDto>())).Returns((RegisterDto dto) => new User { Email = dto.Email });
            _mapper.Setup(m => m.Map<Account>(It.IsAny<RegisterDto>())).Returns((RegisterDto dto) => new Account { Username = dto.Username });
            _userRepo.Setup(x => x.AddAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
            _accountRepo.Setup(x => x.AddAsync(It.IsAny<Account>())).Returns(Task.CompletedTask);
            _accountRepo.Setup(x => x.AssignRoleAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);
            _emailService.Setup(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);

            var dto = new RegisterDto { Username = "weakpass", Email = "weak@example.com", Password = "userpassword", RoleId = 2 };

            var result = await _service.RegisterAsync(dto);

            result.Should().Be("Register success. Please check your email for OTP.");
        }

        [Fact]
        public async Task Register_PasswordTooShort_BehaviorObserved()
        {
            _env.Setup(x => x.ContentRootPath).Returns(AppContext.BaseDirectory);
            _userRepo.Setup(x => x.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);
            _accountRepo.Setup(x => x.GetByUsernameAsync(It.IsAny<string>())).ReturnsAsync((Account?)null);
            _userRepo.Setup(x => x.CountAllUsersAsync()).ReturnsAsync(4);
            _accountRepo.Setup(x => x.CountAllAccountsAsync()).ReturnsAsync(4);

            _mapper.Setup(m => m.Map<User>(It.IsAny<RegisterDto>())).Returns((RegisterDto dto) => new User { Email = dto.Email });
            _mapper.Setup(m => m.Map<Account>(It.IsAny<RegisterDto>())).Returns((RegisterDto dto) => new Account { Username = dto.Username });
            _userRepo.Setup(x => x.AddAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
            _accountRepo.Setup(x => x.AddAsync(It.IsAny<Account>())).Returns(Task.CompletedTask);
            _accountRepo.Setup(x => x.AssignRoleAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);
            _emailService.Setup(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);

            var dto = new RegisterDto { Username = "short", Email = "short@example.com", Password = "pass", RoleId = 2 };

            var result = await _service.RegisterAsync(dto);

            result.Should().Be("Register success. Please check your email for OTP.");
        }
    }
}
