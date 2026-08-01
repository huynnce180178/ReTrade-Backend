using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RetradeBE.Models;
using RetradeBE.Models.DTOs;
using RetradeBE.Models.DTOs.AssistantChat;
using RetradeBE.Models.DTOs.Gemini;
using RetradeBE.Repositories;
using RetradeBE.Services;
using RetradeBE.Services.AssistantChat;
using RetradeBE.Services.GeminiAssistant;
using Xunit;

namespace Test.AssistantChatTests
{
    public class AssistantChatSendMessageTests
    {
        private readonly Mock<IAssistantChatSessionRepository> _chatSessionRepository;
        private readonly Mock<IAssistantChatMessageRepository> _chatMessageRepository;
        private readonly Mock<IAccountRepository> _accountRepository;
        private readonly Mock<IProductRepository> _productRepository;
        private readonly Mock<IPurchaseService> _purchaseService;
        private readonly Mock<IGeminiAssistantApiService> _geminiApiService;
        private readonly Mock<ILogger<AssistantChatService>> _logger;
        private readonly AssistantChatService _service;

        public AssistantChatSendMessageTests()
        {
            _chatSessionRepository = new Mock<IAssistantChatSessionRepository>();
            _chatMessageRepository = new Mock<IAssistantChatMessageRepository>();
            _accountRepository = new Mock<IAccountRepository>();
            _productRepository = new Mock<IProductRepository>();
            _purchaseService = new Mock<IPurchaseService>();
            _geminiApiService = new Mock<IGeminiAssistantApiService>();
            _logger = new Mock<ILogger<AssistantChatService>>();

            _service = new AssistantChatService(
                _chatSessionRepository.Object,
                _chatMessageRepository.Object,
                _accountRepository.Object,
                _productRepository.Object,
                _purchaseService.Object,
                _geminiApiService.Object,
                _logger.Object
            );
        }

        #region Normal Tests (N)

