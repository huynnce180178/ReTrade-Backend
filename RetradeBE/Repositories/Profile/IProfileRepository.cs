using RetradeBE.Models;

namespace RetradeBE.Repositories
{
    public interface IProfileRepository
    {
        Task<Account?> GetAccountWithUserAsync(string accountId);
        Task<User?> GetUserByIdAsync(string userId);
        Task<Account?> GetPrimaryAccountByUserIdAsync(string userId);
        Task<List<Address>> GetActiveAddressesByUserIdAsync(string userId);
        Task<Address?> GetAddressByIdAsync(string addressId);
        Task<int> CountAddressesAsync();
        Task AddAddressAsync(Address address);
        Task UpdateAddressAsync(Address address);
        Task UpdateUserAsync(User user);
        Task UpdateAccountAsync(Account account);
        Task<bool> UsernameExistsAsync(string username, string excludeAccountId);
        Task<bool> EmailExistsAsync(string email, string excludeUserId);
        Task<bool> FollowExistsAsync(string followerId, string followedUserId);
        Task<int> CountFollowsAsync();
        Task<int> CountFollowersAsync(string userId);
        Task<int> CountFollowingAsync(string userId);
        Task<int> CountProductsAsync(string sellerId);
        Task<double?> GetAverageSellerRatingAsync(string sellerId);
        Task AddFollowAsync(UserFollow follow);
        Task<UserFollow?> GetFollowAsync(string followerId, string followedUserId);
        Task RemoveFollowAsync(UserFollow follow);
    }
}
