using RetradeBE.Models.DTOs;
using RetradeBE.Models;

namespace RetradeBE.Services
{
    public interface IOrderService
    {
        Task<Order?> GetByIdAsync(string orderId);
        Task<PagedResultDto<OrderListDto>> GetMyOrdersAsync(string userId, OrderSearchQueryDto query);
        Task<PagedResultDto<OrderListDto>> GetSellerOrdersAsync(string sellerId, OrderSearchQueryDto query);
        Task<SellerSalesStatisticsDto> GetSellerSalesStatisticsAsync(string sellerId, int periodDays);
        Task<PagedResultDto<OrderListDto>> GetAllOrdersAsync(OrderSearchQueryDto query);
        Task<OrderDetailDto?> GetOrderDetailAsync(string sellerId, string orderId);
        Task<OrderDetailDto?> ConfirmOrderAsync(string sellerId, string orderId);
        Task<OrderDetailDto?> UpdateStatusAsync(string sellerId, string orderId, OrderStatusUpdateDto dto);
        Task<OrderDetailDto?> ApproveReturnAsync(string sellerId, string orderId);
        Task<OrderDetailDto?> RejectReturnAsync(string sellerId, string orderId);
        Task<int> ProcessDueShippingOutcomesAsync(CancellationToken cancellationToken = default);
    }
}
