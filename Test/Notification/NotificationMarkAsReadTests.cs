using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Moq;
using RetradeBE.Data;
using RetradeBE.Hubs;
using RetradeBE.Models;
using RetradeBE.Repositories;
using RetradeBE.Services;
using Xunit;

namespace Test.NotificationTests
{
    public class NotificationMarkAsReadTests
    {
        private readonly Mock<IChatRepository> _chatRepository;
        private readonly Mock<IAccountRepository> _accountRepository;
        private readonly Mock<AppDbContext> _context;
        private readonly Mock<IHubContext<ChatHub>> _hubContext;
        private readonly Mock<IHubClients> _hubClients;
        private readonly Mock<IClientProxy> _clientProxy;
        private readonly ChatService _service;

        public NotificationMarkAsReadTests()
        {
            _chatRepository = new Mock<IChatRepository>();
            _accountRepository = new Mock<IAccountRepository>();
            _context = new Mock<AppDbContext>();
            _hubContext = new Mock<IHubContext<ChatHub>>();
            _hubClients = new Mock<IHubClients>();
            _clientProxy = new Mock<IClientProxy>();

            _hubContext.Setup(h => h.Clients).Returns(_hubClients.Object);
            _hubClients.Setup(c => c.Group(It.IsAny<string>())).Returns(_clientProxy.Object);

            _accountRepository.Setup(r => r.GetRolesAsync(It.IsAny<string>())).ReturnsAsync(new List<string>());

            _service = new ChatService(
                _chatRepository.Object,
                _accountRepository.Object,
                _context.Object,
                _hubContext.Object
            );
        }

        #region Normal Tests (N)

        [Fact]
        public async Task MarkAsReadAsync_ShouldMarkNotificationsAsRead_WhenUserHasAccessAndUnreadNotificationsExist()
        {
            // Arrange
            string accountId = "acc_001";
            string userId = "usr_001";
            string notificationTargetId = "notif_group_001";

            var account = new Account { AccountId = accountId, UserId = userId };
            _accountRepository.Setup(r => r.GetByIdAsync(accountId)).ReturnsAsync(account);

            var targetRoom = new ChatRoom { RoomId = notificationTargetId, BuyerId = userId, SellerId = "usr_other" };
            _chatRepository.Setup(r => r.GetRoomByIdAsync(notificationTargetId)).ReturnsAsync(targetRoom);

            _chatRepository.Setup(r => r.MarkMessagesAsReadAsync(notificationTargetId, userId)).ReturnsAsync(3);

            // Act
            var result = await _service.MarkMessagesAsReadAsync(accountId, notificationTargetId);

            // Assert
            result.Should().Be(3);
            _chatRepository.Verify(r => r.MarkMessagesAsReadAsync(notificationTargetId, userId), Times.Once);
            _hubClients.Verify(c => c.Group(ChatHub.GetRoomGroupName(notificationTargetId)), Times.Once);
        }

        [Fact]
        public async Task MarkAsReadAsync_ShouldAllowAdminToMarkRead_WhenAdminAccessesNotificationGroup()
        {
            // Arrange
            string accountId = "acc_admin";
            string adminId = "usr_admin";
            string notificationTargetId = "notif_group_001";

            var account = new Account { AccountId = accountId, UserId = adminId };
            _accountRepository.Setup(r => r.GetByIdAsync(accountId)).ReturnsAsync(account);
            _accountRepository.Setup(r => r.GetRolesAsync(accountId)).ReturnsAsync(new List<string> { "Admin" });

            var targetRoom = new ChatRoom { RoomId = notificationTargetId, BuyerId = "usr_buyer", SellerId = "usr_seller" };
            _chatRepository.Setup(r => r.GetRoomByIdAsync(notificationTargetId)).ReturnsAsync(targetRoom);
            _chatRepository.Setup(r => r.MarkMessagesAsReadAsync(notificationTargetId, adminId)).ReturnsAsync(1);

            // Act
            var result = await _service.MarkMessagesAsReadAsync(accountId, notificationTargetId);

            // Assert
            result.Should().Be(1);
        }

        #endregion

        #region Abnormal Tests (A)

        [Fact]
        public async Task MarkAsReadAsync_ShouldThrowUnauthorizedAccessException_WhenAccountNotFound()
        {
            // Arrange
            string accountId = "non_existent_acc";
            _accountRepository.Setup(r => r.GetByIdAsync(accountId)).ReturnsAsync((Account?)null);

            // Act
            Func<Task> act = async () => await _service.MarkMessagesAsReadAsync(accountId, "notif_group_001");

            // Assert
            await act.Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("Account is not linked to a user.");
        }

