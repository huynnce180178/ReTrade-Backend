using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Moq;
using FluentAssertions;
using RetradeBE.Services;
using RetradeBE.Repositories;
using RetradeBE.Models;

namespace Test.UserTests
{
    public class UserGetAllTests
    {
        private readonly Mock<IUserRepository> _userRepo;
        private readonly UserService _service;

        public UserGetAllTests()
        {
            _userRepo = new Mock<IUserRepository>();
            _service = new UserService(_userRepo.Object);
        }

        #region Normal Tests (N)

        [Fact]
        public async Task GetAllAsync_ShouldReturnEmptyList_WhenNoUsersExist()
        {
            // Arrange
            _userRepo.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<User>());

            // Act
            var result = await _service.GetAllAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnUsers_WhenUsersExist()
        {
            // Arrange
            var users = new List<User>
            {
                new User { UserId = "user_1", Email = "user1@example.com" },
                new User { UserId = "user_2", Email = "user2@example.com" }
            };
            _userRepo.Setup(x => x.GetAllAsync()).ReturnsAsync(users);

            // Act
            var result = await _service.GetAllAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result.First().UserId.Should().Be("user_1");
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnMultipleUsers_WhenMultipleUsersExist()
        {
            // Arrange
            var users = new List<User>
            {
                new User { UserId = "user_1" },
                new User { UserId = "user_2" },
                new User { UserId = "user_3" }
            };
            _userRepo.Setup(x => x.GetAllAsync()).ReturnsAsync(users);

            // Act
            var result = await _service.GetAllAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(3);
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnUsersWithProfileDetails_WhenCalled()
        {
            // Arrange
            var users = new List<User>
            {
                new User { UserId = "user_1", FirstName = "John", AvatarUrl = "http://avatar.url" }
            };
            _userRepo.Setup(x => x.GetAllAsync()).ReturnsAsync(users);

            // Act
            var result = await _service.GetAllAsync();

            // Assert
            result.Should().NotBeNull();
            result.First().FirstName.Should().Be("John");
            result.First().AvatarUrl.Should().Be("http://avatar.url");
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnUsersIncludingDeleted_WhenTheyExist()
        {
            // Arrange
            var users = new List<User>
            {
                new User { UserId = "user_1", IsDeleted = true }
            };
            _userRepo.Setup(x => x.GetAllAsync()).ReturnsAsync(users);

            // Act
            var result = await _service.GetAllAsync();

            // Assert
            result.Should().NotBeNull();
            result.First().IsDeleted.Should().BeTrue();
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnUsersSortedByRepositoryDefault_WhenCalled()
        {
            // Arrange
            var users = new List<User>
            {
                new User { UserId = "user_b" },
                new User { UserId = "user_a" }
            };
            _userRepo.Setup(x => x.GetAllAsync()).ReturnsAsync(users);

            // Act
            var result = await _service.GetAllAsync();

            // Assert
            result.Should().NotBeNull();
            result.ElementAt(0).UserId.Should().Be("user_b");
            result.ElementAt(1).UserId.Should().Be("user_a");
        }

        [Fact]
        public async Task GetAllAsync_ShouldNotThrow_WhenRepositoryReturnsNull()
        {
            // Arrange
            _userRepo.Setup(x => x.GetAllAsync()).ReturnsAsync((List<User>)null!);

            // Act
            var result = await _service.GetAllAsync();

            // Assert
            result.Should().BeNull();
        }

        #endregion

        #region Abnormal Tests (A)

        [Fact]
        public async Task GetAllAsync_ShouldThrowException_WhenRepositoryThrowsException()
        {
            // Arrange
            _userRepo.Setup(x => x.GetAllAsync()).ThrowsAsync(new Exception("Database connection error"));

            // Act & Assert
            await _service.Invoking(s => s.GetAllAsync())
                .Should().ThrowAsync<Exception>()
                .WithMessage("Database connection error");
        }

        [Fact]
        public async Task GetAllAsync_ShouldThrowException_WhenDatabaseConnectionFails()
        {
            // Arrange
            _userRepo.Setup(x => x.GetAllAsync()).ThrowsAsync(new InvalidOperationException("Invalid operation"));

            // Act & Assert
            await _service.Invoking(s => s.GetAllAsync())
                .Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Invalid operation");
        }

        #endregion

        #region Boundary Tests (B)

        [Fact]
        public async Task GetAllAsync_ShouldReturnLargeNumberOfUsers_WhenRepositoryContainsManyUsers()
        {
            // Arrange
            var users = Enumerable.Range(1, 100).Select(i => new User
            {
                UserId = $"user_{i}",
                FirstName = $"Name {i}"
            }).ToList();
            _userRepo.Setup(x => x.GetAllAsync()).ReturnsAsync(users);

            // Act
            var result = await _service.GetAllAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(100);
            result.ElementAt(99).UserId.Should().Be("user_100");
        }

        #endregion
    }
}
