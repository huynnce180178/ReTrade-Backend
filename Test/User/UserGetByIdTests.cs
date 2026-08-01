using System;
using System.Threading.Tasks;
using Xunit;
using Moq;
using FluentAssertions;
using RetradeBE.Services;
using RetradeBE.Repositories;
using RetradeBE.Models;

namespace Test.UserTests
{
    public class UserGetByIdTests
    {
        private readonly Mock<IUserRepository> _userRepo;
        private readonly UserService _service;

        public UserGetByIdTests()
        {
            _userRepo = new Mock<IUserRepository>();
            _service = new UserService(_userRepo.Object);
        }

        #region Normal Tests (N)

        [Fact]
        public async Task GetByIdAsync_ShouldReturnUser_WhenUserExists()
        {
            // Arrange
            var userId = "user_1";
            var user = new User { UserId = userId, Email = "user1@example.com" };
            _userRepo.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);

            // Act
            var result = await _service.GetByIdAsync(userId);

            // Assert
            result.Should().NotBeNull();
            result.UserId.Should().Be(userId);
            result.Email.Should().Be("user1@example.com");
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnUserWithProfileDetails_WhenCalledWithValidId()
        {
            // Arrange
            var userId = "user_123";
            var user = new User
            {
                UserId = userId,
                FirstName = "John",
                LastName = "Doe",
                AvatarUrl = "http://avatar.url",
            };
            _userRepo.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);

            // Act
            var result = await _service.GetByIdAsync(userId);

            // Assert
            result.Should().NotBeNull();
            result.FirstName.Should().Be("John");
            result.LastName.Should().Be("Doe");
            result.AvatarUrl.Should().Be("http://avatar.url");
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnDeletedUser_WhenUserIsDeletedAndExists()
        {
            // Arrange
            var userId = "user_deleted";
            var user = new User { UserId = userId, IsDeleted = true };
            _userRepo.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);

            // Act
            var result = await _service.GetByIdAsync(userId);

            // Assert
            result.Should().NotBeNull();
            result.IsDeleted.Should().BeTrue();
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnNull_WhenUserDoesNotExist()
        {
            // Arrange
            var userId = "non_existent";
            _userRepo.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync((User)null!);

            // Act
            var result = await _service.GetByIdAsync(userId);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByIdAsync_ShouldAcceptStringId_WhenCalled()
        {
            // Arrange
            var stringId = "some_string_id";
            var user = new User { UserId = stringId };
            _userRepo.Setup(x => x.GetByIdAsync(stringId)).ReturnsAsync(user);

            // Act
            var result = await _service.GetByIdAsync(stringId);

            // Assert
            result.Should().NotBeNull();
            result.UserId.Should().Be(stringId);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldAcceptIntId_WhenCalled()
        {
            // Arrange
            int intId = 12345;
            var user = new User { UserId = "12345" };
            _userRepo.Setup(x => x.GetByIdAsync(intId)).ReturnsAsync(user);

            // Act
            var result = await _service.GetByIdAsync(intId);

            // Assert
            result.Should().NotBeNull();
            result.UserId.Should().Be("12345");
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnCorrectUser_WhenMultipleUsersExist()
        {
            // Arrange
            var userId1 = "user_1";
            var userId2 = "user_2";
            var user1 = new User { UserId = userId1 };
            _userRepo.Setup(x => x.GetByIdAsync(userId1)).ReturnsAsync(user1);
            _userRepo.Setup(x => x.GetByIdAsync(userId2)).ReturnsAsync(new User { UserId = userId2 });

            // Act
            var result = await _service.GetByIdAsync(userId1);

            // Assert
            result.Should().NotBeNull();
            result.UserId.Should().Be(userId1);
        }

        #endregion

        #region Abnormal Tests (A)

        [Fact]
        public async Task GetByIdAsync_ShouldThrowException_WhenRepositoryThrowsException()
        {
            // Arrange
            var userId = "user_1";
            _userRepo.Setup(x => x.GetByIdAsync(userId)).ThrowsAsync(new Exception("Database connection error"));

            // Act & Assert
            await _service.Invoking(s => s.GetByIdAsync(userId))
                .Should().ThrowAsync<Exception>()
                .WithMessage("Database connection error");
        }

        [Fact]
        public async Task GetByIdAsync_ShouldThrowException_WhenIdIsNull()
        {
            // Arrange
            object nullId = null!;
            _userRepo.Setup(x => x.GetByIdAsync(nullId)).ThrowsAsync(new ArgumentNullException("id"));

            // Act & Assert
            await _service.Invoking(s => s.GetByIdAsync(nullId))
                .Should().ThrowAsync<ArgumentNullException>();
        }

        #endregion

        #region Boundary Tests (B)

        [Fact]
        public async Task GetByIdAsync_ShouldReturnUser_WhenIdIsEmptyString()
        {
            // Arrange
            var emptyId = "";
            var user = new User { UserId = emptyId };
            _userRepo.Setup(x => x.GetByIdAsync(emptyId)).ReturnsAsync(user);

            // Act
            var result = await _service.GetByIdAsync(emptyId);

            // Assert
            result.Should().NotBeNull();
            result.UserId.Should().Be(emptyId);
        }

        #endregion
    }
}
