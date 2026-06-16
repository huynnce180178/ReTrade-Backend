using RetradeBE.Models.DTOs;

namespace RetradeBE.Services
{
    public interface IOrderService
    {
        Task<PagedResultDto<OrderListDto>> GetMyOrdersAsync(string accountId, OrderSearchQueryDto query);
        Task<PagedResultDto<OrderListDto>> GetSellerOrdersAsync(string accountId, OrderSearchQueryDto query);
        Task<PagedResultDto<OrderListDto>> GetAllOrdersAsync(OrderSearchQueryDto query);
        Task<OrderDetailDto?> GetOrderDetailAsync(string accountId, string orderId);
    }
}
