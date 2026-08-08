using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using RetradeBE.Data;
using RetradeBE.Hubs;
using RetradeBE.Models;
using RetradeBE.Models.DTOs;
using RetradeBE.Repositories;

namespace RetradeBE.Services
{
    public class ChatService : IChatService
    {
        private static readonly TimeSpan RecallWindow = TimeSpan.FromMinutes(3);
        private readonly IChatRepository _chatRepository;
        private readonly IAccountRepository _accountRepository;
        private readonly AppDbContext _context;
        private readonly IHubContext<ChatHub> _hubContext;

        public ChatService(
            IChatRepository chatRepository,
            IAccountRepository accountRepository,
            AppDbContext context,
            IHubContext<ChatHub> hubContext)
        {
            _chatRepository = chatRepository;
            _accountRepository = accountRepository;
            _context = context;
            _hubContext = hubContext;
        }

        public async Task<List<ChatRoomListDto>> GetRoomsAsync(string accountId)
        {
            var principal = await ResolvePrincipalAsync(accountId);
            return await _chatRepository.GetRoomsForUserAsync(principal.UserId, principal.IsAdmin);
        }

        public async Task<ChatRoomListDto> GetOrCreateRoomAsync(string accountId, CreateChatRoomRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.ProductId) && string.IsNullOrWhiteSpace(request.SellerId))
            {
                throw new ArgumentException("ProductId or SellerId is required.");
            }

            var principal = await ResolvePrincipalAsync(accountId);
            Product? product = null;
            string? sellerId = request.SellerId;
            var isProductRoom = !string.IsNullOrWhiteSpace(request.ProductId);

            if (isProductRoom)
            {
                product = await _context.Product
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.ProductId == request.ProductId && p.IsDeleted != true);

                if (product == null)
                {
                    throw new KeyNotFoundException("Product not found.");
                }

