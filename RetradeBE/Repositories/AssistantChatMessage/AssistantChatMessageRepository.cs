using Microsoft.EntityFrameworkCore;
using RetradeBE.Data;
using RetradeBE.Models;

namespace RetradeBE.Repositories
{
    public class AssistantChatMessageRepository : IAssistantChatMessageRepository
    {
        private readonly AppDbContext _context;

        public AssistantChatMessageRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<ChatMessage>> GetBySessionIdAsync(string sessionId)
        {
            return await _context.ChatMessage
                .AsNoTracking()
                .Where(m => m.SessionId == sessionId)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();
        }

        public async Task AddAsync(ChatMessage message)
        {
            await _context.ChatMessage.AddAsync(message);
            await _context.SaveChangesAsync();
        }
    }
}
