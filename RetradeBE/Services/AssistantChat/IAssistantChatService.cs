using RetradeBE.Models.DTOs.AssistantChat;

namespace RetradeBE.Services.AssistantChat
{
    public interface IAssistantChatService
    {
        Task<AssistantChatResponseDto> SendChatAssistantAsync(string? accountId, AssistantChatRequestDto request, CancellationToken cancellationToken = default);
        Task<List<AssistantChatMessageDto>> GetSessionHistoryAsync(string? accountId, string sessionId);
    }
}