                sellerId = product.SellerId;
            }

            if (string.IsNullOrWhiteSpace(sellerId))
            {
                throw new InvalidOperationException("SellerId is required.");
            }

            var sellerExists = await _context.User
                .AsNoTracking()
                .AnyAsync(u => u.UserId == sellerId && u.IsDeleted != true);
            if (!sellerExists)
            {
                throw new KeyNotFoundException("Seller not found.");
            }

            if (sellerId == principal.UserId && !principal.IsAdmin)
            {
                throw new UnauthorizedAccessException("You cannot create a chat room with yourself.");
            }

            var buyerId = principal.UserId;
            var isNewRoom = false;
            var room = product != null
                ? await _chatRepository.GetRoomByProductAndBuyerAsync(product.ProductId, buyerId)
                : await _chatRepository.GetBusinessRoomAsync(sellerId, buyerId);

            if (room == null)
            {
                isNewRoom = true;
                room = await _chatRepository.CreateRoomAsync(new ChatRoom
                {
                    RoomId = RetradeBE.Utils.IdGenerator.GenerateId("room"),
                    BuyerId = buyerId,
                    SellerId = sellerId,
                    ProductId = product?.ProductId,
                    RoomType = product == null ? "Business" : "Product",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsDeleted = false
                });

                if (product != null)
                {
                    await AddAutoSellerGreetingAsync(room, product, sellerId);
                }
            }

            var rooms = await _chatRepository.GetRoomsForUserAsync(principal.UserId, principal.IsAdmin);
            var roomDto = rooms.First(r => r.RoomId == room.RoomId);

            if (isNewRoom)
            {
                try
                {
                    var sellerRooms = await _chatRepository.GetRoomsForUserAsync(sellerId, false);
                    var sellerRoomDto = sellerRooms.FirstOrDefault(r => r.RoomId == room.RoomId) ?? roomDto;

                    var buyerRooms = await _chatRepository.GetRoomsForUserAsync(buyerId, false);
                    var buyerRoomDto = buyerRooms.FirstOrDefault(r => r.RoomId == room.RoomId) ?? roomDto;

                    await _hubContext.Clients
                        .Group(ChatHub.GetUserGroupName(sellerId))
                        .SendAsync("RoomCreated", sellerRoomDto);

                    await _hubContext.Clients
                        .Group(ChatHub.GetUserGroupName(buyerId))
                        .SendAsync("RoomCreated", buyerRoomDto);

                    if (!string.IsNullOrWhiteSpace(principal.AccountId) && principal.AccountId != buyerId)
                    {
                        await _hubContext.Clients
                            .Group(ChatHub.GetUserGroupName(principal.AccountId))
                            .SendAsync("RoomCreated", buyerRoomDto);
                    }
                }
                catch
                {
                    // Ignore broadcast errors so room creation API call succeeds
                }
            }

            return roomDto;
        }

        public async Task<List<ChatMessageDto>> GetMessagesAsync(string accountId, string roomId, int page, int limit)
        {
            var principal = await ResolvePrincipalAsync(accountId);
            await EnsureCanAccessRoomAsync(roomId, principal);
            var messages = await _chatRepository.GetMessagesByRoomIdAsync(roomId, principal.UserId, page, limit);
            return messages.Select(message => MapMessage(message, principal.UserId)).ToList();
        }

        public async Task<ChatMessageDto> SendMessageAsync(string accountId, string roomId, SendMessageRequestDto request)
        {
            var principal = await ResolvePrincipalAsync(accountId);
            var room = await EnsureCanAccessRoomAsync(roomId, principal);

            var message = (request.Message ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(message))
            {
                throw new ArgumentException("Message is required.");
            }

            if (message.Length > 2000)
            {
                throw new ArgumentException("Message is too long.");
            }

            var now = DateTime.UtcNow;
            var saved = await _chatRepository.AddMessageAsync(new Chat
            {
                ChatId = RetradeBE.Utils.IdGenerator.GenerateId("chat"),
                RoomId = room.RoomId,
                SenderId = principal.UserId,
                Message = message,
                MessageType = string.IsNullOrWhiteSpace(request.MessageType) ? "Text" : request.MessageType,
                IsRead = false,
                IsRecalled = false,
                DeletedForSender = false,
                DeletedForReceiver = false,
                CreatedAt = now,
                UpdatedAt = now,
                IsDeleted = false
            });

            var dto = MapMessage(saved, principal.UserId);
            await _hubContext.Clients
                .Group(ChatHub.GetRoomGroupName(room.RoomId))
                .SendAsync("ReceiveMessage", dto);

            var targetUserIds = new[] { room.BuyerId, room.SellerId }
                .Where(id => !string.IsNullOrWhiteSpace(id) && id != principal.UserId)
                .Distinct()
                .ToList();

            foreach (var targetUserId in targetUserIds)
            {
                await _hubContext.Clients
                    .Group(ChatHub.GetUserGroupName(targetUserId!))
                    .SendAsync("ChatNotification", new
                    {
                        RoomId = room.RoomId,
                        Message = dto,
                        ProductId = room.ProductId,
                        SenderId = principal.UserId
                    });
            }

            return dto;
        }

        public async Task<bool> DeleteMessageAsync(string accountId, string roomId, string messageId)
        {
            var principal = await ResolvePrincipalAsync(accountId);
            await EnsureCanAccessRoomAsync(roomId, principal);

            var message = await _chatRepository.GetMessageByIdAsync(messageId);
            if (message == null || message.RoomId != roomId)
            {
                throw new KeyNotFoundException("Message not found.");
            }

            if (message.SenderId == principal.UserId)
            {
                message.DeletedForSender = true;
            }
            else
            {
                message.DeletedForReceiver = true;
            }

            message.UpdatedAt = DateTime.UtcNow;
            if (message.DeletedForSender == true && message.DeletedForReceiver == true)
            {
                message.IsDeleted = true;
            }

            await _chatRepository.UpdateMessageAsync(message);
            var userGroup = ChatHub.GetUserGroupName(principal.UserId);
            var accountGroup = ChatHub.GetUserGroupName(principal.AccountId);

            var deletePayload = new
            {
                RoomId = roomId,
                ChatId = messageId,
                UserId = principal.UserId
            };

            await _hubContext.Clients.Group(userGroup).SendAsync("MessageDeleted", deletePayload);
            if (accountGroup != userGroup)
            {
                await _hubContext.Clients.Group(accountGroup).SendAsync("MessageDeleted", deletePayload);
            }

            return true;
        }

        public async Task<ChatMessageDto> RecallMessageAsync(string accountId, string roomId, string messageId)
        {
            var principal = await ResolvePrincipalAsync(accountId);
            await EnsureCanAccessRoomAsync(roomId, principal);

            var message = await _chatRepository.GetMessageByIdAsync(messageId);
            if (message == null || message.RoomId != roomId)
            {
                throw new KeyNotFoundException("Message not found.");
            }

            if (message.SenderId != principal.UserId)
            {
                throw new UnauthorizedAccessException("You can only recall your own message.");
            }

            if (message.IsRecalled == true)
            {
                return MapMessage(message, principal.UserId);
            }

            if (message.CreatedAt == null || DateTime.UtcNow - message.CreatedAt.Value > RecallWindow)
            {
                throw new InvalidOperationException("Messages can only be recalled within 3 minutes.");
            }

            message.IsRecalled = true;
            message.RecalledAt = DateTime.UtcNow;
            message.Message = "Tin nhắn đã bị thu hồi";
            message.MessageType = "Recall";
            message.UpdatedAt = DateTime.UtcNow;

            var saved = await _chatRepository.UpdateMessageAsync(message);
            var dto = MapMessage(saved, principal.UserId);

            await _hubContext.Clients
                .Group(ChatHub.GetRoomGroupName(roomId))
                .SendAsync("MessageRecalled", dto);

            var room = await _chatRepository.GetRoomByIdAsync(roomId);
            if (room != null)
            {
                var targetUserIds = new[] { room.BuyerId, room.SellerId }
                    .Where(id => !string.IsNullOrWhiteSpace(id) && id != principal.UserId)
                    .Distinct();

                foreach (var targetUserId in targetUserIds)
                {
                    await _hubContext.Clients
                        .Group(ChatHub.GetUserGroupName(targetUserId!))
                        .SendAsync("ChatNotification", new
                        {
                            RoomId = room.RoomId,
                            Message = dto,
                            ProductId = room.ProductId,
                            SenderId = principal.UserId
                        });
                }
            }

            return dto;
        }

        public async Task<int> MarkMessagesAsReadAsync(string accountId, string roomId)
        {
            var principal = await ResolvePrincipalAsync(accountId);
            var room = await EnsureCanAccessRoomAsync(roomId, principal);
            var count = await _chatRepository.MarkMessagesAsReadAsync(roomId, principal.UserId);

            if (count > 0)
            {
                await _hubContext.Clients
                    .Group(ChatHub.GetRoomGroupName(room.RoomId))
                    .SendAsync("MessagesRead", new
                    {
                        RoomId = room.RoomId,
                        ReaderId = principal.UserId,
                        ReadAt = DateTime.UtcNow,
                        Count = count
                    });
            }

            return count;
        }

        public async Task<bool> ClearRoomMessagesAsync(string accountId, string roomId)
        {
            var principal = await ResolvePrincipalAsync(accountId);
            var room = await EnsureCanAccessRoomAsync(roomId, principal);

            await _chatRepository.ClearRoomMessagesAsync(roomId, principal.UserId);
            await _hubContext.Clients
                .Group(ChatHub.GetUserGroupName(principal.UserId))
                .SendAsync("ChatCleared", new { RoomId = room.RoomId, UserId = principal.UserId });

            return true;
        }


        private async Task<ChatRoom> EnsureCanAccessRoomAsync(string roomId, ChatPrincipal principal)
        {
            var room = await _chatRepository.GetRoomByIdAsync(roomId);
            if (room == null)
            {
                throw new KeyNotFoundException("Chat room not found.");
            }

            if (!principal.IsAdmin && room.BuyerId != principal.UserId && room.SellerId != principal.UserId)
            {
                throw new UnauthorizedAccessException("You do not have permission to access this chat room.");
            }

            return room;
        }

        private async Task<ChatPrincipal> ResolvePrincipalAsync(string accountId)
        {
            var account = await _accountRepository.GetByIdAsync(accountId);
            if (account == null || string.IsNullOrWhiteSpace(account.UserId))
            {
                throw new UnauthorizedAccessException("Account is not linked to a user.");
            }

            var roles = await _accountRepository.GetRolesAsync(accountId);
            return new ChatPrincipal(
                account.AccountId,
                account.UserId,
                roles.Any(role => string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase)));
        }

        private async Task AddAutoSellerGreetingAsync(ChatRoom room, Product product, string sellerId)
        {
            var now = DateTime.UtcNow;
            var saved = await _chatRepository.AddMessageAsync(new Chat
            {
                ChatId = RetradeBE.Utils.IdGenerator.GenerateId("chat"),
                RoomId = room.RoomId,
                SenderId = sellerId,
                Message = $"Xin chao, minh thay ban dang quan tam san pham \"{product.Name ?? "nay"}\". Ban can minh ho tro them thong tin gi khong?",
                MessageType = "Auto",
                IsRead = false,
                CreatedAt = now,
                UpdatedAt = now,
                IsDeleted = false,
                IsRecalled = false,
                DeletedForSender = false,
                DeletedForReceiver = false
            });

            var dto = MapMessage(saved, null);

            await _hubContext.Clients
                .Group(ChatHub.GetRoomGroupName(room.RoomId))
                .SendAsync("ReceiveMessage", dto);

            if (!string.IsNullOrWhiteSpace(room.BuyerId))
            {
                await _hubContext.Clients
                    .Group(ChatHub.GetUserGroupName(room.BuyerId))
                    .SendAsync("ChatNotification", new
                    {
                        RoomId = room.RoomId,
                        Message = dto,
                        ProductId = room.ProductId,
                        SenderId = sellerId
                    });
            }
        }

        private static ChatMessageDto MapMessage(Chat chat, string? currentUserId = null)
        {
            var senderName = chat.Sender == null
                ? null
                : $"{chat.Sender.FirstName} {chat.Sender.LastName}".Trim();
            var isRecalled = chat.IsRecalled == true;
            var canRecall = currentUserId != null &&
                chat.SenderId == currentUserId &&
                !isRecalled &&
                chat.CreatedAt.HasValue &&
                DateTime.UtcNow - chat.CreatedAt.Value <= RecallWindow;

            return new ChatMessageDto
            {
                ChatId = chat.ChatId,
                RoomId = chat.RoomId,
                SenderId = chat.SenderId,
                SenderName = string.IsNullOrWhiteSpace(senderName) ? chat.Sender?.Email : senderName,
                SenderAvatarUrl = chat.Sender?.AvatarUrl,
                Message = isRecalled ? "Tin nhắn đã bị thu hồi" : chat.Message,
                MessageType = chat.MessageType,
                IsRead = chat.IsRead == true,
                IsRecalled = isRecalled,
                CanRecall = canRecall,
                ReadAt = chat.ReadAt,
                RecalledAt = chat.RecalledAt,
                CreatedAt = chat.CreatedAt
            };
        }

        private sealed record ChatPrincipal(string AccountId, string UserId, bool IsAdmin);
    }
}
