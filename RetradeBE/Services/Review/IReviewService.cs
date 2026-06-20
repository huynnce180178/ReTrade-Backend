using RetradeBE.Models.DTOs;

namespace RetradeBE.Services
{
    public interface IReviewService
    {
        Task<ReviewResponseDto?> GetByBuyerOrderAsync(string buyerId, string orderId);
        Task<ReviewResponseDto?> CreateAsync(string buyerId, ReviewCreateDto request);
    }
}
