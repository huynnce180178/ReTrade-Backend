using RetradeBE.Models.DTOs;

namespace RetradeBE.Services
{
    public interface IChatService
    {
        Task<List<ChatRoomListDto>> GetRoomsAsync(string accountId);
        Task<ChatRoomListDto> GetOrCreateRoomAsync(string accountId, CreateChatRoomRequestDto request);
        Task<List<ChatMessageDto>> GetMessagesAsync(string accountId, string roomId, int page, int limit);
        Task<ChatMessageDto> SendMessageAsync(string accountId, string roomId, SendMessageRequestDto request);
        Task<bool> DeleteMessageAsync(string accountId, string roomId, string messageId);
        Task<ChatMessageDto> RecallMessageAsync(string accountId, string roomId, string messageId);
        Task<int> MarkMessagesAsReadAsync(string accountId, string roomId);
        Task<bool> ClearRoomMessagesAsync(string accountId, string roomId);
    }
}