        [Fact]
        public async Task SendMessageAsync_ShouldReturnResponseDto_WhenAuthenticatedUserSendsValidMessage()
        {
            // Arrange
            string accountId = "acc_001";
            string userId = "usr_001";
            var request = new AssistantChatRequestDto { Message = "Hello assistant" };

            var account = new Account { AccountId = accountId, UserId = userId };
            _accountRepository.Setup(r => r.GetByIdAsync(accountId)).ReturnsAsync(account);

            _chatSessionRepository.Setup(r => r.AddAsync(It.IsAny<ChatSession>())).Returns(Task.CompletedTask);
            _chatSessionRepository.Setup(r => r.UpdateAsync(It.IsAny<ChatSession>())).Returns(Task.CompletedTask);
            _chatMessageRepository.Setup(r => r.AddAsync(It.IsAny<ChatMessage>())).Returns(Task.CompletedTask);
            _chatMessageRepository.Setup(r => r.GetBySessionIdAsync(It.IsAny<string>())).ReturnsAsync(new List<ChatMessage>());

            var emptyOrders = new List<PurchaseListDto>().AsQueryable();
            _purchaseService.Setup(s => s.QueryByBuyerId(userId, It.IsAny<string?>())).Returns(emptyOrders);

            var geminiResponse = new GeminiGenerateContentResponseDto
            {
                Candidates = new List<GeminiCandidateDto>
                {
                    new GeminiCandidateDto
                    {
                        Content = new GeminiContentDto
                        {
                            Parts = new List<GeminiPartDto> { new GeminiPartDto { Text = "Hello! How can I help you today?" } }
                        }
                    }
                }
            };
            _geminiApiService.Setup(g => g.GenerateContentAsync(It.IsAny<List<GeminiContentDto>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(geminiResponse);

            // Act
            var result = await _service.SendMessageAsync(accountId, request);

            // Assert
            result.Should().NotBeNull();
            result.Role.Should().Be("model");
            result.Content.Should().Be("Hello! How can I help you today?");
            result.Products.Should().BeEmpty();
            _chatSessionRepository.Verify(r => r.AddAsync(It.IsAny<ChatSession>()), Times.Once);
            _chatMessageRepository.Verify(r => r.AddAsync(It.IsAny<ChatMessage>()), Times.Exactly(2));
        }

        [Fact]
        public async Task SendMessageAsync_ShouldReuseExistingSession_WhenValidSessionIdProvided()
        {
            // Arrange
            string accountId = "acc_001";
            string userId = "usr_001";
            string sessionId = "ases_123";
            var request = new AssistantChatRequestDto { SessionId = sessionId, Message = "Follow up question" };

            var account = new Account { AccountId = accountId, UserId = userId };
            _accountRepository.Setup(r => r.GetByIdAsync(accountId)).ReturnsAsync(account);

            var existingSession = new ChatSession
            {
                SessionId = sessionId,
                UserId = userId,
                Title = "Initial session"
            };
            _chatSessionRepository.Setup(r => r.GetOwnedSessionAsync(userId, sessionId)).ReturnsAsync(existingSession);
            _chatSessionRepository.Setup(r => r.UpdateAsync(It.IsAny<ChatSession>())).Returns(Task.CompletedTask);
            _chatMessageRepository.Setup(r => r.AddAsync(It.IsAny<ChatMessage>())).Returns(Task.CompletedTask);
            _chatMessageRepository.Setup(r => r.GetBySessionIdAsync(sessionId)).ReturnsAsync(new List<ChatMessage>());

            var emptyOrders = new List<PurchaseListDto>().AsQueryable();
            _purchaseService.Setup(s => s.QueryByBuyerId(userId, It.IsAny<string?>())).Returns(emptyOrders);

            var geminiResponse = new GeminiGenerateContentResponseDto
            {
                Candidates = new List<GeminiCandidateDto>
                {
                    new GeminiCandidateDto
                    {
                        Content = new GeminiContentDto
                        {
                            Parts = new List<GeminiPartDto> { new GeminiPartDto { Text = "Answer to follow up." } }
                        }
                    }
                }
            };
            _geminiApiService.Setup(g => g.GenerateContentAsync(It.IsAny<List<GeminiContentDto>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(geminiResponse);

            // Act
            var result = await _service.SendMessageAsync(accountId, request);

            // Assert
            result.SessionId.Should().Be(sessionId);
            _chatSessionRepository.Verify(r => r.AddAsync(It.IsAny<ChatSession>()), Times.Never);
        }

        [Fact]
        public async Task SendMessageAsync_ShouldIncludeUserOrderContext_WhenUserHasRecentOrders()
        {
            // Arrange
            string accountId = "acc_001";
            string userId = "usr_001";
            var request = new AssistantChatRequestDto { Message = "Where is my order?" };

            var account = new Account { AccountId = accountId, UserId = userId };
            _accountRepository.Setup(r => r.GetByIdAsync(accountId)).ReturnsAsync(account);

            _chatSessionRepository.Setup(r => r.AddAsync(It.IsAny<ChatSession>())).Returns(Task.CompletedTask);
            _chatSessionRepository.Setup(r => r.UpdateAsync(It.IsAny<ChatSession>())).Returns(Task.CompletedTask);
            _chatMessageRepository.Setup(r => r.AddAsync(It.IsAny<ChatMessage>())).Returns(Task.CompletedTask);
            _chatMessageRepository.Setup(r => r.GetBySessionIdAsync(It.IsAny<string>())).ReturnsAsync(new List<ChatMessage>());

            var orders = new List<PurchaseListDto>
            {
                new PurchaseListDto
                {
                    OrderId = "ord_001",
                    OrderCode = "ORD123",
                    ProductName = "iPhone 13",
                    FinalAmount = 15000000,
                    Status = "Shipping",
                    CreatedAt = DateTime.UtcNow.AddDays(-1)
                }
            }.AsQueryable();

            _purchaseService.Setup(s => s.QueryByBuyerId(userId, It.IsAny<string?>())).Returns(orders);

            var geminiResponse = new GeminiGenerateContentResponseDto
            {
                Candidates = new List<GeminiCandidateDto>
                {
                    new GeminiCandidateDto
                    {
                        Content = new GeminiContentDto
                        {
                            Parts = new List<GeminiPartDto> { new GeminiPartDto { Text = "Your order #ORD123 is currently shipping." } }
                        }
                    }
                }
            };
            _geminiApiService.Setup(g => g.GenerateContentAsync(It.IsAny<List<GeminiContentDto>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(geminiResponse);

            // Act
            var result = await _service.SendMessageAsync(accountId, request);

            // Assert
            result.Should().NotBeNull();
            result.Content.Should().Be("Your order #ORD123 is currently shipping.");
        }

        #endregion

        #region Abnormal Tests (A)

        [Fact]
        public async Task SendMessageAsync_ShouldThrowUnauthorizedAccessException_WhenAccountIdProvidedButAccountNotFound()
        {
            // Arrange
            string accountId = "invalid_acc";
            var request = new AssistantChatRequestDto { Message = "Hello" };

            _accountRepository.Setup(r => r.GetByIdAsync(accountId)).ReturnsAsync((Account?)null);

            // Act
            Func<Task> act = async () => await _service.SendMessageAsync(accountId, request);

            // Assert
            await act.Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("Account is not linked to a user.");
        }

        [Fact]
        public async Task SendMessageAsync_ShouldThrowArgumentException_WhenMessageIsEmptyOrWhitespace()
        {
            // Arrange
            var request = new AssistantChatRequestDto { Message = "   " };

            // Act
            Func<Task> act = async () => await _service.SendMessageAsync(null, request);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("Message is required.");
        }

        [Fact]
        public async Task SendMessageAsync_ShouldReturnGeneralErrorMessage_WhenGeminiThrowsGenericException()
        {
            // Arrange
            var request = new AssistantChatRequestDto { Message = "Test generic error" };

            _chatSessionRepository.Setup(r => r.AddAsync(It.IsAny<ChatSession>())).Returns(Task.CompletedTask);
            _chatSessionRepository.Setup(r => r.UpdateAsync(It.IsAny<ChatSession>())).Returns(Task.CompletedTask);
            _chatMessageRepository.Setup(r => r.AddAsync(It.IsAny<ChatMessage>())).Returns(Task.CompletedTask);
            _chatMessageRepository.Setup(r => r.GetBySessionIdAsync(It.IsAny<string>())).ReturnsAsync(new List<ChatMessage>());

            _geminiApiService.Setup(g => g.GenerateContentAsync(It.IsAny<List<GeminiContentDto>>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Network timeout"));

            // Act
            var result = await _service.SendMessageAsync(null, request);

            // Assert
            result.Should().NotBeNull();
            result.Content.Should().Be("Xin lỗi, hiện trợ lý AI đang gặp lỗi khi xử lý yêu cầu. Bạn thử lại sau ít phút nhé.");
        }

        #endregion

        #region Boundary Tests (B)

        [Fact]
        public async Task SendMessageAsync_ShouldSucceed_WhenMessageLengthIsExactly2000Characters()
        {
            // Arrange
            string maxMessage = new string('x', 2000);
            var request = new AssistantChatRequestDto { Message = maxMessage };

            _chatSessionRepository.Setup(r => r.AddAsync(It.IsAny<ChatSession>())).Returns(Task.CompletedTask);
            _chatSessionRepository.Setup(r => r.UpdateAsync(It.IsAny<ChatSession>())).Returns(Task.CompletedTask);
            _chatMessageRepository.Setup(r => r.AddAsync(It.IsAny<ChatMessage>())).Returns(Task.CompletedTask);
            _chatMessageRepository.Setup(r => r.GetBySessionIdAsync(It.IsAny<string>())).ReturnsAsync(new List<ChatMessage>());

            var geminiResponse = new GeminiGenerateContentResponseDto
            {
                Candidates = new List<GeminiCandidateDto>
                {
                    new GeminiCandidateDto
                    {
                        Content = new GeminiContentDto
                        {
                            Parts = new List<GeminiPartDto> { new GeminiPartDto { Text = "Processed max length message." } }
                        }
                    }
                }
            };
            _geminiApiService.Setup(g => g.GenerateContentAsync(It.IsAny<List<GeminiContentDto>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(geminiResponse);

            // Act
            var result = await _service.SendMessageAsync(null, request);

            // Assert
            result.Should().NotBeNull();
            result.Content.Should().Be("Processed max length message.");
        }

        #endregion
    }
}
