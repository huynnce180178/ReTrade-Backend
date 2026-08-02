using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.AspNetCore.SignalR;
using RetradeBE.Hubs;
using RetradeBE.Models;
using RetradeBE.Models.DTOs;
using RetradeBE.Repositories;

namespace RetradeBE.Services
{
    public class PurchaseService : IPurchaseService
    {
        private const string AwaitingPaymentStatus = "AwaitingPayment";
        private const string PendingStatus = "Pending";
        private const string ConfirmedStatus = "Confirmed";
        private const string DeliveredStatus = "Delivered";
        private const string CompletedStatus = "Completed";
        private const string CancelledStatus = "Cancelled";
        private const string ReturnRequestedStatus = "ReturnRequested";
        private static readonly TimeSpan ReturnRequestWindow = TimeSpan.FromDays(7);

        private readonly IOrderRepository _orderRepository;
        private readonly IMapper _mapper;
        private readonly IHubContext<OrderHub> _orderHub;

        public PurchaseService(
            IOrderRepository orderRepository,
            IMapper mapper,
            IHubContext<OrderHub> orderHub)
        {
            _orderRepository = orderRepository;
            _mapper = mapper;
            _orderHub = orderHub;
        }

        public IQueryable<PurchaseListDto> QueryByBuyerId(string buyerId, string? status = null)
        {
            if (string.IsNullOrWhiteSpace(buyerId))
            {
                return Enumerable.Empty<PurchaseListDto>().AsQueryable();
            }

            var q = _orderRepository.Query()
                .Where(o => o.BuyerId == buyerId);

            if (!string.IsNullOrWhiteSpace(status))
            {
                q = q.Where(o => o.Status == status);
            }

            // Always order by CreatedAt descending (newest first)
            q = q.OrderByDescending(o => o.CreatedAt);

            return q.ProjectTo<PurchaseListDto>(_mapper.ConfigurationProvider);
        }

        public async Task<PurchaseDetailDto?> GetByIdAsync(string buyerId, string orderId)
        {
            if (string.IsNullOrWhiteSpace(buyerId))
            {
                throw new ArgumentException("Buyer ID is required.", nameof(buyerId));
            }

            if (string.IsNullOrWhiteSpace(orderId))
            {
                throw new ArgumentException("Order ID is required.", nameof(orderId));
            }

            var order = await _orderRepository.GetByIdAsync(orderId);
            if (order == null)
            {
                throw new KeyNotFoundException("Purchase order not found.");
            }

            if (order.BuyerId != buyerId)
            {
                throw new UnauthorizedAccessException("You do not have permission to view this order.");
            }

            return _mapper.Map<PurchaseDetailDto>(order);
        }

        public async Task<PurchaseDetailDto?> CompletePurchaseAsync(string buyerId, string orderId)
        {
            if (string.IsNullOrWhiteSpace(buyerId))
            {
                return null;
            }

            var order = await _orderRepository.GetForUpdateAsync(orderId);
            if (order == null || order.BuyerId != buyerId)
            {
                return null;
            }

            if (!string.Equals(order.Status, DeliveredStatus , StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Purchase can only be completed from Delivered status.");
            }

            order.Status = CompletedStatus;
            order.UpdatedAt = DateTime.UtcNow;

            await _orderRepository.UpdateAsync(order);
            await NotifyOrderStatusChangedAsync(order);

            return _mapper.Map<PurchaseDetailDto>(order);
        }

        public async Task<PurchaseDetailDto?> CancelPurchaseAsync(string buyerId, string orderId)
        {
            if (string.IsNullOrWhiteSpace(buyerId))
            {
                return null;
            }

            var order = await _orderRepository.GetForUpdateAsync(orderId);
            if (order == null || order.BuyerId != buyerId)
            {
                return null;
            }

            if (!CanCancel(order.Status))
            {
                throw new InvalidOperationException("Purchase can only be cancelled from AwaitingPayment, Pending, or Confirmed status.");
            }

            order.Status = CancelledStatus;
            order.UpdatedAt = DateTime.UtcNow;

            await _orderRepository.UpdateAsync(order);
            await NotifyOrderStatusChangedAsync(order);

            return _mapper.Map<PurchaseDetailDto>(order);
        }

        public async Task<PurchaseDetailDto?> RequestReturnAsync(string buyerId, string orderId, ReturnPurchaseRequestDto dto)
        {
            if (string.IsNullOrWhiteSpace(buyerId))
            {
                return null;
            }

            var reason = dto?.Reason?.Trim();
            if (string.IsNullOrWhiteSpace(reason))
            {
                throw new InvalidOperationException("Return reason is required.");
            }

            var order = await _orderRepository.GetForUpdateAsync(orderId);
            if (order == null || order.BuyerId != buyerId)
            {
                return null;
            }

            if (!string.Equals(order.Status, CompletedStatus, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Purchase can only request return from Completed status.");
            }

            if (!IsWithinReturnRequestWindow(order))
            {
                throw new InvalidOperationException("Purchase can only request return within 7 days after receiving the order.");
            }

            order.Status = ReturnRequestedStatus;
            order.ReturnReason = reason;
            order.UpdatedAt = DateTime.UtcNow;

            await _orderRepository.UpdateAsync(order);
            await NotifyOrderStatusChangedAsync(order);

            return _mapper.Map<PurchaseDetailDto>(order);
        }

        private async Task NotifyOrderStatusChangedAsync(Order order)
        {
            var payload = new
            {
                order.OrderId,
                order.OrderCode,
                SellerId = order.SellerId,
                BuyerId = order.BuyerId,
                order.Status,
                order.TrackingCode,
                order.ShippingProvider,
                order.ExpectedDeliveryTime,
                order.ReturnReason,
                order.UpdatedAt
            };

            if (!string.IsNullOrWhiteSpace(order.SellerId))
            {
                await _orderHub.Clients
                    .Group(OrderHub.GetSellerOrderGroupName(order.SellerId))
                    .SendAsync("SellerOrderStatusChanged", payload);
            }

            if (!string.IsNullOrWhiteSpace(order.BuyerId))
            {
                await _orderHub.Clients
                    .Group(OrderHub.GetBuyerOrderGroupName(order.BuyerId))
                    .SendAsync("BuyerOrderStatusChanged", payload);
            }
        }

        private static bool CanCancel(string? status)
        {
            return string.Equals(status, AwaitingPaymentStatus, StringComparison.OrdinalIgnoreCase)
                || string.Equals(status, PendingStatus, StringComparison.OrdinalIgnoreCase)
                || string.Equals(status, ConfirmedStatus, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsWithinReturnRequestWindow(Order order)
        {
            var receivedAt = NormalizeUtc(order.UpdatedAt ?? order.CreatedAt);
            if (!receivedAt.HasValue)
            {
                return false;
            }

            return DateTime.UtcNow - receivedAt.Value <= ReturnRequestWindow;
        }

        private static DateTime? NormalizeUtc(DateTime? value)
        {
            if (!value.HasValue)
            {
                return null;
            }

            return value.Value.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)
                : value.Value.ToUniversalTime();
        }
    }
}
