using RetradeBE.Models;
using RetradeBE.Models.DTOs;
using RetradeBE.Repositories;

namespace RetradeBE.Services
{
    public class ProfileService : IProfileService
    {
        private readonly IProfileRepository _repository;

        public ProfileService(IProfileRepository repository)
        {
            _repository = repository;
        }

        public async Task<ProfileDetailDto?> GetMyProfileAsync(string accountId)
        {
            var account = await _repository.GetAccountWithUserAsync(accountId);
            if (account?.User == null) return null;

            var addresses = await _repository.GetActiveAddressesByUserIdAsync(account.User.UserId);
            return MapProfile(account, account.User, addresses);
        }

        public async Task<ProfileDetailDto?> GetUserProfileAsync(string userId)
        {
            var user = await _repository.GetUserByIdAsync(userId);
            if (user == null || user.IsDeleted == true) return null;

            var account = await _repository.GetPrimaryAccountByUserIdAsync(userId);
            if (account == null) return null;

            var addresses = await _repository.GetActiveAddressesByUserIdAsync(user.UserId);
            return MapProfile(account, user, addresses);
        }

        public async Task<ProfileDetailDto?> UpdateMyProfileAsync(string accountId, ProfileUpdateDto dto)
        {
            var account = await _repository.GetAccountWithUserAsync(accountId);
            if (account?.User == null) return null;

            if (!string.IsNullOrWhiteSpace(dto.Username))
            {
                var username = dto.Username.Trim();
                if (await _repository.UsernameExistsAsync(username, accountId))
                {
                    throw new InvalidOperationException("Username already exists.");
                }
                account.Username = username;
                account.UpdatedAt = DateTime.UtcNow;
                await _repository.UpdateAccountAsync(account);
            }

            if (!string.IsNullOrWhiteSpace(dto.Email))
            {
                var email = dto.Email.Trim();
                if (await _repository.EmailExistsAsync(email, account.User.UserId))
                {
                    throw new InvalidOperationException("Email already exists.");
                }
                account.User.Email = email;
            }

            if (dto.FirstName != null) account.User.FirstName = dto.FirstName;
            if (dto.LastName != null) account.User.LastName = dto.LastName;
            if (dto.Phone != null) account.User.Phone = dto.Phone;
            account.User.UpdatedAt = DateTime.UtcNow;
            await _repository.UpdateUserAsync(account.User);

            if (dto.Address != null)
            {
                await UpsertAddressAsync(account.User.UserId, dto.Address);
            }

            var addresses = await _repository.GetActiveAddressesByUserIdAsync(account.User.UserId);
            return MapProfile(account, account.User, addresses);
        }

        public async Task<SellerDetailDto?> GetSellerInformationAsync(string sellerId, string? currentAccountId = null)
        {
            sellerId = await ResolveUserIdAsync(sellerId) ?? sellerId;
            var seller = await _repository.GetUserByIdAsync(sellerId);
            if (seller == null || seller.IsDeleted == true) return null;

            var sellerAccount = await _repository.GetPrimaryAccountByUserIdAsync(sellerId);
            var addresses = await _repository.GetActiveAddressesByUserIdAsync(sellerId);
            var currentUserId = await GetUserIdByAccountIdAsync(currentAccountId);

            return new SellerDetailDto
            {
                SellerId = seller.UserId,
                AccountId = sellerAccount?.AccountId,
                Username = sellerAccount?.Username,
                FirstName = seller.FirstName,
                LastName = seller.LastName,
                Email = seller.Email,
                Phone = seller.Phone,
                AvatarUrl = seller.AvatarUrl,
                CreatedAt = seller.CreatedAt,
                FollowersCount = await _repository.CountFollowersAsync(seller.UserId),
                FollowingCount = await _repository.CountFollowingAsync(seller.UserId),
                ProductCount = await _repository.CountProductsAsync(seller.UserId),
                AverageRating = await _repository.GetAverageSellerRatingAsync(seller.UserId),
                IsFollowing = currentUserId != null && await _repository.FollowExistsAsync(currentUserId, seller.UserId),
                DefaultAddress = MapDefaultAddress(addresses)
            };
        }

        public async Task<FollowResultDto?> FollowSellerAsync(string accountId, string sellerId)
        {
            var currentUserId = await GetUserIdByAccountIdAsync(accountId);
            if (currentUserId == null) return null;

            sellerId = await ResolveUserIdAsync(sellerId) ?? sellerId;
            var seller = await _repository.GetUserByIdAsync(sellerId);
            if (seller == null || seller.IsDeleted == true) return null;
            if (currentUserId == sellerId)
            {
                throw new InvalidOperationException("You cannot follow yourself.");
            }

            if (!await _repository.FollowExistsAsync(currentUserId, sellerId))
            {
                var follow = new UserFollow
                {
                    FollowId = await GenerateFollowIdAsync(),
                    FollowerId = currentUserId,
                    FollowedUserId = sellerId,
                    CreatedAt = DateTime.UtcNow
                };
                await _repository.AddFollowAsync(follow);
            }

            return new FollowResultDto
            {
                SellerId = sellerId,
                IsFollowing = true,
                FollowersCount = await _repository.CountFollowersAsync(sellerId),
                Message = "Follow seller successfully."
            };
        }

