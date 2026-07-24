using RetradeBE.Models;
using RetradeBE.Models.DTOs;
using RetradeBE.Repositories;

namespace RetradeBE.Services
{
    public class UserFavoriteService : IUserFavoriteService
    {
        private readonly IUserFavoriteRepository _userFavoriteRepository;
        private readonly IAccountRepository _accountRepository;
        private readonly ICategoryRepository _categoryRepository;

        public UserFavoriteService(
            IUserFavoriteRepository userFavoriteRepository,
            IAccountRepository accountRepository,
            ICategoryRepository categoryRepository)
        {
            _userFavoriteRepository = userFavoriteRepository;
            _accountRepository = accountRepository;
            _categoryRepository = categoryRepository;
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

        public async Task<List<UserFavoriteResponseDto>> GetFavoritesAsync(string accountId)
        {
            var userId = await ResolveUserIdAsync(accountId);

            var favorites = await _userFavoriteRepository.GetFavoritesByUserIdAsync(userId);

            return favorites.Select(f => new UserFavoriteResponseDto
            {
                FavoriteId = f.FavoriteId,
                CategoryId = f.CategoryId,
                CategoryName = f.Category?.Name,
                CategoryImageUrl = f.Category?.CategoryImage
                    .OrderByDescending(ci => ci.CreatedAt)
                    .Select(ci => ci.Image?.ImageUrl)
                    .FirstOrDefault(),
                CreatedAt = f.CreatedAt
            }).ToList();
        }

        public async Task<UserFavoriteResponseDto> AddFavoriteAsync(string accountId, UserFavoriteCreateDto dto)
        {
            var userId = await ResolveUserIdAsync(accountId);

            if (string.IsNullOrWhiteSpace(dto.CategoryId))
                throw new Exception("CategoryId là bắt buộc.");

            var category = await _categoryRepository.GetByIdAsync(dto.CategoryId);
            if (category == null || category.Status != "Active")
                throw new Exception("Danh mục không tồn tại hoặc không hoạt động.");

            var existing = await _userFavoriteRepository.GetByUserIdAndCategoryIdAsync(userId, dto.CategoryId);
            if (existing != null)
                throw new Exception("Danh mục này đã nằm trong danh sách yêu thích.");

            var currentCount = await _userFavoriteRepository.CountByUserIdAsync(userId);
            if (currentCount >= 10)
                throw new Exception("Bạn chỉ có thể chọn tối đa 10 danh mục yêu thích.");

            var favoriteId = $"UF_{Guid.NewGuid():N}";
            var favorite = new UserFavorite
            {
                FavoriteId = favoriteId,
                UserId = userId,
                CategoryId = dto.CategoryId,
                CreatedAt = DateTime.UtcNow
            };

            await _userFavoriteRepository.AddAsync(favorite);

            return new UserFavoriteResponseDto
            {
                FavoriteId = favorite.FavoriteId,
                CategoryId = favorite.CategoryId,
                CategoryName = category.Name,
                CreatedAt = favorite.CreatedAt
            };
        }

        public async Task RemoveFavoriteAsync(string accountId, string categoryId)
        {
            var userId = await ResolveUserIdAsync(accountId);

            var favorite = await _userFavoriteRepository.GetByUserIdAndCategoryIdAsync(userId, categoryId);

            if (favorite == null)
                throw new Exception("Danh mục không nằm trong danh sách yêu thích.");

            await _userFavoriteRepository.RemoveAsync(favorite);
        }
    }
}
