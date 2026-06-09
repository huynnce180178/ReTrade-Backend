using RetradeBE.Models;

namespace RetradeBE.Repositories
{
    public interface IAddressRepository
    {
        Task<string?> GetUserIdByAccountIdAsync(string accountId);
        Task<List<Address>> GetActiveByUserIdAsync(string userId);
        Task<Address?> GetOwnedActiveAsync(string userId, string addressId);
        Task<bool> HasActiveAddressAsync(string userId);
        Task<int> CountAsync();
        Task AddAsync(Address address);
        Task SaveChangesAsync();
    }
}
