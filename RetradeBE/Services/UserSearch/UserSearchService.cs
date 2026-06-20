using Microsoft.EntityFrameworkCore;
using RetradeBE.Data;
using RetradeBE.Models;
using RetradeBE.Models.DTOs;
using RetradeBE.Repositories;

namespace RetradeBE.Services
{
    public class UserSearchService : IUserSearchService
    {
        private readonly AppDbContext _context;
        private readonly IAccountRepository _accountRepository;

        public UserSearchService(AppDbContext context, IAccountRepository accountRepository)
        {
            _context = context;
            _accountRepository = accountRepository;
        }

        private async Task<string> ResolveUserIdAsync(string accountId)
        {
            var account = await _accountRepository.GetByIdAsync(accountId);
            if (account == null)
                throw new Exception("Tài khoản không tồn tại.");
            if (string.IsNullOrEmpty(account.UserId))
                throw new Exception("Tài khoản không liên kết với người dùng.");
            return account.UserId;
        }

        public async Task<List<UserSearchResponseDto>> GetSearchHistoryAsync(string accountId, int limit = 20)
        {
            var userId = await ResolveUserIdAsync(accountId);

            return await _context.UserSearch
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.CreatedAt)
                .Take(limit)
                .Select(s => new UserSearchResponseDto
                {
                    SearchId = s.SearchId,
                    Keyword = s.Keyword,
                    CategoryId = s.CategoryId,
                    CategoryName = s.Category != null ? s.Category.Name : null,
                    CreatedAt = s.CreatedAt
                })
                .ToListAsync();
        }

        public async Task<UserSearchResponseDto> SaveSearchAsync(string accountId, UserSearchCreateDto dto)
        {
            var userId = await ResolveUserIdAsync(accountId);

            if (string.IsNullOrWhiteSpace(dto.Keyword) && string.IsNullOrWhiteSpace(dto.CategoryId))
                throw new Exception("Keyword hoặc CategoryId phải được cung cấp.");

            // Check for duplicate recent search (same keyword within last 5 minutes)
            if (!string.IsNullOrWhiteSpace(dto.Keyword))
            {
                var recentDuplicate = await _context.UserSearch
                    .Where(s => s.UserId == userId
                             && s.Keyword == dto.Keyword.Trim()
                             && s.CreatedAt > DateTime.UtcNow.AddMinutes(-5))
                    .FirstOrDefaultAsync();

                if (recentDuplicate != null)
                {
                    // Update timestamp instead of creating duplicate
                    recentDuplicate.CreatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();

                    return new UserSearchResponseDto
                    {
                        SearchId = recentDuplicate.SearchId,
                        Keyword = recentDuplicate.Keyword,
                        CategoryId = recentDuplicate.CategoryId,
                        CreatedAt = recentDuplicate.CreatedAt
                    };
                }
            }

            var searchId = $"US_{Guid.NewGuid():N}";
            var search = new UserSearch
            {
                SearchId = searchId,
                UserId = userId,
                Keyword = dto.Keyword?.Trim(),
                CategoryId = string.IsNullOrWhiteSpace(dto.CategoryId) ? null : dto.CategoryId,
                CreatedAt = DateTime.UtcNow
            };

            await _context.UserSearch.AddAsync(search);
            await _context.SaveChangesAsync();

            return new UserSearchResponseDto
            {
                SearchId = search.SearchId,
                Keyword = search.Keyword,
                CategoryId = search.CategoryId,
                CreatedAt = search.CreatedAt
            };
        }

        public async Task DeleteSearchAsync(string accountId, string searchId)
        {
            var userId = await ResolveUserIdAsync(accountId);

            var search = await _context.UserSearch
                .FirstOrDefaultAsync(s => s.SearchId == searchId && s.UserId == userId);

            if (search == null)
                throw new Exception("Lịch sử tìm kiếm không tồn tại.");

            _context.UserSearch.Remove(search);
            await _context.SaveChangesAsync();
        }

        public async Task ClearAllSearchAsync(string accountId)
        {
            var userId = await ResolveUserIdAsync(accountId);

            var searches = await _context.UserSearch
                .Where(s => s.UserId == userId)
                .ToListAsync();

            _context.UserSearch.RemoveRange(searches);
            await _context.SaveChangesAsync();
        }
    }
}
