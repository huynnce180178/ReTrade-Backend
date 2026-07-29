using RetradeBE.Models.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RetradeBE.Services.Refund
{
    public interface IRefundService
    {
        Task<IEnumerable<AdminRefundResponseDto>> GetAllRefundsAsync();
        Task<(bool Success, string Message)> ApproveRefundAsync(string id);
        Task<(bool Success, string Message)> RejectRefundAsync(string id, RejectRefundRequestDto dto);
    }
}
