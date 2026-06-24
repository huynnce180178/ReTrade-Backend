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
        private static readonly TimeSpan AwaitingPaymentCancelDelay = TimeSpan.FromMinutes(15);
        private static readonly TimeSpan ShippingOutcomeDelay = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan BusinessTimeZoneOffset = TimeSpan.FromHours(7);
        private const double ShippingFailureRate = 0.10;

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

        public async Task<SellerSalesStatisticsDto> GetSellerSalesStatisticsAsync(string sellerId, int periodDays)
        {
            var normalizedPeriodDays = Math.Clamp(periodDays, 7, 365);
            var todayLocal = ToBusinessLocal(DateTime.UtcNow).Date;
            var periodStartLocal = todayLocal.AddDays(-(normalizedPeriodDays - 1));
            var periodEndExclusiveLocal = todayLocal.AddDays(1);
            var periodEndLocal = periodEndExclusiveLocal.AddTicks(-1);
            var periodStartUtc = FromBusinessLocal(periodStartLocal);
            var periodEndExclusiveUtc = FromBusinessLocal(periodEndExclusiveLocal);

            if (string.IsNullOrWhiteSpace(sellerId))
            {
                return new SellerSalesStatisticsDto
                {
                    PeriodDays = normalizedPeriodDays,
                    PeriodStart = periodStartLocal,
                    PeriodEnd = periodEndLocal
                };
            }

            var orders = _orderRepository.Query()
                .Where(o => o.SellerId == sellerId
                    && o.CreatedAt.HasValue
                    && o.CreatedAt.Value >= periodStartUtc
                    && o.CreatedAt.Value < periodEndExclusiveUtc);

            var successfulOrders = orders.Where(o =>
                o.Status == nameof(OrderStatusEnum.Delivered)
                || o.Status == nameof(OrderStatusEnum.Completed));
            var successfulTrendSource = await successfulOrders
                .Select(o => new
                {
                    CreatedAt = o.CreatedAt!.Value,
                    Revenue = o.FinalAmount ?? 0
                })
                .ToListAsync();

            return new SellerSalesStatisticsDto
            {
                PeriodDays = normalizedPeriodDays,
                PeriodStart = periodStartLocal,
                PeriodEnd = periodEndLocal,
                TotalOrders = await orders.CountAsync(),
                AwaitingPaymentOrders = await orders.CountAsync(o => o.Status == nameof(OrderStatusEnum.AwaitingPayment)),
                PendingOrders = await orders.CountAsync(o => o.Status == nameof(OrderStatusEnum.Pending)),
                ConfirmedOrders = await orders.CountAsync(o => o.Status == nameof(OrderStatusEnum.Confirmed)),
                ShippingOrders = await orders.CountAsync(o => o.Status == nameof(OrderStatusEnum.Shipping)),
                DeliveredOrders = await orders.CountAsync(o => o.Status == nameof(OrderStatusEnum.Delivered)),
                CompletedOrders = await orders.CountAsync(o => o.Status == nameof(OrderStatusEnum.Completed)),
                ReturnRequestedOrders = await orders.CountAsync(o => o.Status == nameof(OrderStatusEnum.ReturnRequested)),
                ReturnRejectedOrders = await orders.CountAsync(o => o.Status == nameof(OrderStatusEnum.ReturnRejected)),
                DeliveryFailedOrders = await orders.CountAsync(o => o.Status == nameof(OrderStatusEnum.DeliveryFailed)),
                ReturnedOrders = await orders.CountAsync(o => o.Status == nameof(OrderStatusEnum.Returned)),
                CancelledOrders = await orders.CountAsync(o => o.Status == nameof(OrderStatusEnum.Cancelled)),
                SoldItems = await successfulOrders
                    .SumAsync(o => o.Quantity ?? 0),
                GrossSales = await successfulOrders
                    .SumAsync(o => o.TotalAmount ?? 0),
                ShippingCollected = await successfulOrders
                    .SumAsync(o => o.ShippingFee ?? 0),
                DiscountGiven = await successfulOrders
                    .SumAsync(o => o.DiscountAmount ?? 0),
                NetSales = await successfulOrders
                    .SumAsync(o => o.FinalAmount ?? 0),
                RevenueTrend = BuildRevenueTrend(periodStartLocal, periodEndExclusiveLocal, successfulTrendSource
                    .Select(o => (ToBusinessLocal(o.CreatedAt), o.Revenue))
                    .ToList())
            };
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
            if (!CanMoveTo(order, currentStatus, nextStatus))
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

            if (nextStatus == OrderStatusEnum.Shipping)
            {
                order.ExpectedDeliveryTime = DateTime.UtcNow.Add(ShippingOutcomeDelay);
            }

            await _orderRepository.UpdateAsync(order);
            var updatedOrder = ToDetailDto(order);
            await NotifySellerOrderStatusChangedAsync(updatedOrder);

            return updatedOrder;
        }

        public async Task<OrderDetailDto?> ApproveReturnAsync(string sellerId, string orderId)
        {
            var order = await GetSellerReturnOrderForUpdateAsync(sellerId, orderId);
            if (order == null)
            {
                return null;
            }

            order.Status = nameof(OrderStatusEnum.Returned);
            order.UpdatedAt = DateTime.UtcNow;

            await _orderRepository.UpdateAsync(order);
            var updatedOrder = ToDetailDto(order);
            await NotifySellerOrderStatusChangedAsync(updatedOrder);

            return updatedOrder;
        }

        public async Task<OrderDetailDto?> RejectReturnAsync(string sellerId, string orderId)
        {
            var order = await GetSellerReturnOrderForUpdateAsync(sellerId, orderId);
            if (order == null)
            {
                return null;
            }

            order.Status = nameof(OrderStatusEnum.ReturnRejected);
            order.UpdatedAt = DateTime.UtcNow;

            await _orderRepository.UpdateAsync(order);
            var updatedOrder = ToDetailDto(order);
            await NotifySellerOrderStatusChangedAsync(updatedOrder);

            return updatedOrder;
        }

        public async Task<int> ProcessDueShippingOutcomesAsync(CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            var dueUpdatedAt = now.Subtract(ShippingOutcomeDelay);
            var dueOrderIds = await _orderRepository.Query()
                .Where(o => o.Status == nameof(OrderStatusEnum.Shipping)
                    && ((o.UpdatedAt.HasValue && o.UpdatedAt.Value <= dueUpdatedAt)
                        || (!o.UpdatedAt.HasValue && o.CreatedAt.HasValue && o.CreatedAt.Value <= dueUpdatedAt)
                        || (o.ExpectedDeliveryTime.HasValue && o.ExpectedDeliveryTime.Value <= now)))
                .Select(o => o.OrderId)
                .ToListAsync(cancellationToken);

            var processedCount = 0;
            foreach (var orderId in dueOrderIds)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var order = await _orderRepository.GetForUpdateAsync(orderId);
                if (order == null || ParseStatus(order.Status) != OrderStatusEnum.Shipping)
                {
                    continue;
                }

                var carrierSucceeded = !string.IsNullOrWhiteSpace(order.AuctionId)
                    || Random.Shared.NextDouble() >= ShippingFailureRate;
                order.Status = carrierSucceeded
                    ? nameof(OrderStatusEnum.Delivered)
                    : nameof(OrderStatusEnum.DeliveryFailed);
                order.UpdatedAt = now;

                await _orderRepository.UpdateAsync(order);
                await NotifySellerOrderStatusChangedAsync(ToDetailDto(order));
                processedCount++;
            }

            return processedCount;
        }

        private static List<SellerSalesTrendPointDto> BuildRevenueTrend(
            DateTime periodStartLocal,
            DateTime periodEndExclusiveLocal,
            List<(DateTime CreatedAt, decimal Revenue)> deliveredOrders)
        {
            var totalDays = Math.Max(1, (int)(periodEndExclusiveLocal.Date - periodStartLocal.Date).TotalDays);
            var bucketCount = Math.Min(7, totalDays);
            var baseBucketDays = totalDays / bucketCount;
            var extraDays = totalDays % bucketCount;
            var trend = new List<SellerSalesTrendPointDto>();
            var fromDate = periodStartLocal.Date;

            for (var index = 0; index < bucketCount; index++)
            {
                var bucketDays = baseBucketDays + (index < extraDays ? 1 : 0);
                var toDateExclusive = fromDate.AddDays(bucketDays);
                var toDateInclusive = toDateExclusive.AddTicks(-1);

                var bucketOrders = deliveredOrders
                    .Where(o => o.CreatedAt >= fromDate
                        && o.CreatedAt < toDateExclusive)
                    .ToList();

                trend.Add(new SellerSalesTrendPointDto
                {
                    Label = bucketDays == 1
                        ? fromDate.ToString("dd/MM")
                        : $"{fromDate:dd/MM}-{toDateInclusive:dd/MM}",
                    FromDate = fromDate,
                    ToDate = toDateInclusive,
                    OrderCount = bucketOrders.Count,
                    Revenue = bucketOrders.Sum(o => o.Revenue)
                });

                fromDate = toDateExclusive;
            }

            return trend;
        }

        private static DateTime ToBusinessLocal(DateTime dateTime)
        {
            var utc = dateTime.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(dateTime, DateTimeKind.Utc)
                : dateTime.ToUniversalTime();

            return DateTime.SpecifyKind(utc.Add(BusinessTimeZoneOffset), DateTimeKind.Unspecified);
        }

        private static DateTime FromBusinessLocal(DateTime localDateTime)
        {
            return DateTime.SpecifyKind(localDateTime.Subtract(BusinessTimeZoneOffset), DateTimeKind.Utc);
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
                var toDate = query.ToDate.Value;
                var exclusiveToDate = toDate.TimeOfDay == TimeSpan.Zero
                    ? toDate.Date.AddDays(1)
                    : toDate;
                orders = toDate.TimeOfDay == TimeSpan.Zero
                    ? orders.Where(o => o.CreatedAt < exclusiveToDate)
                    : orders.Where(o => o.CreatedAt <= exclusiveToDate);
            }

            if (query.MinTotal.HasValue)
            {
                orders = orders.Where(o => o.FinalAmount >= query.MinTotal.Value);
            }

            if (!string.IsNullOrWhiteSpace(query.SearchTerm))
            {
                var search = query.SearchTerm.Trim().ToLower();
                orders = orders.Where(o =>
                    (o.OrderCode != null && o.OrderCode.ToLower().Contains(search)) ||
                    (o.TrackingCode != null && o.TrackingCode.ToLower().Contains(search)) ||
                    (o.Product != null && o.Product.Name != null && o.Product.Name.ToLower().Contains(search)) ||
                    (o.User != null && o.User.Email != null && o.User.Email.ToLower().Contains(search)) ||
                    (o.User != null && o.User.Phone != null && o.User.Phone.ToLower().Contains(search)) ||
                    (o.User != null && (((o.User.FirstName ?? "") + " " + (o.User.LastName ?? "")).Trim()).ToLower().Contains(search)));
            }

            return orders;
        }

        private static async Task<PagedResultDto<OrderListDto>> ToPagedListAsync(IQueryable<Order> orders, OrderSearchQueryDto query)
        {
            var totalItems = await orders.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalItems / query.PageSize);
            if (totalPages == 0) totalPages = 1;

            var items = await ProjectToOrderListDto(OrderByDynamic(orders, query.SortBy))
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
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

        private static IQueryable<Order> OrderByDynamic(IQueryable<Order> orders, string? sortBy)
        {
            return sortBy?.Trim().ToLowerInvariant() switch
            {
                "createdat asc" or "oldest" => orders.OrderBy(o => o.CreatedAt),
                "finalamount desc" or "total_desc" => orders.OrderByDescending(o => o.FinalAmount),
                "finalamount asc" or "total_asc" => orders.OrderBy(o => o.FinalAmount),
                _ => orders.OrderByDescending(o => o.CreatedAt),
            };
        }

        private static IQueryable<OrderListDto> ProjectToOrderListDto(IQueryable<Order> orders)
        {
            return orders.Select(o => new OrderListDto
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
                ReturnReason = o.ReturnReason,
                CreatedAt = o.CreatedAt,
                UpdatedAt = o.UpdatedAt
            });
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
                BuyerPhone = ResolveBuyerPhone(order),
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
                ReturnReason = order.ReturnReason,
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

        private static string? ResolveBuyerPhone(Order order)
        {
            if (!string.IsNullOrWhiteSpace(order.User?.Phone))
            {
                return order.User.Phone;
            }

            return ExtractReceiverPhone(order.AddressSnapshot);
        }

        private static string? ExtractReceiverPhone(string? addressSnapshot)
        {
            if (string.IsNullOrWhiteSpace(addressSnapshot))
            {
                return null;
            }

            return addressSnapshot
                .Split(" - ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .FirstOrDefault(part => part.Length is >= 9 and <= 12 && part.All(char.IsDigit));
        }

        private static bool CanMoveTo(Order order, OrderStatusEnum currentStatus, OrderStatusEnum nextStatus)
        {
            if (currentStatus == nextStatus)
            {
                return true;
            }

            if (currentStatus is OrderStatusEnum.DeliveryFailed
                or OrderStatusEnum.ReturnRequested
                or OrderStatusEnum.ReturnRejected
                or OrderStatusEnum.Returned
                or OrderStatusEnum.Cancelled)
            {
                return false;
            }

            return currentStatus switch
            {
                OrderStatusEnum.AwaitingPayment => nextStatus is OrderStatusEnum.Cancelled && IsAwaitingPaymentExpired(order),
                OrderStatusEnum.Pending => nextStatus is OrderStatusEnum.Confirmed or OrderStatusEnum.Cancelled,
                OrderStatusEnum.Confirmed => nextStatus is OrderStatusEnum.Shipping or OrderStatusEnum.Cancelled,
                OrderStatusEnum.Shipping => false,
                _ => false
            };
        }

        private async Task<Order?> GetSellerReturnOrderForUpdateAsync(string sellerId, string orderId)
        {
            if (string.IsNullOrWhiteSpace(sellerId))
            {
                return null;
            }

            var order = await _orderRepository.GetForUpdateAsync(orderId);
            if (order == null || order.SellerId != sellerId)
            {
                return null;
            }

            if (ParseStatus(order.Status) != OrderStatusEnum.ReturnRequested)
            {
                throw new InvalidOperationException("Return can only be reviewed from ReturnRequested status.");
            }

            return order;
        }

        private static bool IsAwaitingPaymentExpired(Order order)
        {
            if (!order.CreatedAt.HasValue)
            {
                return false;
            }

            var createdAt = order.CreatedAt.Value.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(order.CreatedAt.Value, DateTimeKind.Utc)
                : order.CreatedAt.Value.ToUniversalTime();

            return DateTime.UtcNow - createdAt >= AwaitingPaymentCancelDelay;
        }

        private async Task NotifySellerOrderStatusChangedAsync(OrderDetailDto order)
        {
            var payload = new
            {
                order.OrderId,
                order.OrderCode,
                order.SellerId,
                order.BuyerId,
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
