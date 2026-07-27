using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.SignalR;
using RetradeBE.Models;
using RetradeBE.Models.DTOs;
using RetradeBE.Models.DTOs.Admin;
using RetradeBE.Hubs;
using RetradeBE.Repositories;
using RetradeBE.Config;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Net.Http.Headers;

namespace RetradeBE.Services
{
    public class AccountService : IAccountService
    {
        private readonly IAccountRepository _repository;
        private readonly IUserRepository _userRepository;
        private readonly IEmailService _emailService;
        private readonly IMemoryCache _cache;
        private readonly JwtSettings _jwtSettings;
        private readonly GoogleSettings _googleSettings;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IMapper _mapper;
        private readonly IWebHostEnvironment _env;
        private readonly IHubContext<AccountHub> _accountHub;
        private readonly ILogger<AccountService> _logger;

        public AccountService(IAccountRepository repository, IUserRepository userRepository, IEmailService emailService, IMemoryCache cache, IOptions<JwtSettings> jwtSettings, IOptions<GoogleSettings> googleSettings, IHttpClientFactory httpClientFactory, IMapper mapper, IWebHostEnvironment env, IHubContext<AccountHub> accountHub, ILogger<AccountService> logger)
        {
            _repository = repository;
            _userRepository = userRepository;
            _emailService = emailService;
            _cache = cache;
            _jwtSettings = jwtSettings.Value;
            _googleSettings = googleSettings.Value;
            _httpClientFactory = httpClientFactory;
            _mapper = mapper;
            _env = env;
            _accountHub = accountHub;
            _logger = logger;
        }

        private async Task<string> GetEmailTemplateAsync(string templateName)
        {
            string path = Path.Combine(_env.ContentRootPath, "Templates", templateName);
            if (!File.Exists(path)) return string.Empty;
            return await File.ReadAllTextAsync(path);
        }

        public IQueryable<UserListDto> QueryUserList()
        {
            return _repository.Query()
                .ProjectTo<UserListDto>(_mapper.ConfigurationProvider);
        }

