using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Moq;
using RetradeBE.Data;
using RetradeBE.Hubs;
using RetradeBE.Models;
using Xunit;

namespace Test.NotificationTests
{
    public class NotificationJoinUserNotificationsTests
    {
        private readonly Mock<AppDbContext> _context;
        private readonly Mock<HubCallerContext> _hubCallerContext;
        private readonly Mock<IGroupManager> _groupManager;
        private readonly ChatHub _hub;

        public NotificationJoinUserNotificationsTests()
        {
            _context = new Mock<AppDbContext>();
            _hubCallerContext = new Mock<HubCallerContext>();
            _groupManager = new Mock<IGroupManager>();

            _hub = new ChatHub(_context.Object)
            {
                Context = _hubCallerContext.Object,
                Groups = _groupManager.Object
            };
        }

        #region Normal Tests (N)

        [Fact]
        public async Task JoinUserNotifications_ShouldAddToGroup_WhenUserIsAuthenticatedAndAccountExists()
        {
            // Arrange
            string accountId = "acc_100";
            string userId = "usr_100";
            string connectionId = "conn_xyz";

            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, accountId) };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _hubCallerContext.Setup(c => c.User).Returns(claimsPrincipal);
            _hubCallerContext.Setup(c => c.ConnectionId).Returns(connectionId);

            var accounts = new List<Account>
            {
                new Account { AccountId = accountId, UserId = userId }
            }.AsMockDbSet();
            _context.Setup(c => c.Account).Returns(accounts.Object);

            _groupManager.Setup(g => g.AddToGroupAsync(connectionId, ChatHub.GetUserGroupName(userId), default))
                .Returns(Task.CompletedTask);

            // Act
            await _hub.JoinUserNotifications();

            // Assert
            _groupManager.Verify(g => g.AddToGroupAsync(connectionId, ChatHub.GetUserGroupName(userId), default), Times.Once);
        }

        [Fact]
        public async Task JoinUserNotifications_ShouldUseCorrectUserGroupNameFormat()
        {
            // Arrange
            string userId = "usr_format_test";
            string expectedGroupName = $"chat-user-{userId}";

            // Act
            var groupName = ChatHub.GetUserGroupName(userId);

            // Assert
            groupName.Should().Be(expectedGroupName);
        }

        #endregion

        #region Abnormal Tests (A)

        [Fact]
        public async Task JoinUserNotifications_ShouldNotAddToGroup_WhenUserClaimsMissingOrUnauthenticated()
        {
            // Arrange
            _hubCallerContext.Setup(c => c.User).Returns((ClaimsPrincipal?)null);
            _hubCallerContext.Setup(c => c.ConnectionId).Returns("conn_xyz");

            // Act
            await _hub.JoinUserNotifications();

            // Assert
            _groupManager.Verify(g => g.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), default), Times.Never);
        }

        [Fact]
        public async Task JoinUserNotifications_ShouldNotAddToGroup_WhenNameIdentifierClaimIsEmpty()
        {
            // Arrange
            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, "") };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _hubCallerContext.Setup(c => c.User).Returns(claimsPrincipal);
            _hubCallerContext.Setup(c => c.ConnectionId).Returns("conn_xyz");

            // Act
            await _hub.JoinUserNotifications();

            // Assert
            _groupManager.Verify(g => g.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), default), Times.Never);
        }

        [Fact]
        public async Task JoinUserNotifications_ShouldNotAddToGroup_WhenNameIdentifierClaimIsWhitespace()
        {
            // Arrange
            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, "   ") };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _hubCallerContext.Setup(c => c.User).Returns(claimsPrincipal);
            _hubCallerContext.Setup(c => c.ConnectionId).Returns("conn_xyz");

            // Act
            await _hub.JoinUserNotifications();

            // Assert
            _groupManager.Verify(g => g.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), default), Times.Never);
        }

        #endregion

        #region Boundary Tests (B)

        [Fact]
        public async Task JoinUserNotifications_ShouldNotAddToGroup_WhenAccountNotFoundOrUserIdIsNull()
        {
            // Arrange
            string accountId = "non_existing_acc";
            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, accountId) };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _hubCallerContext.Setup(c => c.User).Returns(claimsPrincipal);
            _hubCallerContext.Setup(c => c.ConnectionId).Returns("conn_xyz");

            var emptyAccounts = new List<Account>().AsMockDbSet();
            _context.Setup(c => c.Account).Returns(emptyAccounts.Object);

            // Act
            await _hub.JoinUserNotifications();

            // Assert
            _groupManager.Verify(g => g.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), default), Times.Never);
        }

        [Fact]
        public async Task JoinUserNotifications_ShouldNotAddToGroup_WhenAccountExistsButUserIdPropertyIsEmpty()
        {
            // Arrange
            string accountId = "acc_empty_user";
            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, accountId) };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _hubCallerContext.Setup(c => c.User).Returns(claimsPrincipal);
            _hubCallerContext.Setup(c => c.ConnectionId).Returns("conn_xyz");

            var accounts = new List<Account>
            {
                new Account { AccountId = accountId, UserId = "" }
            }.AsMockDbSet();
            _context.Setup(c => c.Account).Returns(accounts.Object);

            // Act
            await _hub.JoinUserNotifications();

            // Assert
            _groupManager.Verify(g => g.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), default), Times.Never);
        }

        #endregion
    }
}
