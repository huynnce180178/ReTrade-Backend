using Microsoft.EntityFrameworkCore;
using RetradeBE.Data;
using RetradeBE.Models;
using RetradeBE.Models.DTOs;

namespace RetradeBE.Repositories
{
    public class ChatRepository : IChatRepository
    {
        private readonly AppDbContext _context;

        public ChatRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<ChatRoom?> GetRoomByIdAsync(string roomId)
        {
            return await _context.ChatRoom
                .Include(r => r.Buyer)
                .Include(r => r.Seller)
                .Include(r => r.Product)
                    .ThenInclude(p => p!.ProductImage)
                    .ThenInclude(pi => pi.Image)
                .Include(r => r.Chat.Where(c => c.IsDeleted != true))
                    .ThenInclude(c => c.Sender)
                .FirstOrDefaultAsync(r => r.RoomId == roomId && r.IsDeleted != true);
        }

        public async Task<ChatRoom?> GetRoomByProductAndBuyerAsync(string productId, string buyerId)
        {
            return await _context.ChatRoom
                .Include(r => r.Buyer)
                .Include(r => r.Seller)
                .Include(r => r.Product)
                    .ThenInclude(p => p!.ProductImage)
                    .ThenInclude(pi => pi.Image)
                .FirstOrDefaultAsync(r =>
                    r.ProductId == productId &&
                    r.BuyerId == buyerId &&
                    (r.RoomType == "Product" || r.RoomType == null) &&
                    r.IsDeleted != true);
        }

        public async Task<ChatRoom?> GetBusinessRoomAsync(string sellerId, string buyerId)
        {
            return await _context.ChatRoom
                .Include(r => r.Buyer)
                .Include(r => r.Seller)
                .FirstOrDefaultAsync(r =>
                    r.ProductId == null &&
                    r.SellerId == sellerId &&
                    r.BuyerId == buyerId &&
                    (r.RoomType == "Business" || r.RoomType == null) &&
                    r.IsDeleted != true);
        }

        public async Task<ChatRoom> CreateRoomAsync(ChatRoom room)
        {
            await _context.ChatRoom.AddAsync(room);
            await _context.SaveChangesAsync();
            return (await GetRoomByIdAsync(room.RoomId))!;
        }

        public async Task<Chat> AddMessageAsync(Chat chat)
        {
            await _context.Chat.AddAsync(chat);
            var room = await _context.ChatRoom.FirstOrDefaultAsync(r => r.RoomId == chat.RoomId);
            if (room != null)
            {
                room.UpdatedAt = chat.CreatedAt;
            }
            await _context.SaveChangesAsync();

            return (await _context.Chat
                .Include(c => c.Sender)
                .FirstAsync(c => c.ChatId == chat.ChatId));
        }

        public async Task<Chat?> GetMessageByIdAsync(string messageId)
        {
            return await _context.Chat
                .Include(c => c.Room)
                .Include(c => c.Sender)
                .FirstOrDefaultAsync(c => c.ChatId == messageId && c.IsDeleted != true);
        }

        public async Task<Chat> UpdateMessageAsync(Chat chat)
        {
            _context.Chat.Update(chat);
            await _context.SaveChangesAsync();

            return (await _context.Chat
                .Include(c => c.Sender)
                .FirstAsync(c => c.ChatId == chat.ChatId));
        }

        public async Task<List<Chat>> GetMessagesByRoomIdAsync(string roomId, string userId, int page, int limit)
        {
            var safePage = Math.Max(1, page);
            var safeLimit = Math.Clamp(limit, 1, 100);

            var messages = await _context.Chat
                .AsNoTracking()
                .Include(c => c.Sender)
                .Where(c =>
                    c.RoomId == roomId &&
                    c.IsDeleted != true &&
                    !((c.SenderId == userId && c.DeletedForSender == true) ||
                      (c.SenderId != userId && c.DeletedForReceiver == true)))
                .OrderByDescending(c => c.CreatedAt)
                .Skip((safePage - 1) * safeLimit)
                .Take(safeLimit)
                .ToListAsync();

            return messages
                .OrderBy(c => c.CreatedAt)
                .ToList();
        }

