using System.ComponentModel.DataAnnotations;

namespace RetradeBE.Models.DTOs
{
    public class AddressDto
    {
        public string AddressId { get; set; } = string.Empty;
        public string? ReceiverName { get; set; }
        public string? ReceiverPhone { get; set; }
        public string? Street { get; set; }
        public int? ProvinceId { get; set; }
        public int? DistrictId { get; set; }
        public string? WardCode { get; set; }
        public bool? IsDefault { get; set; }
        public string? Status { get; set; }
    }

    public class UpsertAddressDto
    {
        public string? AddressId { get; set; }
        public string? ReceiverName { get; set; }
        public string? ReceiverPhone { get; set; }
        public string? Street { get; set; }
        public int? ProvinceId { get; set; }
        public int? DistrictId { get; set; }
        public string? WardCode { get; set; }
        public bool? IsDefault { get; set; }
        public string? Status { get; set; }
    }

    public class ProfileUpdateDto
    {
        public string? Username { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }

        [EmailAddress]
        public string? Email { get; set; }

        public string? Phone { get; set; }
        public UpsertAddressDto? Address { get; set; }
    }

    public class ProfileDetailDto
    {
        public string AccountId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Phone { get; set; }
        public string? AvatarUrl { get; set; }
        public string? Status { get; set; }
        public bool? IsDeleted { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public AddressDto? DefaultAddress { get; set; }
        public List<AddressDto> Addresses { get; set; } = new();
    }

    public class SellerDetailDto
    {
        public string SellerId { get; set; } = string.Empty;
        public string? AccountId { get; set; }
        public string? Username { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? AvatarUrl { get; set; }
        public DateTime? CreatedAt { get; set; }
        public int FollowersCount { get; set; }
        public int FollowingCount { get; set; }
        public int ProductCount { get; set; }
        public double? AverageRating { get; set; }
        public bool IsFollowing { get; set; }
        public AddressDto? DefaultAddress { get; set; }
    }

    public class FollowResultDto
    {
        public string SellerId { get; set; } = string.Empty;
        public bool IsFollowing { get; set; }
        public int FollowersCount { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
