using RetradeBE.Models.DTOs;

namespace RetradeBE.Services
{
    public interface IOrderService
    {
        Task<PagedResultDto<OrderListDto>> GetMyOrdersAsync(string userId, OrderSearchQueryDto query);
        Task<PagedResultDto<OrderListDto>> GetSellerOrdersAsync(string sellerId, OrderSearchQueryDto query);
        IQueryable<OrderListDto> QuerySellerOrders(string sellerId);
        Task<SellerSalesStatisticsDto> GetSellerSalesStatisticsAsync(string sellerId, int periodDays);
        Task<PagedResultDto<OrderListDto>> GetAllOrdersAsync(OrderSearchQueryDto query);
        Task<OrderDetailDto?> GetOrderDetailAsync(string sellerId, string orderId);
        Task<OrderDetailDto?> ConfirmOrderAsync(string sellerId, string orderId);
        Task<OrderDetailDto?> UpdateStatusAsync(string sellerId, string orderId, OrderStatusUpdateDto dto);
    }
}
