using RetradeBE.Models;
using System.Threading.Tasks;

namespace RetradeBE.Repositories
{
    public interface IWishlistRepository
    {
        /// <summary>Lấy hoặc tạo mới Wishlist active của user.</summary>
        Task<Wishlist> GetOrCreateActiveWishlistAsync(string userId);

        /// <summary>Kiểm tra sản phẩm đã có trong wishlist chưa.</summary>
        Task<bool> IsProductInWishlistAsync(string wishlistId, string productId);

        /// <summary>Thêm item vào wishlist.</summary>
        Task AddItemAsync(WishlistItem item);

        /// <summary>Lấy wishlist item theo Id.</summary>
        Task<WishlistItem?> GetItemByIdAsync(string wishlistItemId);

        /// <summary>Lấy wishlist item theo wishlistId + productId.</summary>
        Task<WishlistItem?> GetItemByProductAsync(string wishlistId, string productId);

        /// <summary>Xóa item khỏi wishlist.</summary>
        Task RemoveItemAsync(WishlistItem item);
    }
}
