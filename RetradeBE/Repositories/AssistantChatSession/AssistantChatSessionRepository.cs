using Microsoft.EntityFrameworkCore;
using RetradeBE.Data;
using RetradeBE.Models;

namespace RetradeBE.Repositories
{
    public class AssistantChatSessionRepository : IAssistantChatSessionRepository
    {
        private readonly AppDbContext _context;

        public AssistantChatSessionRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<ChatSession?> GetByIdAsync(string sessionId)
        {
            return await _context.ChatSession
                .Include(s => s.ChatMessage.OrderBy(m => m.CreatedAt))
                .FirstOrDefaultAsync(s => s.SessionId == sessionId);
        }

        public async Task<ChatSession?> GetOwnedSessionAsync(string? userId, string sessionId)
        {
            return await _context.ChatSession
                .Include(s => s.ChatMessage.OrderBy(m => m.CreatedAt))
                .FirstOrDefaultAsync(s => s.SessionId == sessionId && s.UserId == userId && s.IsActive == true);
        }

        public async Task<List<ChatSession>> GetActiveSessionsByUserAsync(string userId)
        {
            return await _context.ChatSession
                .AsNoTracking()
                .Where(s => s.UserId == userId && s.IsActive == true)
                .OrderByDescending(s => s.LastMessageAt ?? s.StartedAt)
                .ToListAsync();
        }

        public async Task AddAsync(ChatSession session)
        {
            await _context.ChatSession.AddAsync(session);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(ChatSession session)
        {
            _context.ChatSession.Update(session);
            await _context.SaveChangesAsync();
        }
    }
}
