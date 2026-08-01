using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Moq;
using RetradeBE.Data;
using RetradeBE.Hubs;
using RetradeBE.Models;
using RetradeBE.Models.DTOs;
using RetradeBE.Repositories;
using RetradeBE.Services;
using Xunit;

namespace Test.ChatTests
{
    public class ChatSendMessageTests
    {
        private readonly Mock<IChatRepository> _chatRepository;
        private readonly Mock<IAccountRepository> _accountRepository;
        private readonly Mock<AppDbContext> _context;
        private readonly Mock<IHubContext<ChatHub>> _hubContext;
        private readonly ChatService _service;

        public ChatSendMessageTests()
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
        public async Task SendMessageAsync_ShouldSendBuyerMessageToSeller_WhenBuyerSendsMessage()
        {
            // Arrange (UTCID01)
            string accountId = "acc_buyer";
            string userId = "usr_buyer";
            string sellerId = "usr_seller";
            string roomId = "room_001";
            string messageText = "Hi seller, is this item still available?";

            var account = new Account { AccountId = accountId, UserId = userId };
            _accountRepository.Setup(r => r.GetByIdAsync(accountId)).ReturnsAsync(account);
            _accountRepository.Setup(r => r.GetRolesAsync(accountId)).ReturnsAsync(new List<string> { "User" });

            var room = new ChatRoom { RoomId = roomId, BuyerId = userId, SellerId = sellerId };
            _chatRepository.Setup(r => r.GetRoomByIdAsync(roomId)).ReturnsAsync(room);

            var savedChat = new Chat
            {
                ChatId = "chat_001",
                RoomId = roomId,
                SenderId = userId,
                Message = messageText,
                MessageType = "Text",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };
            _chatRepository.Setup(r => r.AddMessageAsync(It.IsAny<Chat>())).ReturnsAsync(savedChat);

            var request = new SendMessageRequestDto { Message = messageText };

            // Act
            var result = await _service.SendMessageAsync(accountId, roomId, request);

            // Assert
            result.Should().NotBeNull();
            result.Message.Should().Be(messageText);
            result.SenderId.Should().Be(userId);
            _chatRepository.Verify(r => r.AddMessageAsync(It.Is<Chat>(c => c.SenderId == userId && c.RoomId == roomId)), Times.Once);
        }

        [Fact]
        public async Task SendMessageAsync_ShouldSendSellerMessageToBuyer_WhenSellerSendsMessage()
        {
            // Arrange (UTCID02)
            string accountId = "acc_seller";
            string sellerId = "usr_seller";
            string buyerId = "usr_buyer";
            string roomId = "room_001";
            string messageText = "Yes, it is still available!";

            var account = new Account { AccountId = accountId, UserId = sellerId };
            _accountRepository.Setup(r => r.GetByIdAsync(accountId)).ReturnsAsync(account);
            _accountRepository.Setup(r => r.GetRolesAsync(accountId)).ReturnsAsync(new List<string> { "Seller" });

            var room = new ChatRoom { RoomId = roomId, BuyerId = buyerId, SellerId = sellerId };
            _chatRepository.Setup(r => r.GetRoomByIdAsync(roomId)).ReturnsAsync(room);

            var savedChat = new Chat
            {
                ChatId = "chat_002",
                RoomId = roomId,
                SenderId = sellerId,
                Message = messageText,
                MessageType = "Text",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };
            _chatRepository.Setup(r => r.AddMessageAsync(It.IsAny<Chat>())).ReturnsAsync(savedChat);

            var request = new SendMessageRequestDto { Message = messageText };

            // Act
            var result = await _service.SendMessageAsync(accountId, roomId, request);

            // Assert
            result.Should().NotBeNull();
            result.Message.Should().Be(messageText);
            result.SenderId.Should().Be(sellerId);
            _chatRepository.Verify(r => r.AddMessageAsync(It.Is<Chat>(c => c.SenderId == sellerId && c.RoomId == roomId)), Times.Once);
        }

