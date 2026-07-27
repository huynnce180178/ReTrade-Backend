using RetradeBE.Models;

namespace RetradeBE.Repositories
{
    public interface IAssistantChatSessionRepository
    {
        Task<ChatSession?> GetByIdAsync(string sessionId);
        Task<ChatSession?> GetOwnedSessionAsync(string? userId, string sessionId);
        Task<List<ChatSession>> GetActiveSessionsByUserAsync(string userId);
        Task AddAsync(ChatSession session);
        Task UpdateAsync(ChatSession session);
    }
}