        public async Task<bool> BanUserAsync(string accountId)
        {
            var account = await _repository.GetByIdAsync(accountId);
            if (account == null) return false;

            var isCurrentlyInactive = account.Status == RetradeBE.Models.Enums.AccountStatusEnum.Ban.ToString();
            account.Status = isCurrentlyInactive
                ? RetradeBE.Models.Enums.AccountStatusEnum.Active.ToString()
                : RetradeBE.Models.Enums.AccountStatusEnum.Ban.ToString();
            account.UpdatedAt = DateTime.UtcNow;
            await _repository.UpdateAsync(account);

            if (!isCurrentlyInactive)
            {
                await _accountHub.Clients
                    .Group(AccountHub.GetAccountGroupName(accountId))
                    .SendAsync("ForceLogout", "Your account has been banned by an administrator.");

                try
                {
                    if (!string.IsNullOrWhiteSpace(account.UserId))
                    {
                        var user = await _userRepository.GetByIdAsync(account.UserId);
                        if (user != null && !string.IsNullOrWhiteSpace(user.Email))
                        {
                            var displayName = !string.IsNullOrWhiteSpace(user.FirstName)
                                ? user.FirstName
                                : (account.Username ?? "User");

                            string template = await GetEmailTemplateAsync("AccountBannedNotice.html");
                            string emailBody;

                            if (!string.IsNullOrWhiteSpace(template))
                            {
                                emailBody = template
                                    .Replace("{{DISPLAY_NAME}}", displayName)
                                    .Replace("{{ACCOUNT_ID}}", account.AccountId)
                                    .Replace("{{BANNED_AT}}", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss 'UTC'"));
                            }
                            else
                            {
                                emailBody = $@"<p>Hello {displayName},</p>
<p>Your ReTrade account has been set to <strong>Inactive</strong> by an administrator.</p>
<p>If you have any questions or believe this was a mistake, please reply directly to this email.</p>
<p>Account ID: {account.AccountId}<br/>Time: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</p>
<p>Regards,<br/>ReTrade Support Team</p>";
                            }

                            await _emailService.SendEmailAsync(
                                user.Email,
                                "[ReTrade] Account Ban Notification",
                                emailBody);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send ban notification email for account {AccountId}", accountId);
                }
            }
            else
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(account.UserId))
                    {
                        var user = await _userRepository.GetByIdAsync(account.UserId);
                        if (user != null && !string.IsNullOrWhiteSpace(user.Email))
                        {
                            var displayName = !string.IsNullOrWhiteSpace(user.FirstName)
                                ? user.FirstName
                                : (account.Username ?? "User");

                            string template = await GetEmailTemplateAsync("AccountReactivatedNotice.html");
                            string emailBody;

                            if (!string.IsNullOrWhiteSpace(template))
                            {
                                emailBody = template
                                    .Replace("{{DISPLAY_NAME}}", displayName)
                                    .Replace("{{ACCOUNT_ID}}", account.AccountId)
                                    .Replace("{{REACTIVATED_AT}}", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss 'UTC'"));
                            }
                            else
                            {
                                emailBody = $@"<p>Hello {displayName},</p>
<p>Your ReTrade account has been <strong>reactivated</strong> and is now active again.</p>
<p>If you did not expect this change or have any concerns, please reply directly to this email.</p>
<p>Account ID: {account.AccountId}<br/>Time: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</p>
<p>Regards,<br/>ReTrade Support Team</p>";
                            }

                            await _emailService.SendEmailAsync(
                                user.Email,
                                "[ReTrade] Account Reactivated",
                                emailBody);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send reactivated notification email for account {AccountId}", accountId);
                }
            }

            return true;
        }

        public async Task<bool> DeactivateMyAccountAsync(string accountId)
        {
            var account = await _repository.GetByIdAsync(accountId);
            if (account == null) return false;

            if (account.Status == RetradeBE.Models.Enums.AccountStatusEnum.Inactive.ToString())
            {
                return false;
            }

            account.Status = RetradeBE.Models.Enums.AccountStatusEnum.Inactive.ToString();
            account.UpdatedAt = DateTime.UtcNow;
            await _repository.UpdateAsync(account);

            await _accountHub.Clients
                .Group(AccountHub.GetAccountGroupName(accountId))
                .SendAsync("ForceLogout", "You have deactivated your account.");

            try
            {
                if (!string.IsNullOrWhiteSpace(account.UserId))
                {
                    var user = await _userRepository.GetByIdAsync(account.UserId);
                    if (user != null && !string.IsNullOrWhiteSpace(user.Email))
                    {
                        var displayName = !string.IsNullOrWhiteSpace(user.FirstName)
                            ? user.FirstName
                            : (account.Username ?? "User");

                        string template = await GetEmailTemplateAsync("AccountSelfDeactivatedNotice.html");
                        string emailBody;

                        if (!string.IsNullOrWhiteSpace(template))
                        {
                            emailBody = template
                                .Replace("{{DISPLAY_NAME}}", displayName)
                                .Replace("{{ACCOUNT_ID}}", account.AccountId)
                                .Replace("{{DEACTIVATED_AT}}", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss 'UTC'"));
                        }
                        else
                        {
                            emailBody = $@"<p>Hello {displayName},</p>
<p>Your ReTrade account has been <strong>deactivated</strong>.</p>
<p>If you did not mean to do this or have any questions, please reply directly to this email.</p>
<p>Account ID: {account.AccountId}<br/>Time: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</p>
<p>Regards,<br/>ReTrade Support Team</p>";
                        }

                        await _emailService.SendEmailAsync(
                            user.Email,
                            "[ReTrade] Account Deactivated",
                            emailBody);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send self-deactivation email for account {AccountId}", accountId);
            }

            return true;
        }


        public async Task<IEnumerable<Account>> GetAllAsync() => await _repository.GetAllAsync();
        public async Task<Account> GetByIdAsync(object id) => await _repository.GetByIdAsync(id);
        public async Task AddAsync(Account item) => await _repository.AddAsync(item);
        public async Task UpdateAsync(Account item) => await _repository.UpdateAsync(item);
        public async Task DeleteAsync(object id)
        {
            await _repository.DeleteAsync(id);
        }

        public async Task RestoreAsync(object id)
        {
            await _repository.RestoreAsync(id);
        }

        public async Task<string> RegisterAsync(RegisterDto dto)
        {
            var existingUser = await _userRepository.GetByEmailAsync(dto.Email);
            if (existingUser != null) return "Email already exists.";

            var existingAccount = await _repository.GetByUsernameAsync(dto.Username);
            if (existingAccount != null) return "Username already exists.";

            string userId = await GenerateUserIdAsync();
            string accountId = await GenerateAccountIdAsync();

            // Táº¡o User
            var user = _mapper.Map<User>(dto);
            user.UserId = userId;
            await _userRepository.AddAsync(user);

            // Sinh mÃ£ OTP 6 sá»‘
            string otp = new Random().Next(100000, 999999).ToString();

            // LÆ°u OTP vÃ o MemoryCache vá»›i thá»i háº¡n 3 phÃºt
            _cache.Set(dto.Email, otp, TimeSpan.FromMinutes(3));

            // Táº¡o Account
            var account = _mapper.Map<Account>(dto);
            account.AccountId = accountId;
            account.UserId = userId;
            account.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
            account.IsPasswordSet = true;
            await _repository.AddAsync(account);

            // GÃ¡n quyá»n dá»±a trÃªn RoleId Ä‘Ã£ cÃ³ trong DB (1â€‘Admin, 2â€‘Buyer, 3â€‘Seller)
            string roleName = ((RetradeBE.Models.Enums.RoleEnum)dto.RoleId).ToString();
            await _repository.AssignRoleAsync(accountId, roleName);

            // Gá»­i OTP qua email
            string template = await GetEmailTemplateAsync("VerificationOtp.html");
            string emailBody = template.Replace("{{OTP}}", otp);
            await _emailService.SendEmailAsync(dto.Email, "ReTrade Account Verification", emailBody);

            return "Register success. Please check your email for OTP.";
        }

        public async Task<bool> VerifyAsync(VerifyDto dto)
        {
            // Kiá»ƒm tra OTP trong Cache
            if (!_cache.TryGetValue(dto.Email, out string? savedOtp) || savedOtp != dto.Otp)
            {
                return false;
            }

            var user = await _userRepository.GetByEmailAsync(dto.Email);
            if (user == null) return false;

            // Láº¥y Account liÃªn káº¿t
            var allAccounts = await _repository.GetAllAsync();
            var account = allAccounts.FirstOrDefault(a => a.UserId == user.UserId);

            if (account == null) return false;

            // Cáº­p nháº­t tráº¡ng thÃ¡i account
            account.Status = RetradeBE.Models.Enums.AccountStatusEnum.Active.ToString();
            await _repository.UpdateAsync(account);

            // XÃ³a OTP khá»i Cache
            _cache.Remove(dto.Email);

            return true;
        }

        public async Task<string> ResendOtpAsync(string email)
        {
            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null) return "User not found.";
            
            // Sinh mÃ£ OTP 6 sá»‘ má»›i
            string otp = new Random().Next(100000, 999999).ToString();

            // LÆ°u OTP vÃ o MemoryCache vá»›i thá»i háº¡n 3 phÃºt (Ä‘Ã¨ lÃªn mÃ£ cÅ© náº¿u cÃ³)
            _cache.Set(email, otp, TimeSpan.FromMinutes(3));

            // Gá»­i OTP qua email
            string template = await GetEmailTemplateAsync("VerificationOtp.html");
            string emailBody = template.Replace("{{OTP}}", otp);
            await _emailService.SendEmailAsync(email, "ReTrade Resend OTP", emailBody);

            return "Resend OTP success. Please check your email.";
        }

        public async Task<object?> LoginAsync(LoginDto dto)
        {
            Account? account = null;
            if (dto.Username.Contains("@"))
            {
                var user = await _userRepository.GetByEmailAsync(dto.Username);
                if (user != null)
                {
                    var allAccounts = await _repository.GetAllAsync();
                    account = allAccounts.FirstOrDefault(a => a.UserId == user.UserId);
                }
            }
            else
            {
                account = await _repository.GetByUsernameAsync(dto.Username);
            }

            if (account == null || account.IsDeleted == true) return null;
            if (account.Status == RetradeBE.Models.Enums.AccountStatusEnum.Inactive.ToString())
            {
                throw new InvalidOperationException("ACCOUNT_INACTIVE");
            }
            if (account.Status != RetradeBE.Models.Enums.AccountStatusEnum.Active.ToString()) return null;

            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(dto.Password, account.PasswordHash);
            if (!isPasswordValid) return null;

            var roles = await _repository.GetRolesAsync(account.AccountId);
            if (roles == null || !roles.Any())
            {
                roles = new List<string> { RetradeBE.Models.Enums.RoleEnum.Buyer.ToString() };
            }

            // 2FA removed: proceed to issue JWT immediately

            // Sinh JWT
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtSettings.SecretKey);
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, account.AccountId),
                new Claim(ClaimTypes.Name, account.Username!)
            };
            foreach (var r in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, r));
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes),
                Issuer = _jwtSettings.Issuer,
                Audience = _jwtSettings.Audience,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            
            // Update last login
            account.LastLoginAt = DateTime.UtcNow;
            await _repository.UpdateAsync(account);

            var userEntity = await _userRepository.GetByIdAsync(account.UserId!);

            return new AuthResponseDto
            {
                AccountId = account.AccountId,
                UserId = userEntity?.UserId ?? account.UserId ?? string.Empty,
                Username = account.Username!,
                Email = userEntity?.Email,
                FirstName = userEntity?.FirstName,
                LastName = userEntity?.LastName,
                Phone = userEntity?.Phone,
                AvatarUrl = userEntity?.AvatarUrl,
                PasswordHash = account.PasswordHash,
                Token = tokenHandler.WriteToken(token),
                Roles = roles,
                MustChangePassword = account.MustChangePassword ?? false,
                IsPasswordSet = account.IsPasswordSet ?? true,
            };
        }

