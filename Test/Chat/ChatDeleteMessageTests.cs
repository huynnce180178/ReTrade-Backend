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

namespace Test.ChatTests
{
    public class ChatDeleteMessageTests
    {
        private readonly Mock<IChatRepository> _chatRepository;
        private readonly Mock<IAccountRepository> _accountRepository;
        private readonly Mock<AppDbContext> _context;
        private readonly Mock<IHubContext<ChatHub>> _hubContext;
        private readonly ChatService _service;

        public ChatDeleteMessageTests()
        {
            _chatRepository = new Mock<IChatRepository>();
            _accountRepository = new Mock<IAccountRepository>();
            _context = new Mock<AppDbContext>();
            _hubContext = new Mock<IHubContext<ChatHub>>();

            var mockClients = new Mock<IHubClients>();
            var mockClientProxy = new Mock<IClientProxy>();
            mockClients.Setup(c => c.Group(It.IsAny<string>())).Returns(mockClientProxy.Object);
            mockClients.Setup(c => c.User(It.IsAny<string>())).Returns(mockClientProxy.Object);
            _hubContext.Setup(h => h.Clients).Returns(mockClients.Object);

            _service = new ChatService(
                _chatRepository.Object,
                _accountRepository.Object,
                _context.Object,
                _hubContext.Object
            );
        }

        #region Normal Tests (N)

        [Fact]
        public async Task DeleteMessageAsync_ShouldMarkDeletedForSender_WhenUserIsSender()
        {
            // Arrange (UTCID01)
            string accountId = "acc_sender";
            string userId = "usr_sender";
            string roomId = "room_001";
            string messageId = "chat_001";

            var account = new Account { AccountId = accountId, UserId = userId };
            _accountRepository.Setup(r => r.GetByIdAsync(accountId)).ReturnsAsync(account);
            _accountRepository.Setup(r => r.GetRolesAsync(accountId)).ReturnsAsync(new List<string> { "User" });

            var room = new ChatRoom { RoomId = roomId, BuyerId = userId, SellerId = "usr_receiver" };
            _chatRepository.Setup(r => r.GetRoomByIdAsync(roomId)).ReturnsAsync(room);

            var message = new Chat
            {
                ChatId = messageId,
                RoomId = roomId,
                SenderId = userId,
                Message = "Hello",
                DeletedForSender = false,
                DeletedForReceiver = false
            };
            _chatRepository.Setup(r => r.GetMessageByIdAsync(messageId)).ReturnsAsync(message);

            // Act
            var result = await _service.DeleteMessageAsync(accountId, roomId, messageId);

            // Assert
            result.Should().BeTrue();
            message.DeletedForSender.Should().BeTrue();
            _chatRepository.Verify(r => r.UpdateMessageAsync(message), Times.Once);
        }

        [Fact]
        public async Task DeleteMessageAsync_ShouldMarkDeletedForReceiver_WhenUserIsReceiver()
        {
            // Arrange (UTCID02)
            string accountId = "acc_receiver";
            string userId = "usr_receiver";
            string roomId = "room_001";
            string messageId = "chat_001";

            var account = new Account { AccountId = accountId, UserId = userId };
            _accountRepository.Setup(r => r.GetByIdAsync(accountId)).ReturnsAsync(account);
            _accountRepository.Setup(r => r.GetRolesAsync(accountId)).ReturnsAsync(new List<string> { "User" });

            var room = new ChatRoom { RoomId = roomId, BuyerId = "usr_sender", SellerId = userId };
            _chatRepository.Setup(r => r.GetRoomByIdAsync(roomId)).ReturnsAsync(room);

            var message = new Chat
            {
                ChatId = messageId,
                RoomId = roomId,
                SenderId = "usr_sender",
                Message = "Hello",
                DeletedForSender = false,
                DeletedForReceiver = false
            };
            _chatRepository.Setup(r => r.GetMessageByIdAsync(messageId)).ReturnsAsync(message);

            // Act
            var result = await _service.DeleteMessageAsync(accountId, roomId, messageId);

            // Assert
            result.Should().BeTrue();
            message.DeletedForReceiver.Should().BeTrue();
            _chatRepository.Verify(r => r.UpdateMessageAsync(message), Times.Once);
        }

        #endregion

        #region Abnormal Tests (A)

        [Fact]
        public async Task DeleteMessageAsync_ShouldThrowKeyNotFoundException_WhenMessageNotFound()
        {
            // Arrange (UTCID03)
            string accountId = "acc_user";
            string userId = "usr_user";
            string roomId = "room_001";

            var account = new Account { AccountId = accountId, UserId = userId };
            _accountRepository.Setup(r => r.GetByIdAsync(accountId)).ReturnsAsync(account);
            _accountRepository.Setup(r => r.GetRolesAsync(accountId)).ReturnsAsync(new List<string> { "User" });

            var room = new ChatRoom { RoomId = roomId, BuyerId = userId, SellerId = "usr_seller" };
            _chatRepository.Setup(r => r.GetRoomByIdAsync(roomId)).ReturnsAsync(room);

            _chatRepository.Setup(r => r.GetMessageByIdAsync("invalid_msg")).ReturnsAsync((Chat?)null);

            // Act & Assert
            var act = async () => await _service.DeleteMessageAsync(accountId, roomId, "invalid_msg");
            await act.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage("Message not found.");
        }

        [Fact]
        public async Task DeleteMessageAsync_ShouldThrowUnauthorizedAccessException_WhenUserCannotAccessRoom()
        {
            // Arrange (UTCID04)
            string accountId = "acc_stranger";
            string userId = "usr_stranger";
            string roomId = "room_001";

            var account = new Account { AccountId = accountId, UserId = userId };
            _accountRepository.Setup(r => r.GetByIdAsync(accountId)).ReturnsAsync(account);
            _accountRepository.Setup(r => r.GetRolesAsync(accountId)).ReturnsAsync(new List<string> { "User" });

            var room = new ChatRoom { RoomId = roomId, BuyerId = "usr_buyer", SellerId = "usr_seller" };
            _chatRepository.Setup(r => r.GetRoomByIdAsync(roomId)).ReturnsAsync(room);

            // Act & Assert
            var act = async () => await _service.DeleteMessageAsync(accountId, roomId, "chat_001");
            await act.Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("You do not have permission to access this chat room.");
        }

        #endregion
    }
}
