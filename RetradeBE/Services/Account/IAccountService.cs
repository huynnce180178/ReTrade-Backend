using RetradeBE.Models;
using RetradeBE.Models.DTOs;
using RetradeBE.Models.DTOs.Admin;

namespace RetradeBE.Services
{
    public interface IAccountService
    {
        IQueryable<UserListDto> QueryUserList();
        Task<bool> BanUserAsync(string accountId);
        Task<bool> DeactivateMyAccountAsync(string accountId);

        Task<IEnumerable<Account>> GetAllAsync();
        Task<Account> GetByIdAsync(object id);
        Task AddAsync(Account item);
        Task UpdateAsync(Account item);
        Task DeleteAsync(object id);
        Task RestoreAsync(object id);
        Task<string> RegisterAsync(RegisterDto dto);
        Task<string> ResendOtpAsync(string email);
        Task<bool> VerifyAsync(VerifyDto dto);
        Task<object?> LoginAsync(LoginDto dto);
        Task<object?> LoginWithGoogleAsync(string accessToken);
        Task<string> ForgotPasswordAsync(string email);
        Task<string> ResetPasswordAsync(ResetPasswordDto dto);
        Task<string> PasswordRecoveryAsync(string email);
        Task<string> ChangePasswordAsync(string accountId, ChangePasswordDto dto);
        Task<string> SetPasswordAsync(string accountId, SetPasswordDto dto);
    }
}
