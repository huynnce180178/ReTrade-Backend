using RetradeBE.Models;
using RetradeBE.Models.DTOs;
using RetradeBE.Repositories;
using RetradeBE.Hubs;
using Microsoft.AspNetCore.SignalR;
using AutoMapper;

namespace RetradeBE.Services
{
    public class ProfileService : IProfileService
    {
        private readonly IProfileRepository _repository;
        private readonly IAccountRepository _accountRepository;
        private readonly IHubContext<SellerHub> _sellerHub;
        private readonly IMapper _mapper;

        public ProfileService(IProfileRepository repository, IAccountRepository accountRepository, IHubContext<SellerHub> sellerHub, IMapper mapper)
        {
            _repository = repository;
            _accountRepository = accountRepository;
            _sellerHub = sellerHub;
            _mapper = mapper;
        }

        public async Task<ProfileDetailDto?> GetMyProfileAsync(string accountId)
        {
            var account = await _repository.GetAccountWithUserAsync(accountId);
            if (account?.User == null) return null;

            var addresses = await _repository.GetActiveAddressesByUserIdAsync(account.User.UserId);
            var roles = await _accountRepository.GetRolesAsync(account.AccountId);

            var profileDto = _mapper.Map<ProfileDetailDto>(account);
            profileDto.Addresses = _mapper.Map<List<AddressDto>>(addresses);
            var defaultAddress = addresses.FirstOrDefault(a => a.IsDefault == true) ?? addresses.FirstOrDefault();
            profileDto.DefaultAddress = defaultAddress != null ? _mapper.Map<AddressDto>(defaultAddress) : null;
            profileDto.Roles = roles ?? new List<string>();

            return profileDto;
        }

        public async Task<ProfileDetailDto?> GetUserProfileAsync(string userId)
        {
            var user = await _repository.GetUserByIdAsync(userId);
            if (user == null || user.IsDeleted == true) return null;

            var account = await _repository.GetPrimaryAccountByUserIdAsync(userId);
            if (account == null) return null;

            var addresses = await _repository.GetActiveAddressesByUserIdAsync(user.UserId);
            var roles = await _accountRepository.GetRolesAsync(account.AccountId);

            var profileDto = _mapper.Map<ProfileDetailDto>(account);
            profileDto.Addresses = _mapper.Map<List<AddressDto>>(addresses);
            var defaultAddress = addresses.FirstOrDefault(a => a.IsDefault == true) ?? addresses.FirstOrDefault();
            profileDto.DefaultAddress = defaultAddress != null ? _mapper.Map<AddressDto>(defaultAddress) : null;
            profileDto.Roles = roles ?? new List<string>();

            return profileDto;
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
            var roles = await _accountRepository.GetRolesAsync(account.AccountId);

            var profileDto = _mapper.Map<ProfileDetailDto>(account);
            profileDto.Addresses = _mapper.Map<List<AddressDto>>(addresses);
            var defaultAddress = addresses.FirstOrDefault(a => a.IsDefault == true) ?? addresses.FirstOrDefault();
            profileDto.DefaultAddress = defaultAddress != null ? _mapper.Map<AddressDto>(defaultAddress) : null;
            profileDto.Roles = roles ?? new List<string>();

            return profileDto;
        }

