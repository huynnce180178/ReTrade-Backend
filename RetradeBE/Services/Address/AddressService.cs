using RetradeBE.Models;
using RetradeBE.Models.DTOs;
using RetradeBE.Repositories;

namespace RetradeBE.Services
{
    public class AddressService : IAddressService
    {
        private readonly IAddressRepository _repository;

        public AddressService(IAddressRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<AddressDto>> GetMyAddressesAsync(string accountId)
        {
            var userId = await _repository.GetUserIdByAccountIdAsync(accountId);
            if (userId == null) return new List<AddressDto>();

            var addresses = await _repository.GetActiveByUserIdAsync(userId);

            return addresses.Select(MapAddress).ToList();
        }

        public async Task<AddressDto?> CreateAsync(string accountId, AddressCreateDto dto)
        {
            var userId = await _repository.GetUserIdByAccountIdAsync(accountId);
            if (userId == null) return null;

            var hasActiveAddress = await _repository.HasActiveAddressAsync(userId);
            var shouldBeDefault = dto.IsDefault == true || !hasActiveAddress;

            if (shouldBeDefault)
            {
                await ClearDefaultAddressesAsync(userId);
            }

            var address = new Address
            {
                AddressId = await GenerateAddressIdAsync(),
                UserId = userId,
                ReceiverName = dto.ReceiverName.Trim(),
                ReceiverPhone = dto.ReceiverPhone.Trim(),
                Street = dto.StreetAddress.Trim(),
                ProvinceId = dto.ProvinceId,
                DistrictId = dto.DistrictId,
                WardCode = dto.WardCode.Trim(),
                IsDefault = shouldBeDefault,
                Status = "Active",
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _repository.AddAsync(address);
            return MapAddress(address);
        }

        public async Task<AddressDto?> UpdateAsync(string accountId, string addressId, AddressUpdateDto dto)
        {
            var userId = await _repository.GetUserIdByAccountIdAsync(accountId);
            if (userId == null) return null;

            var address = await _repository.GetOwnedActiveAsync(userId, addressId);
            if (address == null) return null;

            if (dto.IsDefault == true)
            {
                await ClearDefaultAddressesAsync(userId);
            }

            address.ReceiverName = dto.ReceiverName.Trim();
            address.ReceiverPhone = dto.ReceiverPhone.Trim();
            address.Street = dto.StreetAddress.Trim();
            address.ProvinceId = dto.ProvinceId;
            address.DistrictId = dto.DistrictId;
            address.WardCode = dto.WardCode.Trim();
            address.IsDefault = dto.IsDefault ?? address.IsDefault;
            address.Status = string.IsNullOrWhiteSpace(dto.Status) ? address.Status ?? "Active" : dto.Status.Trim();
            address.UpdatedAt = DateTime.UtcNow;

            await _repository.SaveChangesAsync();
            return MapAddress(address);
        }

        public async Task<bool> DeleteAsync(string accountId, string addressId)
        {
            var userId = await _repository.GetUserIdByAccountIdAsync(accountId);
            if (userId == null) return false;

            var address = await _repository.GetOwnedActiveAsync(userId, addressId);
            if (address == null) return false;

            var wasDefault = address.IsDefault == true;
            address.IsDeleted = true;
            address.IsDefault = false;
            address.UpdatedAt = DateTime.UtcNow;
            await _repository.SaveChangesAsync();

            if (wasDefault)
            {
                var nextDefault = (await _repository.GetActiveByUserIdAsync(userId))
                    .OrderByDescending(a => a.CreatedAt)
                    .FirstOrDefault();

                if (nextDefault != null)
                {
                    nextDefault.IsDefault = true;
                    nextDefault.UpdatedAt = DateTime.UtcNow;
                    await _repository.SaveChangesAsync();
                }
            }

            return true;
        }

        public async Task<AddressDto?> SetDefaultAsync(string accountId, string addressId)
        {
            var userId = await _repository.GetUserIdByAccountIdAsync(accountId);
            if (userId == null) return null;

            var address = await _repository.GetOwnedActiveAsync(userId, addressId);
            if (address == null) return null;

            await ClearDefaultAddressesAsync(userId);
            address.IsDefault = true;
            address.UpdatedAt = DateTime.UtcNow;
            await _repository.SaveChangesAsync();
            return MapAddress(address);
        }

        private async Task ClearDefaultAddressesAsync(string userId)
        {
            var defaults = (await _repository.GetActiveByUserIdAsync(userId))
                .Where(a => a.IsDefault == true);

            foreach (var address in defaults)
            {
                address.IsDefault = false;
                address.UpdatedAt = DateTime.UtcNow;
            }
        }

        private Task<string> GenerateAddressIdAsync()
        {
            return Task.FromResult(RetradeBE.Utils.IdGenerator.GenerateId("adr"));
        }

        private static AddressDto MapAddress(Address address)
        {
            return new AddressDto
            {
                AddressId = address.AddressId,
                ReceiverName = address.ReceiverName,
                ReceiverPhone = address.ReceiverPhone,
                Street = address.Street,
                StreetAddress = address.Street,
                ProvinceId = address.ProvinceId,
                DistrictId = address.DistrictId,
                WardCode = address.WardCode,
                IsDefault = address.IsDefault,
                Status = address.Status
            };
        }
    }
}