        public async Task<FollowResultDto?> UnfollowSellerAsync(string accountId, string sellerId)
        {
            var currentUserId = await GetUserIdByAccountIdAsync(accountId);
            if (currentUserId == null) return null;

            sellerId = await ResolveUserIdAsync(sellerId) ?? sellerId;
            var seller = await _repository.GetUserByIdAsync(sellerId);
            if (seller == null || seller.IsDeleted == true) return null;

            var follow = await _repository.GetFollowAsync(currentUserId, sellerId);
            if (follow != null)
            {
                await _repository.RemoveFollowAsync(follow);
            }

            return new FollowResultDto
            {
                SellerId = sellerId,
                IsFollowing = false,
                FollowersCount = await _repository.CountFollowersAsync(sellerId),
                Message = "Unfollow seller successfully."
            };
        }

        private async Task<string?> GetUserIdByAccountIdAsync(string? accountId)
        {
            if (string.IsNullOrWhiteSpace(accountId)) return null;

            var account = await _repository.GetAccountWithUserAsync(accountId);
            return account?.UserId;
        }

        private async Task<string?> ResolveUserIdAsync(string id)
        {
            var user = await _repository.GetUserByIdAsync(id);
            if (user != null) return user.UserId;

            var account = await _repository.GetAccountWithUserAsync(id);
            return account?.UserId;
        }

        private async Task UpsertAddressAsync(string userId, UpsertAddressDto dto)
        {
            Address? address = null;
            if (!string.IsNullOrWhiteSpace(dto.AddressId))
            {
                address = await _repository.GetAddressByIdAsync(dto.AddressId);
                if (address != null && address.UserId != userId)
                {
                    throw new InvalidOperationException("Address does not belong to this user.");
                }
            }

            var isNewAddress = address == null;
            address ??= new Address
            {
                AddressId = await GenerateAddressIdAsync(),
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            if (dto.ReceiverName != null) address.ReceiverName = dto.ReceiverName;
            if (dto.ReceiverPhone != null) address.ReceiverPhone = dto.ReceiverPhone;
            if (dto.Street != null) address.Street = dto.Street;
            if (dto.ProvinceId.HasValue) address.ProvinceId = dto.ProvinceId;
            if (dto.DistrictId.HasValue) address.DistrictId = dto.DistrictId;
            if (dto.WardCode != null) address.WardCode = dto.WardCode;
            address.IsDefault = dto.IsDefault ?? address.IsDefault ?? true;
            address.Status = dto.Status ?? address.Status ?? "Active";
            address.UpdatedAt = DateTime.UtcNow;

            if (isNewAddress)
            {
                await _repository.AddAddressAsync(address);
            }
            else
            {
                await _repository.UpdateAddressAsync(address);
            }
        }

        private async Task<string> GenerateAddressIdAsync()
        {
            var count = await _repository.CountAddressesAsync();
            return $"ADDR{count + 1}";
        }

        private async Task<string> GenerateFollowIdAsync()
        {
            var count = await _repository.CountFollowsAsync();
            return $"UF{count + 1}";
        }

        private static ProfileDetailDto MapProfile(Account account, User user, List<Address> addresses)
        {
            return new ProfileDetailDto
            {
                AccountId = account.AccountId,
                UserId = user.UserId,
                Username = account.Username ?? string.Empty,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Phone = user.Phone,
                AvatarUrl = user.AvatarUrl,
                Status = account.Status,
                IsDeleted = user.IsDeleted,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt,
                DefaultAddress = MapDefaultAddress(addresses),
                Addresses = addresses.Select(MapAddress).ToList()
            };
        }

        private static AddressDto? MapDefaultAddress(List<Address> addresses)
        {
            var address = addresses.FirstOrDefault(a => a.IsDefault == true) ?? addresses.FirstOrDefault();
            return address == null ? null : MapAddress(address);
        }

        private static AddressDto MapAddress(Address address)
        {
            return new AddressDto
            {
                AddressId = address.AddressId,
                ReceiverName = address.ReceiverName,
                ReceiverPhone = address.ReceiverPhone,
                Street = address.Street,
                ProvinceId = address.ProvinceId,
                DistrictId = address.DistrictId,
                WardCode = address.WardCode,
                IsDefault = address.IsDefault,
                Status = address.Status
            };
        }
    }
}
