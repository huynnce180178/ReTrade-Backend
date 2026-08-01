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
    public class NotificationDeleteNotificationTests
    {
        private readonly Mock<IChatRepository> _chatRepository;
        private readonly Mock<IAccountRepository> _accountRepository;
        private readonly Mock<AppDbContext> _context;
        private readonly Mock<IHubContext<ChatHub>> _hubContext;
        private readonly Mock<IHubClients> _hubClients;
        private readonly Mock<IClientProxy> _clientProxy;
        private readonly ChatService _service;

        public NotificationDeleteNotificationTests()
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
        public async Task DeleteNotificationAsync_ShouldMarkDeletedForSender_WhenSenderDeletesNotification()
        {
            // Arrange
            string accountId = "acc_sender";
            string userId = "usr_sender";
            string notificationTargetId = "notif_target_01";
            string notificationId = "notif_001";

            var account = new Account { AccountId = accountId, UserId = userId };
            _accountRepository.Setup(r => r.GetByIdAsync(accountId)).ReturnsAsync(account);

            var notificationGroup = new ChatRoom { RoomId = notificationTargetId, BuyerId = userId, SellerId = "usr_receiver" };
            _chatRepository.Setup(r => r.GetRoomByIdAsync(notificationTargetId)).ReturnsAsync(notificationGroup);

            var notification = new Chat
            {
                ChatId = notificationId,
                RoomId = notificationTargetId,
                SenderId = userId,
                Message = "Notification Alert Item",
                DeletedForSender = false,
                DeletedForReceiver = false
            };
            _chatRepository.Setup(r => r.GetMessageByIdAsync(notificationId)).ReturnsAsync(notification);
            _chatRepository.Setup(r => r.UpdateMessageAsync(It.IsAny<Chat>())).ReturnsAsync(notification);

            // Act
            var result = await _service.DeleteMessageAsync(accountId, notificationTargetId, notificationId);

            // Assert
            result.Should().BeTrue();
            notification.DeletedForSender.Should().BeTrue();
            _chatRepository.Verify(r => r.UpdateMessageAsync(It.Is<Chat>(m => m.DeletedForSender == true)), Times.Once);
        }

        [Fact]
        public async Task DeleteNotificationAsync_ShouldMarkDeletedForReceiver_WhenReceiverDeletesNotification()
        {
            // Arrange
            string accountId = "acc_receiver";
            string receiverId = "usr_receiver";
            string notificationTargetId = "notif_target_01";
            string notificationId = "notif_001";

            var account = new Account { AccountId = accountId, UserId = receiverId };
            _accountRepository.Setup(r => r.GetByIdAsync(accountId)).ReturnsAsync(account);

            var notificationGroup = new ChatRoom { RoomId = notificationTargetId, BuyerId = "usr_sender", SellerId = receiverId };
            _chatRepository.Setup(r => r.GetRoomByIdAsync(notificationTargetId)).ReturnsAsync(notificationGroup);

            var notification = new Chat
            {
                ChatId = notificationId,
                RoomId = notificationTargetId,
                SenderId = "usr_sender",
                Message = "Notification Alert Item",
                DeletedForSender = false,
                DeletedForReceiver = false
            };
            _chatRepository.Setup(r => r.GetMessageByIdAsync(notificationId)).ReturnsAsync(notification);
            _chatRepository.Setup(r => r.UpdateMessageAsync(It.IsAny<Chat>())).ReturnsAsync(notification);

            // Act
            var result = await _service.DeleteMessageAsync(accountId, notificationTargetId, notificationId);

            // Assert
            result.Should().BeTrue();
            notification.DeletedForReceiver.Should().BeTrue();
        }

        #endregion

        #region Abnormal Tests (A)

        [Fact]
        public async Task DeleteNotificationAsync_ShouldThrowUnauthorizedAccessException_WhenUserDoesNotBelongToNotificationGroup()
        {
            // Arrange
            string accountId = "acc_stranger";
            string strangerId = "usr_stranger";
            string notificationTargetId = "notif_target_01";
            string notificationId = "notif_001";

            var account = new Account { AccountId = accountId, UserId = strangerId };
            _accountRepository.Setup(r => r.GetByIdAsync(accountId)).ReturnsAsync(account);

            var notificationGroup = new ChatRoom { RoomId = notificationTargetId, BuyerId = "usr_owner", SellerId = "usr_other" };
            _chatRepository.Setup(r => r.GetRoomByIdAsync(notificationTargetId)).ReturnsAsync(notificationGroup);

            // Act
            Func<Task> act = async () => await _service.DeleteMessageAsync(accountId, notificationTargetId, notificationId);

            // Assert
            await act.Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("You do not have permission to access this chat room.");
        }

        [Fact]
        public async Task DeleteNotificationAsync_ShouldThrowUnauthorizedAccessException_WhenAccountNotFound()
        {
            // Arrange
            string accountId = "non_existent_acc";
            _accountRepository.Setup(r => r.GetByIdAsync(accountId)).ReturnsAsync((Account?)null);

            // Act
            Func<Task> act = async () => await _service.DeleteMessageAsync(accountId, "notif_target_01", "notif_01");

            // Assert
            await act.Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("Account is not linked to a user.");
        }

        [Fact]
        public async Task DeleteNotificationAsync_ShouldThrowKeyNotFoundException_WhenNotificationDoesNotExist()
        {
            // Arrange
            string accountId = "acc_sender";
            string userId = "usr_sender";
            string notificationTargetId = "notif_target_01";
            string nonExistentId = "non_existent_notif";

            var account = new Account { AccountId = accountId, UserId = userId };
            _accountRepository.Setup(r => r.GetByIdAsync(accountId)).ReturnsAsync(account);

            var notificationGroup = new ChatRoom { RoomId = notificationTargetId, BuyerId = userId, SellerId = "usr_receiver" };
            _chatRepository.Setup(r => r.GetRoomByIdAsync(notificationTargetId)).ReturnsAsync(notificationGroup);

            _chatRepository.Setup(r => r.GetMessageByIdAsync(nonExistentId)).ReturnsAsync((Chat?)null);

            // Act
            Func<Task> act = async () => await _service.DeleteMessageAsync(accountId, notificationTargetId, nonExistentId);

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage("Message not found.");
        }

        #endregion

        #region Boundary Tests (B)

        [Fact]
        public async Task DeleteNotificationAsync_ShouldMarkIsDeletedTrue_WhenBothSenderAndReceiverDeleteNotification()
        {
            // Arrange
            string accountId = "acc_receiver";
            string receiverId = "usr_receiver";
            string notificationTargetId = "notif_target_01";
            string notificationId = "notif_002";

            var account = new Account { AccountId = accountId, UserId = receiverId };
            _accountRepository.Setup(r => r.GetByIdAsync(accountId)).ReturnsAsync(account);

            var notificationGroup = new ChatRoom { RoomId = notificationTargetId, BuyerId = "usr_sender", SellerId = receiverId };
            _chatRepository.Setup(r => r.GetRoomByIdAsync(notificationTargetId)).ReturnsAsync(notificationGroup);

            var notification = new Chat
            {
                ChatId = notificationId,
                RoomId = notificationTargetId,
                SenderId = "usr_sender",
                Message = "Notification Alert Item",
                DeletedForSender = true, // Sender already deleted it
                DeletedForReceiver = false
            };
            _chatRepository.Setup(r => r.GetMessageByIdAsync(notificationId)).ReturnsAsync(notification);
            _chatRepository.Setup(r => r.UpdateMessageAsync(It.IsAny<Chat>())).ReturnsAsync(notification);

            // Act
            var result = await _service.DeleteMessageAsync(accountId, notificationTargetId, notificationId);

            // Assert
            result.Should().BeTrue();
            notification.DeletedForReceiver.Should().BeTrue();
            notification.IsDeleted.Should().BeTrue();
            _chatRepository.Verify(r => r.UpdateMessageAsync(It.Is<Chat>(m => m.IsDeleted == true)), Times.Once);
        }

        #endregion
    }
}
