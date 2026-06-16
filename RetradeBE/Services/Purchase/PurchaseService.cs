using AutoMapper;
using AutoMapper.QueryableExtensions;
using RetradeBE.Models.DTOs;
using RetradeBE.Repositories;

namespace RetradeBE.Services
{
    public class PurchaseService : IPurchaseService
    {
        private const string AwaitingPaymentStatus = "AwaitingPayment";
        private const string PendingStatus = "Pending";
        private const string ConfirmedStatus = "Confirmed";
        private const string CompletedStatus = "Completed";
        private const string CancelledStatus = "Cancelled";

        private readonly IOrderRepository _orderRepository;
        private readonly IMapper _mapper;

        public PurchaseService(
            IOrderRepository orderRepository,
            IMapper mapper)
        {
            _orderRepository = orderRepository;
            _mapper = mapper;
        }

        public IQueryable<PurchaseListDto> QueryByBuyerId(string buyerId)
        {
            if (string.IsNullOrWhiteSpace(buyerId))
            {
                return Enumerable.Empty<PurchaseListDto>().AsQueryable();
            }

            return _orderRepository.Query()
                .Where(o => o.UserId == buyerId)
                .ProjectTo<PurchaseListDto>(_mapper.ConfigurationProvider);
        }

        public async Task<PurchaseDetailDto?> GetByIdAsync(string buyerId, string orderId)
        {
            if (string.IsNullOrWhiteSpace(buyerId))
            {
                return null;
            }

            var order = await _orderRepository.GetByIdAsync(orderId);
            if (order == null || order.UserId != buyerId)
            {
                return null;
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
            if (order == null || order.UserId != buyerId)
            {
                return null;
            }

            if (!string.Equals(order.Status, PendingStatus, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Purchase can only be completed from Pending status.");
            }

            order.Status = CompletedStatus;
            order.UpdatedAt = DateTime.UtcNow;

            await _orderRepository.UpdateAsync(order);

            return _mapper.Map<PurchaseDetailDto>(order);
        }

        public async Task<PurchaseDetailDto?> CancelPurchaseAsync(string buyerId, string orderId)
        {
            if (string.IsNullOrWhiteSpace(buyerId))
            {
                return null;
            }

            var order = await _orderRepository.GetForUpdateAsync(orderId);
            if (order == null || order.UserId != buyerId)
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

            return _mapper.Map<PurchaseDetailDto>(order);
        }

        private static bool CanCancel(string? status)
        {
            return string.Equals(status, AwaitingPaymentStatus, StringComparison.OrdinalIgnoreCase)
                || string.Equals(status, PendingStatus, StringComparison.OrdinalIgnoreCase)
                || string.Equals(status, ConfirmedStatus, StringComparison.OrdinalIgnoreCase);
        }
    }
}
