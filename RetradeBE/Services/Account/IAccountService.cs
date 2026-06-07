using RetradeBE.Models;
using RetradeBE.Models.DTOs;

namespace RetradeBE.Services
{
    public interface IAccountService
    {
        Task<IEnumerable<Account>> GetAllAsync();
        Task<Account> GetByIdAsync(object id);
        Task AddAsync(Account item);
        Task UpdateAsync(Account item);
        Task DeleteAsync(object id);
        Task RestoreAsync(object id);
        Task<UserProfileDto?> GetProfileAsync(string accountId);
        Task<string> RegisterAsync(RegisterDto dto);
        Task<string> ResendOtpAsync(string email);
        Task<bool> VerifyAsync(VerifyDto dto);
        Task<object?> LoginAsync(LoginDto dto);
        Task<object?> LoginWithGoogleAsync(string accessToken);
        Task<string> ForgotPasswordAsync(string email);
        Task<string> ResetPasswordAsync(ResetPasswordDto dto);
        Task<string> PasswordRecoveryAsync(string email);
        Task<string> ChangePasswordAsync(string accountId, ChangePasswordDto dto);
        Task<UserProfileDto?> UpdateProfileAsync(string accountId, UpdateProfileDto dto);
        
    }
}