        private Task<string> GenerateUserIdAsync()
        {
            return Task.FromResult(RetradeBE.Utils.IdGenerator.GenerateId("usr"));
        }

        private Task<string> GenerateAccountIdAsync()
        {
            return Task.FromResult(RetradeBE.Utils.IdGenerator.GenerateId("acc"));
        }

        public async Task<object?> LoginWithGoogleAsync(string accessToken)
        {
            // Gá»i Google UserInfo API Ä‘á»ƒ láº¥y thÃ´ng tin user
            using var httpClient = _httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            var response = await httpClient.GetAsync("https://www.googleapis.com/oauth2/v3/userinfo");
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
            string? email = json.TryGetProperty("email", out var emailProp) ? emailProp.GetString() : null;
            string? providerUserId = json.TryGetProperty("sub", out var subProp) ? subProp.GetString() : null;
            string? firstName = json.TryGetProperty("given_name", out var fnProp) ? fnProp.GetString() : null;
            string? lastName = json.TryGetProperty("family_name", out var lnProp) ? lnProp.GetString() : null;
            string? picture = json.TryGetProperty("picture", out var picProp) ? picProp.GetString() : null;

            if (string.IsNullOrEmpty(email)) return null;
            const string googleProvider = "Google";

            // TÃ¬m user theo email
            var user = await _userRepository.GetByEmailAsync(email);
            Account? account = null;

            if (user == null)
            {
                // Táº¡o má»›i User & Account náº¿u chÆ°a cÃ³
                string userId = await GenerateUserIdAsync();
                string accountId = await GenerateAccountIdAsync();

                // Táº¡o username tá»« email (pháº§n trÆ°á»›c @)
                string baseUsername = email.Split('@')[0].Replace(".", "").Replace("+", "");
                string username = baseUsername;
                int suffix = 1;
                while (await _repository.GetByUsernameAsync(username) != null)
                {
                    username = $"{baseUsername}{suffix++}";
                }

                user = new User
                {
                    UserId = userId,
                    Email = email,
                    FirstName = firstName ?? "",
                    LastName = lastName ?? "",
                    AvatarUrl = picture ?? "https://res.cloudinary.com/dx0hrokek/image/upload/v1780673207/avt-emty_wwnzba.jpg",
                    CreatedAt = DateTime.UtcNow
                };
                await _userRepository.AddAsync(user);

                account = new Account
                {
                    AccountId = accountId,
                    UserId = userId,
                    Provider = googleProvider,
                    Username = username,
                    ProviderUserId = providerUserId ?? email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString()), // random password
                    Status = RetradeBE.Models.Enums.AccountStatusEnum.Active.ToString(), // Google accounts bá» qua verify
                    IsPasswordSet = false,
                    CreatedAt = DateTime.UtcNow
                };
                await _repository.AddAsync(account);
                await _repository.AssignRoleAsync(accountId, RetradeBE.Models.Enums.RoleEnum.Buyer.ToString());
            }
            else
            {
                // TÃ¬m account theo userId
                var allAccounts = await _repository.GetAllAsync();
                account = allAccounts.FirstOrDefault(a => a.UserId == user.UserId);
                if (account == null || account.IsDeleted == true) return null;

                if (account.Status == RetradeBE.Models.Enums.AccountStatusEnum.Inactive.ToString())
                {
                    throw new InvalidOperationException("ACCOUNT_INACTIVE");
                }

                // Tá»± Ä‘á»™ng activate náº¿u Pending (Ä‘Äƒng nháº­p Google láº§n Ä‘áº§u sau khi register thÆ°á»ng)
                if (account.Status == RetradeBE.Models.Enums.AccountStatusEnum.Pending.ToString())
                {
                    account.Status = RetradeBE.Models.Enums.AccountStatusEnum.Active.ToString();
                }

                account.Provider = googleProvider;
                account.ProviderUserId = providerUserId ?? email;
                account.UpdatedAt = DateTime.UtcNow;
                await _repository.UpdateAsync(account);
            }

