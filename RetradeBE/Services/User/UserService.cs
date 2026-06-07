using RetradeBE.Models;
using RetradeBE.Repositories;

namespace RetradeBE.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _repo;

        public UserService(IUserRepository repo)
        {
            _repo = repo;
        }

        public async Task AddAsync(User item) => await _repo.AddAsync(item);
        public async Task DeleteAsync(object id) => await _repo.DeleteAsync(id);
        public async Task<IEnumerable<User>> GetAllAsync() => await _repo.GetAllAsync();
        public async Task<User> GetByIdAsync(object id) => await _repo.GetByIdAsync(id);
        public async Task UpdateAsync(User item) => await _repo.UpdateAsync(item);
    }
}
