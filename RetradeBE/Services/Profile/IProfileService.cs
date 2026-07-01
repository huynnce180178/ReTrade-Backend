using RetradeBE.Models.DTOs;

namespace RetradeBE.Services
{
    public interface IProfileService
    {
        Task<ProfileDetailDto?> GetMyProfileAsync(string accountId);
        Task<ProfileDetailDto?> GetUserProfileAsync(string userId);
        Task<ProfileDetailDto?> UpdateMyProfileAsync(string accountId, ProfileUpdateDto dto);
        Task<SellerDetailDto?> GetSellerInformationAsync(string sellerId, string? currentAccountId = null);
        Task<FollowResultDto?> FollowSellerAsync(string accountId, string sellerId);
        Task<FollowResultDto?> UnfollowSellerAsync(string accountId, string sellerId);
        Task<IQueryable<MyVoucherDto>> GetMyVouchersQueryAsync(string accountId);
        Task<MyVoucherDto?> GetMyVoucherDetailAsync(string accountId, string userVoucherId);
    }
}
