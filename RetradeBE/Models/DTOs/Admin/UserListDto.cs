namespace RetradeBE.Models.DTOs.Admin
{
    public class UserListDto
    {
        public string AccountId { get; set; } = string.Empty;
        public string? UserId { get; set; }
        public string? Username { get; set; }
        public string? Provider { get; set; }
        public string? PrimaryRole { get; set; }
        public string? Status { get; set; }
        public bool? IsDeleted { get; set; }
        public bool? IsPasswordSet { get; set; }
        public bool? MustChangePassword { get; set; }
        public string? Email { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Phone { get; set; }
        public string? AvatarUrl { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}