        public async Task<SellerDetailDto?> GetSellerInformationAsync(string sellerId, string? currentAccountId = null)
        {
            sellerId = await ResolveUserIdAsync(sellerId) ?? sellerId;
            var seller = await _repository.GetUserByIdAsync(sellerId);
            if (seller == null || seller.IsDeleted == true) return null;

            var sellerAccount = await _repository.GetPrimaryAccountByUserIdAsync(sellerId);
            var addresses = await _repository.GetActiveAddressesByUserIdAsync(sellerId);
            var currentUserId = await GetUserIdByAccountIdAsync(currentAccountId);
            var sellerRoles = sellerAccount == null
                ? new List<string>()
                : await _accountRepository.GetRolesAsync(sellerAccount.AccountId);
            var currentRoles = string.IsNullOrWhiteSpace(currentAccountId)
                ? new List<string>()
                : await _accountRepository.GetRolesAsync(currentAccountId);
            var isOwnSeller = currentUserId == seller.UserId;
            var isSeller = HasRole(sellerRoles, "Seller");
            var currentIsAdmin = HasRole(currentRoles, "Admin");

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
                IsSeller = isSeller,
                IsFollowing = currentUserId != null && await _repository.FollowExistsAsync(currentUserId, seller.UserId),
                IsOwnSeller = isOwnSeller,
                CanFollow = currentUserId != null && isSeller && !currentIsAdmin && !isOwnSeller,
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
            await EnsureCanFollowSellerAsync(accountId, currentUserId, sellerId);

            if (!await _repository.FollowExistsAsync(currentUserId, sellerId))
            {
                var follow = new UserFollow
                {
                    FollowId = GenerateFollowId(),
                    FollowerId = currentUserId,
                    FollowedUserId = sellerId,
                    CreatedAt = DateTime.UtcNow
                };
                await _repository.AddFollowAsync(follow);
            }

            var result = new FollowResultDto
            {
                SellerId = sellerId,
                FollowerId = currentUserId,
                IsFollowing = true,
                FollowersCount = await _repository.CountFollowersAsync(sellerId),
                Message = "Follow seller successfully."
            };

            await PublishFollowChangedAsync(result);
            return result;
        }

        public async Task<FollowResultDto?> UnfollowSellerAsync(string accountId, string sellerId)
        {
            var currentUserId = await GetUserIdByAccountIdAsync(accountId);
            if (currentUserId == null) return null;

            sellerId = await ResolveUserIdAsync(sellerId) ?? sellerId;
            var seller = await _repository.GetUserByIdAsync(sellerId);
            if (seller == null || seller.IsDeleted == true) return null;
            await EnsureCanFollowSellerAsync(accountId, currentUserId, sellerId);

            var follow = await _repository.GetFollowAsync(currentUserId, sellerId);
            if (follow != null)
            {
                await _repository.RemoveFollowAsync(follow);
            }

            var result = new FollowResultDto
            {
                SellerId = sellerId,
                FollowerId = currentUserId,
                IsFollowing = false,
                FollowersCount = await _repository.CountFollowersAsync(sellerId),
                Message = "Unfollow seller successfully."
            };

            await PublishFollowChangedAsync(result);
            return result;
        }

        private async Task EnsureCanFollowSellerAsync(string accountId, string currentUserId, string sellerId)
        {
            if (currentUserId == sellerId)
            {
                throw new InvalidOperationException("You cannot follow yourself.");
            }

            var currentRoles = await _accountRepository.GetRolesAsync(accountId);
            if (HasRole(currentRoles, "Admin"))
            {
                throw new InvalidOperationException("Admins cannot follow users.");
            }

            var sellerAccount = await _repository.GetPrimaryAccountByUserIdAsync(sellerId);
            var sellerRoles = sellerAccount == null
                ? new List<string>()
                : await _accountRepository.GetRolesAsync(sellerAccount.AccountId);
            if (!HasRole(sellerRoles, "Seller"))
            {
                throw new InvalidOperationException("You can only follow sellers.");
            }
        }

        private static bool HasRole(IEnumerable<string> roles, string roleName)
        {
            return roles.Any(r => string.Equals(r, roleName, StringComparison.OrdinalIgnoreCase));
        }

        private Task PublishFollowChangedAsync(FollowResultDto result)
        {
            return _sellerHub
                .Clients
                .Group(SellerHub.GetSellerGroupName(result.SellerId))
                .SendAsync("SellerFollowChanged", result);
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
            if (dto.Street != null || dto.StreetAddress != null) address.Street = dto.Street ?? dto.StreetAddress;
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

        private static string GenerateFollowId() => $"UF{Guid.NewGuid():N}";

        private AddressDto? MapDefaultAddress(List<Address> addresses)
        {
            var address = addresses.FirstOrDefault(a => a.IsDefault == true) ?? addresses.FirstOrDefault();
            return address == null ? null : _mapper.Map<AddressDto>(address);
        }
    }
}
