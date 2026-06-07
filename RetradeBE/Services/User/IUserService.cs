using RetradeBE.Models;

namespace RetradeBE.Services
{
    public interface IUserService
    {
        Task<IEnumerable<User>> GetAllAsync();
        Task<User> GetByIdAsync(object id);
        Task AddAsync(User item);
        Task UpdateAsync(User item);
        Task DeleteAsync(object id);
    }
}