        [Fact]
        public async Task SendMessageAsync_ShouldUseDefaultMessageTypeText_WhenMessageTypeIsNull()
        {
            // Arrange (UTCID03)
            string accountId = "acc_buyer";
            string userId = "usr_buyer";
            string sellerId = "usr_seller";
            string roomId = "room_001";

            var account = new Account { AccountId = accountId, UserId = userId };
            _accountRepository.Setup(r => r.GetByIdAsync(accountId)).ReturnsAsync(account);
            _accountRepository.Setup(r => r.GetRolesAsync(accountId)).ReturnsAsync(new List<string> { "User" });

            var room = new ChatRoom { RoomId = roomId, BuyerId = userId, SellerId = sellerId };
            _chatRepository.Setup(r => r.GetRoomByIdAsync(roomId)).ReturnsAsync(room);

            Chat? capturedChat = null;
            _chatRepository.Setup(r => r.AddMessageAsync(It.IsAny<Chat>()))
                .Callback<Chat>(c => capturedChat = c)
                .ReturnsAsync((Chat c) => c);

            var request = new SendMessageRequestDto { Message = "Test message", MessageType = null };

            // Act
            await _service.SendMessageAsync(accountId, roomId, request);

            // Assert
            capturedChat.Should().NotBeNull();
            capturedChat!.MessageType.Should().Be("Text");
        }

        #endregion

        #region Abnormal Tests (A)

        [Fact]
        public async Task SendMessageAsync_ShouldThrowArgumentException_WhenMessageIsEmpty()
        {
            // Arrange (UTCID04)
            string accountId = "acc_buyer";
            string userId = "usr_buyer";
            string roomId = "room_001";

            var account = new Account { AccountId = accountId, UserId = userId };
            _accountRepository.Setup(r => r.GetByIdAsync(accountId)).ReturnsAsync(account);
            _accountRepository.Setup(r => r.GetRolesAsync(accountId)).ReturnsAsync(new List<string> { "User" });

            var room = new ChatRoom { RoomId = roomId, BuyerId = userId, SellerId = "usr_seller" };
            _chatRepository.Setup(r => r.GetRoomByIdAsync(roomId)).ReturnsAsync(room);

            var request = new SendMessageRequestDto { Message = "   " };

            // Act & Assert
            var act = async () => await _service.SendMessageAsync(accountId, roomId, request);
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("Message is required.");
        }

        [Fact]
        public async Task SendMessageAsync_ShouldThrowArgumentException_WhenMessageExceeds2000Characters()
        {
            // Arrange (UTCID05)
            string accountId = "acc_buyer";
            string userId = "usr_buyer";
            string roomId = "room_001";

            var account = new Account { AccountId = accountId, UserId = userId };
            _accountRepository.Setup(r => r.GetByIdAsync(accountId)).ReturnsAsync(account);
            _accountRepository.Setup(r => r.GetRolesAsync(accountId)).ReturnsAsync(new List<string> { "User" });

            var room = new ChatRoom { RoomId = roomId, BuyerId = userId, SellerId = "usr_seller" };
            _chatRepository.Setup(r => r.GetRoomByIdAsync(roomId)).ReturnsAsync(room);

            var longMessage = new string('A', 2001);
            var request = new SendMessageRequestDto { Message = longMessage };

            // Act & Assert
            var act = async () => await _service.SendMessageAsync(accountId, roomId, request);
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("Message is too long.");
        }

        [Fact]
        public async Task SendMessageAsync_ShouldThrowUnauthorizedAccessException_WhenAccountDoesNotExist()
        {
            // Arrange (UTCID06)
            string nonExistingAccountId = "acc_invalid";
            string roomId = "room_001";

            _accountRepository.Setup(r => r.GetByIdAsync(nonExistingAccountId)).ReturnsAsync((Account?)null);

            var request = new SendMessageRequestDto { Message = "Hello" };

            // Act & Assert
            var act = async () => await _service.SendMessageAsync(nonExistingAccountId, roomId, request);
            await act.Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("Account is not linked to a user.");
        }

        [Fact]
        public async Task SendMessageAsync_ShouldThrowKeyNotFoundException_WhenRoomNotFound()
        {
            // Arrange (UTCID07)
            string accountId = "acc_buyer";
            string userId = "usr_buyer";

            var account = new Account { AccountId = accountId, UserId = userId };
            _accountRepository.Setup(r => r.GetByIdAsync(accountId)).ReturnsAsync(account);
            _accountRepository.Setup(r => r.GetRolesAsync(accountId)).ReturnsAsync(new List<string> { "User" });

            _chatRepository.Setup(r => r.GetRoomByIdAsync("non_existing_room")).ReturnsAsync((ChatRoom?)null);

            var request = new SendMessageRequestDto { Message = "Hello" };

            // Act & Assert
            var act = async () => await _service.SendMessageAsync(accountId, "non_existing_room", request);
            await act.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage("Chat room not found.");
        }

        #endregion
    }
}
