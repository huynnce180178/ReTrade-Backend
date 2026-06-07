using System.ComponentModel.DataAnnotations;

namespace RetradeBE.Models.DTOs
{
    public class RegisterDto
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public int RoleId { get; set; } = 2;
    }

    public class VerifyDto
    {
        public string Email { get; set; } = string.Empty;
        public string Otp { get; set; } = null!;
    }

    public class ResendOtpDto
    {
        public string Email { get; set; } = null!;
    }

    public class LoginDto
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class AuthResponseDto
    {
        public string AccountId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Phone { get; set; }
        public string? AvatarUrl { get; set; }
        public string PasswordHash { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public List<string> Roles { get; set; } = new();
        public bool MustChangePassword { get; set; } = false;
        public bool IsPasswordSet { get; set; } = true;
    }

    public class GoogleLoginDto
    {
        public string AccessToken { get; set; } = string.Empty;
    }

    public class ForgotPasswordDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }

    public class ResetPasswordDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Otp { get; set; } = string.Empty;

        [Required]
        [RegularExpression(@"^(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$", 
            ErrorMessage = "Password must be at least 8 characters long, contain at least 1 uppercase letter, 1 number, and 1 special character.")]
        public string NewPassword { get; set; } = string.Empty;
    }

    public class ChangePasswordDto
    {
        public string OldPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }

    public class SetPasswordDto
    {
        [Required]
        [RegularExpression(@"^(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$", 
            ErrorMessage = "Password must be at least 8 characters long, contain at least 1 uppercase letter, 1 number, and 1 special character.")]
        public string NewPassword { get; set; } = string.Empty;
    }



    public class UpdateProfileDto
    {
        public string? Username { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
    }

    public class UserProfileDto
    {
        public string AccountId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Phone { get; set; }
        public string? AvatarUrl { get; set; }
        public string? Status { get; set; }
        public bool? IsDeleted { get; set; }
        public DateTime? CreatedAt { get; set; }
        public bool? IsPasswordSet { get; set; }
    }

    
}
