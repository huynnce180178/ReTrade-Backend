using RetradeBE.Models.DTOs;
using System.Threading.Tasks;

namespace RetradeBE.Services
{
    public interface IWishlistService
    {
        /// <summary>Thêm sản phẩm vào wishlist của user đang đăng nhập.</summary>
        Task<WishlistDetailDto> AddToWishlistAsync(string accountId, AddToWishlistDto dto);

        /// <summary>Lấy chi tiết wishlist của user đang đăng nhập (hỗ trợ OData).</summary>
        Task<WishlistDetailDto> GetWishlistDetailAsync(string accountId);

        /// <summary>Xóa một item khỏi wishlist.</summary>
        Task RemoveWishlistItemAsync(string accountId, string wishlistItemId);
    }
}
