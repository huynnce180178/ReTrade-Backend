using RetradeBE.Models.DTOs;

namespace RetradeBE.Services
{
    public interface IUserFavoriteService
    {
        Task<List<UserFavoriteResponseDto>> GetFavoritesAsync(string accountId);
        Task<UserFavoriteResponseDto> AddFavoriteAsync(string accountId, UserFavoriteCreateDto dto);
        Task RemoveFavoriteAsync(string accountId, string categoryId);
    }
}