        [Fact]
        public async Task MarkAsReadAsync_ShouldThrowKeyNotFoundException_WhenNotificationGroupNotFound()
        {
            // Arrange
            string accountId = "acc_001";
            string userId = "usr_001";
            var account = new Account { AccountId = accountId, UserId = userId };
            _accountRepository.Setup(r => r.GetByIdAsync(accountId)).ReturnsAsync(account);
            _chatRepository.Setup(r => r.GetRoomByIdAsync("non_existent_target")).ReturnsAsync((ChatRoom?)null);

            // Act
            Func<Task> act = async () => await _service.MarkMessagesAsReadAsync(accountId, "non_existent_target");

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage("Chat room not found.");
        }

        [Fact]
        public async Task MarkAsReadAsync_ShouldThrowUnauthorizedAccessException_WhenUserDoesNotHaveAccess()
        {
            // Arrange
            string accountId = "acc_stranger";
            string strangerId = "usr_stranger";
            string notificationTargetId = "notif_group_001";

            var account = new Account { AccountId = accountId, UserId = strangerId };
            _accountRepository.Setup(r => r.GetByIdAsync(accountId)).ReturnsAsync(account);

            var targetRoom = new ChatRoom { RoomId = notificationTargetId, BuyerId = "usr_owner", SellerId = "usr_other" };
            _chatRepository.Setup(r => r.GetRoomByIdAsync(notificationTargetId)).ReturnsAsync(targetRoom);

            // Act
            Func<Task> act = async () => await _service.MarkMessagesAsReadAsync(accountId, notificationTargetId);

            // Assert
            await act.Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("You do not have permission to access this chat room.");
        }

        #endregion

        #region Boundary Tests (B)

        [Fact]
        public async Task MarkAsReadAsync_ShouldReturnZero_WhenNoUnreadNotificationsExist()
        {
            // Arrange
            string accountId = "acc_001";
            string userId = "usr_001";
            string notificationTargetId = "notif_group_001";

            var account = new Account { AccountId = accountId, UserId = userId };
            _accountRepository.Setup(r => r.GetByIdAsync(accountId)).ReturnsAsync(account);

            var targetRoom = new ChatRoom { RoomId = notificationTargetId, BuyerId = userId, SellerId = "usr_other" };
            _chatRepository.Setup(r => r.GetRoomByIdAsync(notificationTargetId)).ReturnsAsync(targetRoom);

            _chatRepository.Setup(r => r.MarkMessagesAsReadAsync(notificationTargetId, userId)).ReturnsAsync(0);

            // Act
            var result = await _service.MarkMessagesAsReadAsync(accountId, notificationTargetId);

            // Assert
            result.Should().Be(0);
            _hubClients.Verify(c => c.Group(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task MarkAsReadAsync_ShouldHandleSingleUnreadNotificationBoundary()
        {
            // Arrange
            string accountId = "acc_001";
            string userId = "usr_001";
            string notificationTargetId = "notif_group_001";

            var account = new Account { AccountId = accountId, UserId = userId };
            _accountRepository.Setup(r => r.GetByIdAsync(accountId)).ReturnsAsync(account);

            var targetRoom = new ChatRoom { RoomId = notificationTargetId, BuyerId = userId, SellerId = "usr_other" };
            _chatRepository.Setup(r => r.GetRoomByIdAsync(notificationTargetId)).ReturnsAsync(targetRoom);

            _chatRepository.Setup(r => r.MarkMessagesAsReadAsync(notificationTargetId, userId)).ReturnsAsync(1);

            // Act
            var result = await _service.MarkMessagesAsReadAsync(accountId, notificationTargetId);

            // Assert
            result.Should().Be(1);
            _hubClients.Verify(c => c.Group(ChatHub.GetRoomGroupName(notificationTargetId)), Times.Once);
        }

        [Fact]
        public async Task MarkAsReadAsync_ShouldHandleLargeUnreadNotificationCountBoundary()
        {
            // Arrange
            string accountId = "acc_001";
            string userId = "usr_001";
            string notificationTargetId = "notif_group_001";

            var account = new Account { AccountId = accountId, UserId = userId };
            _accountRepository.Setup(r => r.GetByIdAsync(accountId)).ReturnsAsync(account);

            var targetRoom = new ChatRoom { RoomId = notificationTargetId, BuyerId = userId, SellerId = "usr_other" };
            _chatRepository.Setup(r => r.GetRoomByIdAsync(notificationTargetId)).ReturnsAsync(targetRoom);

            _chatRepository.Setup(r => r.MarkMessagesAsReadAsync(notificationTargetId, userId)).ReturnsAsync(999);

            // Act
            var result = await _service.MarkMessagesAsReadAsync(accountId, notificationTargetId);

            // Assert
            result.Should().Be(999);
        }

        #endregion
    }
}