        public async Task<List<ChatRoomListDto>> GetRoomsForUserAsync(string userId, bool isAdmin)
        {
            var rooms = await _context.ChatRoom
                .AsNoTracking()
                .Include(r => r.Buyer)
                .Include(r => r.Seller)
                .Include(r => r.Product)
                    .ThenInclude(p => p!.ProductImage)
                    .ThenInclude(pi => pi.Image)
                .Where(r =>
                    r.IsDeleted != true &&
                    (r.RoomType == "Product" || r.RoomType == "Business" || r.RoomType == null) &&
                    (isAdmin || r.BuyerId == userId || r.SellerId == userId))
                .OrderByDescending(r => r.UpdatedAt ?? r.CreatedAt)
                .ToListAsync();

            var roomIds = rooms.Select(r => r.RoomId).ToList();
            var lastMessages = await _context.Chat
                .AsNoTracking()
                .Include(c => c.Sender)
                .Where(c =>
                    c.RoomId != null &&
                    roomIds.Contains(c.RoomId) &&
                    c.IsDeleted != true &&
                    !(c.DeletedForSender == true && c.DeletedForReceiver == true) &&
                    !((c.SenderId == userId && c.DeletedForSender == true) ||
                      (c.SenderId != userId && c.DeletedForReceiver == true)))
                .GroupBy(c => c.RoomId!)
                .Select(g => g.OrderByDescending(c => c.CreatedAt).First())
                .ToListAsync();

            var unreadCounts = await _context.Chat
                .AsNoTracking()
                .Where(c =>
                    c.RoomId != null &&
                    roomIds.Contains(c.RoomId) &&
                    c.IsDeleted != true &&
                    c.IsRead != true &&
                    c.SenderId != userId)
                .GroupBy(c => c.RoomId!)
                .Select(g => new { RoomId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.RoomId, x => x.Count);

            return rooms.Select(room =>
            {
                var lastMessage = lastMessages.FirstOrDefault(m => m.RoomId == room.RoomId);
                var otherParticipant = room.BuyerId == userId ? room.Seller : room.Buyer;

                return new ChatRoomListDto
                {
                    RoomId = room.RoomId,
                    BuyerId = room.BuyerId,
                    SellerId = room.SellerId,
                    ProductId = room.ProductId,
                    RoomType = string.IsNullOrWhiteSpace(room.RoomType)
                        ? (string.IsNullOrWhiteSpace(room.ProductId) ? "Business" : "Product")
                        : room.RoomType,
                    ProductName = room.Product?.Name,
                    ProductImageUrl = GetMainImageUrl(room.Product),
                    ProductPrice = room.Product?.Price,
                    Buyer = MapParticipant(room.Buyer),
                    Seller = MapParticipant(room.Seller),
                    OtherParticipant = MapParticipant(isAdmin ? otherParticipant ?? room.Buyer ?? room.Seller : otherParticipant),
                    LastMessage = lastMessage == null ? null : MapMessage(lastMessage),
                    UnreadCount = unreadCounts.TryGetValue(room.RoomId, out var count) ? count : 0,
                    CreatedAt = room.CreatedAt,
                    UpdatedAt = room.UpdatedAt
                };
            }).ToList();
        }

        public async Task<int> MarkMessagesAsReadAsync(string roomId, string userId)
        {
            var now = DateTime.UtcNow;
            var messages = await _context.Chat
                .Where(c =>
                    c.RoomId == roomId &&
                    c.IsDeleted != true &&
                    c.SenderId != userId &&
                    c.IsRead != true)
                .ToListAsync();

            foreach (var message in messages)
            {
                message.IsRead = true;
                message.ReadAt = now;
                message.UpdatedAt = now;
            }

            await _context.SaveChangesAsync();
            return messages.Count;
        }

        private static ChatParticipantDto? MapParticipant(User? user)
        {
            if (user == null) return null;
            var displayName = $"{user.FirstName} {user.LastName}".Trim();
            return new ChatParticipantDto
            {
                UserId = user.UserId,
                DisplayName = string.IsNullOrWhiteSpace(displayName) ? user.Email : displayName,
                Email = user.Email,
                AvatarUrl = user.AvatarUrl
            };
        }

        private static ChatMessageDto MapMessage(Chat chat)
        {
            var senderName = chat.Sender == null
                ? null
                : $"{chat.Sender.FirstName} {chat.Sender.LastName}".Trim();
            var isRecalled = chat.IsRecalled == true;

            return new ChatMessageDto
            {
                ChatId = chat.ChatId,
                RoomId = chat.RoomId,
                SenderId = chat.SenderId,
                SenderName = string.IsNullOrWhiteSpace(senderName) ? chat.Sender?.Email : senderName,
                SenderAvatarUrl = chat.Sender?.AvatarUrl,
                Message = isRecalled ? "Tin nhan da bi thu hoi" : chat.Message,
                MessageType = chat.MessageType,
                IsRead = chat.IsRead == true,
                IsRecalled = isRecalled,
                CanRecall = false,
                ReadAt = chat.ReadAt,
                RecalledAt = chat.RecalledAt,
                CreatedAt = chat.CreatedAt
            };
        }

        private static string? GetMainImageUrl(Product? product)
        {
            return product?.ProductImage
                .Where(pi => pi.IsMain == true)
                .Select(pi => pi.Image?.ImageUrl)
                .FirstOrDefault()
                ?? product?.ProductImage
                    .OrderBy(pi => pi.SortOrder)
                    .Select(pi => pi.Image?.ImageUrl)
                    .FirstOrDefault();
        }
    }
}
