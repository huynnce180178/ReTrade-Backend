using RetradeBE.Models.DTOs;

namespace RetradeBE.Services
{
    public interface IUserSearchService
    {
        Task<List<UserSearchResponseDto>> GetSearchHistoryAsync(string accountId, int limit = 20);
        Task<UserSearchResponseDto> SaveSearchAsync(string accountId, UserSearchCreateDto dto);
        Task DeleteSearchAsync(string accountId, string searchId);
        Task ClearAllSearchAsync(string accountId);
    }
}
