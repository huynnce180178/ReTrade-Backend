using RetradeBE.Models;

namespace RetradeBE.Repositories
{
    public interface IUserRepository
    {
        Task<IEnumerable<User>> GetAllAsync();
        Task<User> GetByIdAsync(object id);
        Task<User?> GetByEmailAsync(string email);
        Task AddAsync(User item);
        Task UpdateAsync(User item);
        Task DeleteAsync(object id);
        Task<int> CountAllUsersAsync();
    }
}
