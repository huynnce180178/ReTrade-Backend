using Microsoft.EntityFrameworkCore;
using RetradeBE.Models;
using RetradeBE.Models.DTOs;
using RetradeBE.Repositories;

namespace RetradeBE.Services
{
    public class UserSearchService : IUserSearchService
    {
        private readonly IUserSearchRepository _userSearchRepository;
        private readonly IAccountRepository _accountRepository;

        public UserSearchService(IUserSearchRepository userSearchRepository, IAccountRepository accountRepository)
        {
            _userSearchRepository = userSearchRepository;
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

            var history = await _userSearchRepository.GetHistoryByUserIdAsync(userId, limit);
            
            return history.Select(s => new UserSearchResponseDto
            {
                SearchId = s.SearchId,
                Keyword = s.Keyword,
                CategoryId = s.CategoryId,
                CategoryName = s.Category != null ? s.Category.Name : null,
                CreatedAt = s.CreatedAt
            }).ToList();
        }

        public async Task<UserSearchResponseDto> SaveSearchAsync(string accountId, UserSearchCreateDto dto)
        {
            var userId = await ResolveUserIdAsync(accountId);

            if (string.IsNullOrWhiteSpace(dto.Keyword) && string.IsNullOrWhiteSpace(dto.CategoryId))
                throw new Exception("Keyword hoặc CategoryId phải được cung cấp.");

            // Check for duplicate recent search (same keyword within last 5 minutes)
            if (!string.IsNullOrWhiteSpace(dto.Keyword))
            {
                var recentDuplicate = await _userSearchRepository.GetRecentDuplicateAsync(userId, dto.Keyword.Trim());

                if (recentDuplicate != null)
                {
                    // Update timestamp instead of creating duplicate
                    recentDuplicate.CreatedAt = DateTime.UtcNow;
                    await _userSearchRepository.UpdateAsync(recentDuplicate);

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

            await _userSearchRepository.AddAsync(search);

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

            var search = await _userSearchRepository.GetByIdAndUserIdAsync(searchId, userId);

            if (search == null)
                throw new Exception("Lịch sử tìm kiếm không tồn tại.");

            await _userSearchRepository.RemoveAsync(search);
        }

        public async Task ClearAllSearchAsync(string accountId)
        {
            var userId = await ResolveUserIdAsync(accountId);

            var searches = await _userSearchRepository.GetAllByUserIdAsync(userId);

            await _userSearchRepository.RemoveRangeAsync(searches);
        }
    }
}
