using System.Threading.Tasks;
using RetradeBE.Models;
using RetradeBE.Models.DTOs;

namespace RetradeBE.Services.Checkout
{
    public interface ICheckoutService
    {
        Task<CalculateFeeResponseDto> CalculateShippingFeeAsync(CalculateFeeRequestDto request);
        Task<string> ProcessCheckoutAsync(CheckoutRequestDto request, string userId);
        Task<string> GetAddressSnapshotPublicAsync(Address address);
    }
}
