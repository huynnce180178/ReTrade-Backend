using RetradeBE.Models;

namespace RetradeBE.Repositories
{
    public interface IAssistantChatMessageRepository
    {
        Task<List<ChatMessage>> GetBySessionIdAsync(string sessionId);
        Task AddAsync(ChatMessage message);
    }
}
