using RetradeBE.Models.DTOs;

namespace RetradeBE.Services
{
    public interface IAddressService
    {
        Task<List<AddressDto>> GetMyAddressesAsync(string accountId);
        Task<AddressDto?> CreateAsync(string accountId, AddressCreateDto dto);
        Task<AddressDto?> UpdateAsync(string accountId, string addressId, AddressUpdateDto dto);
        Task<bool> DeleteAsync(string accountId, string addressId);
        Task<AddressDto?> SetDefaultAsync(string accountId, string addressId);
    }
}
