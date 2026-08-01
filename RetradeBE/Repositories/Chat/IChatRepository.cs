using RetradeBE.Models;
using RetradeBE.Models.DTOs;

namespace RetradeBE.Repositories
{
    public interface IChatRepository
    {
        Task<ChatRoom?> GetRoomByIdAsync(string roomId);
        Task<ChatRoom?> GetRoomByProductAndBuyerAsync(string productId, string buyerId);
        Task<ChatRoom?> GetBusinessRoomAsync(string sellerId, string buyerId);
        Task<ChatRoom> CreateRoomAsync(ChatRoom room);
        Task<Chat> AddMessageAsync(Chat chat);
        Task<Chat?> GetMessageByIdAsync(string messageId);
        Task<Chat> UpdateMessageAsync(Chat chat);
        Task<List<Chat>> GetMessagesByRoomIdAsync(string roomId, string userId, int page, int limit);
        Task<List<ChatRoomListDto>> GetRoomsForUserAsync(string userId, bool isAdmin);
        Task<int> MarkMessagesAsReadAsync(string roomId, string userId);
        Task<int> ClearRoomMessagesAsync(string roomId, string userId);
    }
}

