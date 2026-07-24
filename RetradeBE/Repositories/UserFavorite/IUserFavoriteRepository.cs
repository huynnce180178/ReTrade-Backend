using RetradeBE.Models;

namespace RetradeBE.Repositories
{
    public interface IUserFavoriteRepository
    {
        IQueryable<UserFavorite> Query();
        Task<List<UserFavorite>> GetFavoritesByUserIdAsync(string userId);
        Task<UserFavorite?> GetByUserIdAndCategoryIdAsync(string userId, string categoryId);
        Task<int> CountByUserIdAsync(string userId);
        Task AddAsync(UserFavorite favorite);
        Task RemoveAsync(UserFavorite favorite);
    }
}
