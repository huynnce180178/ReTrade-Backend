using System.Threading.Tasks;
using RetradeBE.Models.DTOs;

namespace RetradeBE.Services.Ghn
{
    public interface IGhnService
    {
        Task<GhnCalculateFeeResponse> CalculateFeeAsync(GhnCalculateFeeRequest request);
        Task<object> GetProvincesAsync();
        Task<object> GetDistrictsAsync(int provinceId);
        Task<object> GetWardsAsync(int districtId);
    }
}
