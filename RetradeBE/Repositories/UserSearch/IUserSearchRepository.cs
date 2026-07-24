using RetradeBE.Models;

namespace RetradeBE.Repositories
{
    public interface IUserSearchRepository
    {
        IQueryable<UserSearch> Query();
        Task<List<UserSearch>> GetHistoryByUserIdAsync(string userId, int limit);
        Task<UserSearch?> GetRecentDuplicateAsync(string userId, string keyword);
        Task<UserSearch?> GetByIdAndUserIdAsync(string searchId, string userId);
        Task<List<UserSearch>> GetAllByUserIdAsync(string userId);
        Task AddAsync(UserSearch userSearch);
        Task UpdateAsync(UserSearch userSearch);
        Task RemoveAsync(UserSearch userSearch);
        Task RemoveRangeAsync(IEnumerable<UserSearch> userSearches);
    }
}
