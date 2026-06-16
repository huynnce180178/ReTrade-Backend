using Microsoft.EntityFrameworkCore;
using RetradeBE.Data;
using RetradeBE.Models;
using RetradeBE.Models.DTOs;
using RetradeBE.Repositories;

namespace RetradeBE.Services
{
    public class UserFavoriteService : IUserFavoriteService
    {
        private readonly AppDbContext _context;
        private readonly IAccountRepository _accountRepository;

        public UserFavoriteService(AppDbContext context, IAccountRepository accountRepository)
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

        public async Task<List<UserFavoriteResponseDto>> GetFavoritesAsync(string accountId)
        {
            var userId = await ResolveUserIdAsync(accountId);

            return await _context.UserFavorite
                .Where(f => f.UserId == userId)
                .OrderByDescending(f => f.CreatedAt)
                .Select(f => new UserFavoriteResponseDto
                {
                    FavoriteId = f.FavoriteId,
                    CategoryId = f.CategoryId,
                    CategoryName = f.Category != null ? f.Category.Name : null,
                    CategoryImageUrl = f.Category != null
                        ? f.Category.CategoryImage
                            .OrderByDescending(ci => ci.CreatedAt)
                            .Select(ci => ci.Image != null ? ci.Image.ImageUrl : null)
                            .FirstOrDefault()
                        : null,
                    CreatedAt = f.CreatedAt
                })
                .ToListAsync();
        }

        public async Task<UserFavoriteResponseDto> AddFavoriteAsync(string accountId, UserFavoriteCreateDto dto)
        {
            var userId = await ResolveUserIdAsync(accountId);

            if (string.IsNullOrWhiteSpace(dto.CategoryId))
                throw new Exception("CategoryId là bắt buộc.");

            // Check category exists and is active
            var category = await _context.Category
                .FirstOrDefaultAsync(c => c.CategoryId == dto.CategoryId && c.Status == "Active");
            if (category == null)
                throw new Exception("Danh mục không tồn tại hoặc không hoạt động.");

            // Check duplicate
            var existing = await _context.UserFavorite
                .FirstOrDefaultAsync(f => f.UserId == userId && f.CategoryId == dto.CategoryId);
            if (existing != null)
                throw new Exception("Danh mục này đã nằm trong danh sách yêu thích.");

            // Check limit (max 10)
            var currentCount = await _context.UserFavorite
                .CountAsync(f => f.UserId == userId);
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

            await _context.UserFavorite.AddAsync(favorite);
            await _context.SaveChangesAsync();

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

            var favorite = await _context.UserFavorite
                .FirstOrDefaultAsync(f => f.UserId == userId && f.CategoryId == categoryId);

            if (favorite == null)
                throw new Exception("Danh mục không nằm trong danh sách yêu thích.");

            _context.UserFavorite.Remove(favorite);
            await _context.SaveChangesAsync();
        }
    }
}
