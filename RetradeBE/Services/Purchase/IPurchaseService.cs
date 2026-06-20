using RetradeBE.Models.DTOs;

namespace RetradeBE.Services
{
    public interface IPurchaseService
    {
        IQueryable<PurchaseListDto> QueryByBuyerId(string buyerId, string? status = null);
        Task<PurchaseDetailDto?> GetByIdAsync(string buyerId, string orderId);
        Task<PurchaseDetailDto?> CompletePurchaseAsync(string buyerId, string orderId);
        Task<PurchaseDetailDto?> CancelPurchaseAsync(string buyerId, string orderId);
    }
}