            // Sinh JWT
            var roles = await _repository.GetRolesAsync(account.AccountId);
            if (roles == null || !roles.Any())
                roles = new List<string> { RetradeBE.Models.Enums.RoleEnum.Buyer.ToString() };

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtSettings.SecretKey);
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, account.AccountId),
                new Claim(ClaimTypes.Name, account.Username!)
            };
            foreach (var r in roles)
                claims.Add(new Claim(ClaimTypes.Role, r));

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes),
                Issuer = _jwtSettings.Issuer,
                Audience = _jwtSettings.Audience,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);

            return new AuthResponseDto
            {
                AccountId = account.AccountId,
                UserId = user.UserId,
                Username = account.Username!,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Phone = user.Phone,
                AvatarUrl = user.AvatarUrl,
                PasswordHash = "",
                Token = tokenHandler.WriteToken(token),
                Roles = roles,
                IsPasswordSet = account.IsPasswordSet ?? true,
            };
        }

        public async Task<string> ForgotPasswordAsync(string email)
        {
            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null) return "Email not found.";

            // Check if there is a linked account
            var allAccounts = await _repository.GetAllAsync();
            var account = allAccounts.FirstOrDefault(a => a.UserId == user.UserId);
            if (account == null) return "No account associated with this email.";

            // Generate OTP
            string otp = new Random().Next(100000, 999999).ToString();

            // Save OTP to cache with prefix to avoid collision, 3 minutes expiration
            _cache.Set($"forgot_pwd_{email}", otp, TimeSpan.FromMinutes(3));

            // Send Email
            string template = await GetEmailTemplateAsync("ForgotPasswordOtp.html");
            string emailBody = template.Replace("{{OTP}}", otp);
            await _emailService.SendEmailAsync(email, "ReTrade Password Reset OTP", emailBody);

            return "Password reset OTP has been sent to your email.";
        }

        private string GenerateRandomPassword()
        {
            const string uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string lowercase = "abcdefghijklmnopqrstuvwxyz";
            const string digits = "0123456789";
            const string specialChars = "@$!%*?&";
            var random = new Random();
            
            var passwordChars = new List<char>
            {
                uppercase[random.Next(uppercase.Length)],
                digits[random.Next(digits.Length)],
                specialChars[random.Next(specialChars.Length)]
            };
            
            string allChars = uppercase + lowercase + digits + specialChars;
            for (int i = passwordChars.Count; i < 8; i++)
            {
                passwordChars.Add(allChars[random.Next(allChars.Length)]);
            }
            
            return new string(passwordChars.OrderBy(x => random.Next()).ToArray());
        }

        public async Task<string> PasswordRecoveryAsync(string email)
        {
            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null) return "No account is associated with this email address.";

            var allAccounts = await _repository.GetAllAsync();
            var account = allAccounts.FirstOrDefault(a => a.UserId == user.UserId);
            if (account == null) return "No account is associated with this email address.";

            // Generate Random Password
            string newPassword = GenerateRandomPassword();

            // Update Password and mark MustChangePassword
            account.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            account.MustChangePassword = true;
            account.UpdatedAt = DateTime.UtcNow;
            await _repository.UpdateAsync(account);

            // Send Email
            string template = await GetEmailTemplateAsync("ResetPasswordAuto.html");
            string emailBody = template.Replace("{{NEW_PASSWORD}}", newPassword);
            await _emailService.SendEmailAsync(email, "ReTrade Password Generated", emailBody);

            return "Password reset successful. Please check your email for your new password.";
        }

        public async Task<string> ResetPasswordAsync(ResetPasswordDto dto)
        {
            // Verify OTP
            if (!_cache.TryGetValue($"forgot_pwd_{dto.Email}", out string? savedOtp) || savedOtp != dto.Otp)
            {
                return "Invalid or expired OTP.";
            }

            var user = await _userRepository.GetByEmailAsync(dto.Email);
            if (user == null) return "User not found.";

            var allAccounts = await _repository.GetAllAsync();
            var account = allAccounts.FirstOrDefault(a => a.UserId == user.UserId);
            if (account == null) return "Account not found.";

            // Update Password
            account.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            await _repository.UpdateAsync(account);

            // Remove OTP from cache
            _cache.Remove($"forgot_pwd_{dto.Email}");

            // Send Success Email
            string template = await GetEmailTemplateAsync("ResetPasswordSuccess.html");
            string emailBody = template;
            await _emailService.SendEmailAsync(dto.Email, "ReTrade Password Reset Successful", emailBody);

            return "Password has been reset successfully.";
        }

        public async Task<string> ChangePasswordAsync(string accountId, ChangePasswordDto dto)
        {
            var account = await _repository.GetByIdAsync(accountId);
            if (account == null) return "Account not found.";

            if (!BCrypt.Net.BCrypt.Verify(dto.OldPassword, account.PasswordHash))
            {
                return "Old password is incorrect.";
            }

            account.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            account.IsPasswordSet = true;
            account.MustChangePassword = false;
            account.UpdatedAt = DateTime.UtcNow;
            await _repository.UpdateAsync(account);

            return "Password changed successfully.";
        }

        public async Task<string> SetPasswordAsync(string accountId, SetPasswordDto dto)
        {
            var account = await _repository.GetByIdAsync(accountId);
            if (account == null) return "Account not found.";

            account.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            account.IsPasswordSet = true;
            account.MustChangePassword = false;
            account.UpdatedAt = DateTime.UtcNow;
            await _repository.UpdateAsync(account);

            return "Password set successfully.";
        }



    }
}
