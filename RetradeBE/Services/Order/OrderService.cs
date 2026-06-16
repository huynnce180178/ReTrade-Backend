using Microsoft.EntityFrameworkCore;
using RetradeBE.Models;
using RetradeBE.Models.DTOs;
using RetradeBE.Repositories;

namespace RetradeBE.Services
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IAccountRepository _accountRepository;

        public OrderService(IOrderRepository orderRepository, IAccountRepository accountRepository)
        {
            _orderRepository = orderRepository;
            _accountRepository = accountRepository;
        }

        public async Task<PagedResultDto<OrderListDto>> GetMyOrdersAsync(string accountId, OrderSearchQueryDto query)
        {
            var userId = await GetUserIdByAccountIdAsync(accountId);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return EmptyPagedResult(query);
            }

            var orders = ApplyFilters(_orderRepository.Query().Where(o => o.UserId == userId), query);
            return await ToPagedListAsync(orders, query);
        }

        public async Task<PagedResultDto<OrderListDto>> GetSellerOrdersAsync(string accountId, OrderSearchQueryDto query)
        {
            var userId = await GetUserIdByAccountIdAsync(accountId);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return EmptyPagedResult(query);
            }

            var orders = ApplyFilters(_orderRepository.Query().Where(o => o.SellerId == userId), query);
            return await ToPagedListAsync(orders, query);
        }

        public async Task<PagedResultDto<OrderListDto>> GetAllOrdersAsync(OrderSearchQueryDto query)
        {
            var orders = ApplyFilters(_orderRepository.Query(), query);
            return await ToPagedListAsync(orders, query);
        }

        public async Task<OrderDetailDto?> GetOrderDetailAsync(string accountId, string orderId)
        {
            var order = await _orderRepository.GetByIdAsync(orderId);
            if (order == null)
            {
                return null;
            }

            var account = await _accountRepository.GetByIdAsync(accountId);
            var roles = await _accountRepository.GetRolesAsync(accountId);
            var isAdmin = roles.Any(r => string.Equals(r, "Admin", StringComparison.OrdinalIgnoreCase));
            var currentUserId = account?.UserId;

            if (!isAdmin && order.UserId != currentUserId && order.SellerId != currentUserId)
            {
                return null;
            }

            return ToDetailDto(order);
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

        private async Task<string?> GetUserIdByAccountIdAsync(string accountId)
        {
            var account = await _accountRepository.GetByIdAsync(accountId);
            return account?.UserId;
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
