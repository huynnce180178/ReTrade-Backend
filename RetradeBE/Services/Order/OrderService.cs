using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using RetradeBE.Hubs;
using RetradeBE.Models;
using RetradeBE.Models.DTOs;
using RetradeBE.Models.Enums;
using RetradeBE.Repositories;

namespace RetradeBE.Services
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IHubContext<OrderHub> _orderHub;

        public OrderService(
            IOrderRepository orderRepository,
            IHubContext<OrderHub> orderHub)
        {
            _orderRepository = orderRepository;
            _orderHub = orderHub;
        }

        public async Task<PagedResultDto<OrderListDto>> GetMyOrdersAsync(string userId, OrderSearchQueryDto query)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return EmptyPagedResult(query);
            }

            var orders = ApplyFilters(_orderRepository.Query().Where(o => o.UserId == userId), query);
            return await ToPagedListAsync(orders, query);
        }

        public async Task<PagedResultDto<OrderListDto>> GetSellerOrdersAsync(string sellerId, OrderSearchQueryDto query)
        {
            if (string.IsNullOrWhiteSpace(sellerId))
            {
                return EmptyPagedResult(query);
            }

            var orders = ApplyFilters(_orderRepository.Query().Where(o => o.SellerId == sellerId), query);
            return await ToPagedListAsync(orders, query);
        }

        public async Task<PagedResultDto<OrderListDto>> GetAllOrdersAsync(OrderSearchQueryDto query)
        {
            var orders = ApplyFilters(_orderRepository.Query(), query);
            return await ToPagedListAsync(orders, query);
        }

        public async Task<OrderDetailDto?> GetOrderDetailAsync(string sellerId, string orderId)
        {
            var order = await _orderRepository.GetByIdAsync(orderId);
            if (order == null)
            {
                return null;
            }

            if (order.SellerId != sellerId)
            {
                return null;
            }

            return ToDetailDto(order);
        }

        public async Task<OrderDetailDto?> ConfirmOrderAsync(string sellerId, string orderId)
        {
            return await UpdateStatusAsync(sellerId, orderId, new OrderStatusUpdateDto
            {
                Status = nameof(OrderStatusEnum.Confirmed)
            });
        }

        public async Task<OrderDetailDto?> UpdateStatusAsync(string sellerId, string orderId, OrderStatusUpdateDto dto)
        {
            var order = await _orderRepository.GetForUpdateAsync(orderId);
            if (order == null)
            {
                return null;
            }

            if (order.SellerId != sellerId)
            {
                return null;
            }

            if (!Enum.TryParse<OrderStatusEnum>(dto.Status, true, out var nextStatus))
            {
                throw new InvalidOperationException("Invalid order status.");
            }

            var currentStatus = ParseStatus(order.Status);
            if (!CanMoveTo(currentStatus, nextStatus))
            {
                throw new InvalidOperationException($"Cannot update order status from {currentStatus} to {nextStatus}.");
            }

            order.Status = nextStatus.ToString();
            order.UpdatedAt = DateTime.UtcNow;

            if (!string.IsNullOrWhiteSpace(dto.TrackingCode))
            {
                order.TrackingCode = dto.TrackingCode.Trim();
            }

            if (!string.IsNullOrWhiteSpace(dto.ShippingProvider))
            {
                order.ShippingProvider = dto.ShippingProvider.Trim();
            }

            if (dto.ExpectedDeliveryTime.HasValue)
            {
                order.ExpectedDeliveryTime = dto.ExpectedDeliveryTime.Value;
            }

            await _orderRepository.UpdateAsync(order);
            var updatedOrder = ToDetailDto(order);
            await NotifySellerOrderStatusChangedAsync(updatedOrder);

            return updatedOrder;
        }

        private static IQueryable<Order> ApplyFilters(IQueryable<Order> orders, OrderSearchQueryDto query)
        {
            query.Page = query.Page < 1 ? 1 : query.Page;
            query.PageSize = query.PageSize < 1 ? 12 : Math.Min(query.PageSize, 100);

            if (!string.IsNullOrWhiteSpace(query.Status))
            {
                orders = orders.Where(o => o.Status == query.Status);
            }

            if (query.FromDate.HasValue)
            {
                orders = orders.Where(o => o.CreatedAt >= query.FromDate.Value);
            }

            if (query.ToDate.HasValue)
            {
                orders = orders.Where(o => o.CreatedAt <= query.ToDate.Value);
            }

            if (!string.IsNullOrWhiteSpace(query.SearchTerm))
            {
                var search = query.SearchTerm.Trim().ToLower();
                orders = orders.Where(o =>
                    (o.OrderCode != null && o.OrderCode.ToLower().Contains(search)) ||
                    (o.TrackingCode != null && o.TrackingCode.ToLower().Contains(search)) ||
                    (o.Product != null && o.Product.Name != null && o.Product.Name.ToLower().Contains(search)));
            }

            return orders;
        }

        private static async Task<PagedResultDto<OrderListDto>> ToPagedListAsync(IQueryable<Order> orders, OrderSearchQueryDto query)
        {
            var totalItems = await orders.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalItems / query.PageSize);
            if (totalPages == 0) totalPages = 1;

            var items = await orders
                .OrderByDescending(o => o.CreatedAt)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(o => new OrderListDto
                {
                    OrderId = o.OrderId,
                    OrderCode = o.OrderCode,
                    ProductId = o.ProductId,
                    ProductName = o.Product != null ? o.Product.Name : null,
                    ProductImageUrl = o.Product != null
                        ? o.Product.ProductImage
                            .Where(pi => pi.IsMain == true)
                            .Select(pi => pi.Image.ImageUrl)
                            .FirstOrDefault()
                          ?? o.Product.ProductImage
                            .OrderBy(pi => pi.SortOrder)
                            .Select(pi => pi.Image.ImageUrl)
                            .FirstOrDefault()
                        : null,
                    BuyerId = o.UserId,
                    BuyerName = o.User != null ? (o.User.FirstName + " " + o.User.LastName).Trim() : null,
                    BuyerEmail = o.User != null ? o.User.Email : null,
                    BuyerPhone = o.User != null ? o.User.Phone : null,
                    SellerId = o.SellerId,
                    SellerName = o.Seller != null ? (o.Seller.FirstName + " " + o.Seller.LastName).Trim() : null,
                    SellerEmail = o.Seller != null ? o.Seller.Email : null,
                    SellerPhone = o.Seller != null ? o.Seller.Phone : null,
                    Quantity = o.Quantity,
                    UnitPrice = o.UnitPrice,
                    TotalAmount = o.TotalAmount,
                    ShippingFee = o.ShippingFee,
                    DiscountAmount = o.DiscountAmount,
                    FinalAmount = o.FinalAmount,
                    Status = o.Status,
                    TrackingCode = o.TrackingCode,
                    ShippingProvider = o.ShippingProvider,
                    ExpectedDeliveryTime = o.ExpectedDeliveryTime,
                    CreatedAt = o.CreatedAt,
                    UpdatedAt = o.UpdatedAt
                })
                .ToListAsync();

            return new PagedResultDto<OrderListDto>
            {
                Items = items,
                TotalItems = totalItems,
                Page = query.Page,
                PageSize = query.PageSize,
                TotalPages = totalPages
            };
        }

        private static OrderDetailDto ToDetailDto(Order order)
        {
            var productImageUrl = order.Product?.ProductImage
                .Where(pi => pi.IsMain == true)
                .Select(pi => pi.Image.ImageUrl)
                .FirstOrDefault()
                ?? order.Product?.ProductImage
                    .OrderBy(pi => pi.SortOrder)
                    .Select(pi => pi.Image.ImageUrl)
                    .FirstOrDefault();

            return new OrderDetailDto
            {
                OrderId = order.OrderId,
                OrderCode = order.OrderCode,
                ProductId = order.ProductId,
                ProductName = order.Product?.Name,
                ProductImageUrl = productImageUrl,
                BuyerId = order.UserId,
                BuyerName = order.User != null ? $"{order.User.FirstName} {order.User.LastName}".Trim() : null,
                BuyerEmail = order.User?.Email,
                BuyerPhone = order.User?.Phone,
                SellerId = order.SellerId,
                SellerName = order.Seller != null ? $"{order.Seller.FirstName} {order.Seller.LastName}".Trim() : null,
                SellerEmail = order.Seller?.Email,
                SellerPhone = order.Seller?.Phone,
                Quantity = order.Quantity,
                UnitPrice = order.UnitPrice,
                TotalAmount = order.TotalAmount,
                ShippingFee = order.ShippingFee,
                DiscountAmount = order.DiscountAmount,
                FinalAmount = order.FinalAmount,
                Status = order.Status,
                TrackingCode = order.TrackingCode,
                ShippingProvider = order.ShippingProvider,
                ExpectedDeliveryTime = order.ExpectedDeliveryTime,
                CreatedAt = order.CreatedAt,
                UpdatedAt = order.UpdatedAt,
                AddressSnapshot = order.AddressSnapshot,
                VoucherId = order.VoucherId,
                AuctionId = order.AuctionId,
                OfferId = order.OfferId,
                Payments = order.Payment
                    .OrderByDescending(p => p.CreatedAt)
                    .Select(p => new PaymentSummaryDto
                    {
                        PaymentId = p.PaymentId,
                        Amount = p.Amount,
                        PaymentMethod = p.PaymentMethod,
                        ProviderTransactionId = p.ProviderTransactionId,
                        Status = p.Status,
                        CreatedAt = p.CreatedAt,
                        UpdatedAt = p.UpdatedAt
                    })
                    .ToList()
            };
        }

        private static OrderStatusEnum ParseStatus(string? status)
        {
            return Enum.TryParse<OrderStatusEnum>(status, true, out var parsed)
                ? parsed
                : OrderStatusEnum.Pending;
        }

        private static bool CanMoveTo(OrderStatusEnum currentStatus, OrderStatusEnum nextStatus)
        {
            if (currentStatus == nextStatus)
            {
                return true;
            }

            if (currentStatus is OrderStatusEnum.Delivered or OrderStatusEnum.Returned or OrderStatusEnum.Cancelled)
            {
                return false;
            }

            return currentStatus switch
            {
                OrderStatusEnum.AwaitingPayment => nextStatus is OrderStatusEnum.Cancelled,
                OrderStatusEnum.Pending => nextStatus is OrderStatusEnum.Confirmed or OrderStatusEnum.Cancelled,
                OrderStatusEnum.Confirmed => nextStatus is OrderStatusEnum.Shipping or OrderStatusEnum.Cancelled,
                OrderStatusEnum.Shipping => nextStatus is OrderStatusEnum.Delivered or OrderStatusEnum.Returned,
                _ => false
            };
        }

        private async Task NotifySellerOrderStatusChangedAsync(OrderDetailDto order)
        {
            if (string.IsNullOrWhiteSpace(order.SellerId))
            {
                return;
            }

            await _orderHub.Clients
                .Group(OrderHub.GetSellerOrderGroupName(order.SellerId))
                .SendAsync("SellerOrderStatusChanged", new
                {
                    order.OrderId,
                    order.OrderCode,
                    order.SellerId,
                    order.Status,
                    order.TrackingCode,
                    order.ShippingProvider,
                    order.ExpectedDeliveryTime,
                    order.UpdatedAt
                });
        }

        private static PagedResultDto<OrderListDto> EmptyPagedResult(OrderSearchQueryDto query)
        {
            return new PagedResultDto<OrderListDto>
            {
                Items = new List<OrderListDto>(),
                TotalItems = 0,
                Page = query.Page < 1 ? 1 : query.Page,
                PageSize = query.PageSize < 1 ? 12 : Math.Min(query.PageSize, 100),
                TotalPages = 1
            };
        }

    }
}
