using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using RetradeBE.Data;
using RetradeBE.Hubs;
using RetradeBE.Models;
using RetradeBE.Models.DTOs;
using RetradeBE.Services.Checkout;

namespace RetradeBE.Services.Offer
{
    public class OfferService : IOfferService
    {
        private readonly AppDbContext _context;
        private readonly ICheckoutService _checkoutService;
        private readonly IHubContext<OrderHub> _orderHub;

        public OfferService(AppDbContext context, ICheckoutService checkoutService, IHubContext<OrderHub> orderHub)
        {
            _context = context;
            _checkoutService = checkoutService;
            _orderHub = orderHub;
        }

        public async Task<OfferDto> MakeOfferAsync(string accountId, MakeOfferRequestDto request)
        {
            var account = await _context.Account.FirstOrDefaultAsync(a => a.AccountId == accountId);
            if (account == null || string.IsNullOrEmpty(account.UserId))
                throw new Exception("Account not found or not linked to a user.");

            var buyerUserId = account.UserId;

            var product = await _context.Product
                .Include(p => p.ProductImage)
                    .ThenInclude(pi => pi.Image)
                .FirstOrDefaultAsync(p => p.ProductId == request.ProductId);
            if (product == null) throw new Exception("Product not found.");
            if (product.Status != "Accepted") throw new Exception("Product is not available for offers.");
            if (product.SellerId == buyerUserId) throw new Exception("You cannot make an offer on your own product.");
            if (request.OfferPrice <= 0) throw new Exception("Offer price must be greater than 0.");
            if (product.Price.HasValue && request.OfferPrice >= product.Price.Value)
                throw new Exception($"Your offer must be lower than the listed price ({product.Price.Value:N0} VND). Offers are for bargaining only.");

            // Check if buyer already has a Pending offer for this product
            var existingOffer = await _context.Offer.FirstOrDefaultAsync(o =>
                o.BuyerId == buyerUserId && o.ProductId == request.ProductId && o.Status == "Pending");
            if (existingOffer != null)
                throw new Exception("You already have a pending offer for this product. Cancel it before making a new one.");

            var now = DateTime.UtcNow;
            var offer = new RetradeBE.Models.Offer
            {
                OfferId = Guid.NewGuid().ToString(),
                BuyerId = buyerUserId,
                ProductId = request.ProductId,
                OfferPrice = request.OfferPrice,
                Message = request.Message,
                ExpiresAt = now.AddHours(request.ExpiresInHours <= 0 ? 48 : request.ExpiresInHours),
                Status = "Pending",
                CreatedAt = now
            };

            _context.Offer.Add(offer);
            await _context.SaveChangesAsync();

            var buyer = await _context.User.FirstOrDefaultAsync(u => u.UserId == buyerUserId);
            var mainImage = product.ProductImage
                .OrderBy(pi => pi.SortOrder)
                .Select(pi => pi.Image?.ImageUrl)
                .FirstOrDefault();

            return MapToDto(offer, buyer, product, mainImage);
        }

        public async Task<List<OfferDto>> GetMyOffersAsync(string accountId, string? productId = null)
        {
            var account = await _context.Account.FirstOrDefaultAsync(a => a.AccountId == accountId);
            if (account == null || string.IsNullOrEmpty(account.UserId))
                return new List<OfferDto>();

            var buyerUserId = account.UserId;

            var query = _context.Offer
                .Include(o => o.Buyer)
                .Include(o => o.Product)
                    .ThenInclude(p => p.ProductImage)
                        .ThenInclude(pi => pi.Image)
                .Where(o => o.BuyerId == buyerUserId);

            if (!string.IsNullOrEmpty(productId))
                query = query.Where(o => o.ProductId == productId);

            var offers = await query.OrderByDescending(o => o.CreatedAt).ToListAsync();

            return offers.Select(o =>
            {
                var mainImage = o.Product?.ProductImage
                    .OrderBy(pi => pi.SortOrder)
                    .Select(pi => pi.Image?.ImageUrl)
                    .FirstOrDefault();
                return MapToDto(o, o.Buyer, o.Product, mainImage);
            }).ToList();
        }

        public async Task<List<OfferDto>> GetOffersForProductAsync(string sellerId, string productId)
        {
            var product = await _context.Product.FirstOrDefaultAsync(p => p.ProductId == productId);
            if (product == null || product.SellerId != sellerId)
                throw new Exception("Product not found or you are not the seller.");

            var offers = await _context.Offer
                .Include(o => o.Buyer)
                .Include(o => o.Product)
                    .ThenInclude(p => p.ProductImage)
                        .ThenInclude(pi => pi.Image)
                .Where(o => o.ProductId == productId)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            return offers.Select(o =>
            {
                var mainImage = o.Product?.ProductImage
                    .OrderBy(pi => pi.SortOrder)
                    .Select(pi => pi.Image?.ImageUrl)
                    .FirstOrDefault();
                return MapToDto(o, o.Buyer, o.Product, mainImage);
            }).ToList();
        }

        public async Task<OfferDto> AcceptOfferAsync(string sellerId, string offerId)
        {
            var offer = await _context.Offer
                .Include(o => o.Product)
                    .ThenInclude(p => p!.ProductImage)
                        .ThenInclude(pi => pi.Image)
                .Include(o => o.Buyer)
                .FirstOrDefaultAsync(o => o.OfferId == offerId);

            if (offer == null) throw new Exception("Offer not found.");
            if (offer.Product?.SellerId != sellerId) throw new Exception("You are not authorized to manage this offer.");
            if (offer.Status != "Pending") throw new Exception("Only pending offers can be accepted.");

            offer.Status = "Accepted";
            await _context.SaveChangesAsync();

            var mainImage = offer.Product?.ProductImage
                .OrderBy(pi => pi.SortOrder)
                .Select(pi => pi.Image?.ImageUrl)
                .FirstOrDefault();

            return MapToDto(offer, offer.Buyer, offer.Product, mainImage);
        }

        public async Task<OfferDto> RejectOfferAsync(string sellerId, string offerId)
        {
            var offer = await _context.Offer
                .Include(o => o.Product)
                    .ThenInclude(p => p!.ProductImage)
                        .ThenInclude(pi => pi.Image)
                .Include(o => o.Buyer)
                .FirstOrDefaultAsync(o => o.OfferId == offerId);

            if (offer == null) throw new Exception("Offer not found.");
            if (offer.Product?.SellerId != sellerId) throw new Exception("You are not authorized to manage this offer.");
            if (offer.Status != "Pending") throw new Exception("Only pending offers can be rejected.");

            offer.Status = "Rejected";
            await _context.SaveChangesAsync();

            var mainImage = offer.Product?.ProductImage
                .OrderBy(pi => pi.SortOrder)
                .Select(pi => pi.Image?.ImageUrl)
                .FirstOrDefault();

            return MapToDto(offer, offer.Buyer, offer.Product, mainImage);
        }

        public async Task<OfferDto> CancelOfferAsync(string buyerUserId, string offerId)
        {
            var offer = await _context.Offer
                .Include(o => o.Product)
                    .ThenInclude(p => p!.ProductImage)
                        .ThenInclude(pi => pi.Image)
                .Include(o => o.Buyer)
                .FirstOrDefaultAsync(o => o.OfferId == offerId);

            if (offer == null) throw new Exception("Offer not found.");
            if (offer.BuyerId != buyerUserId) throw new Exception("You are not authorized to cancel this offer.");
            if (offer.Status != "Pending") throw new Exception("Only pending offers can be cancelled.");

            offer.Status = "Cancelled";
            await _context.SaveChangesAsync();

            var mainImage = offer.Product?.ProductImage
                .OrderBy(pi => pi.SortOrder)
                .Select(pi => pi.Image?.ImageUrl)
                .FirstOrDefault();

            return MapToDto(offer, offer.Buyer, offer.Product, mainImage);
        }

        public async Task<string> CheckoutFromOfferAsync(OfferCheckoutRequestDto request, string accountId)
        {
            var account = await _context.Account.FirstOrDefaultAsync(a => a.AccountId == accountId);
            if (account == null || string.IsNullOrEmpty(account.UserId))
                throw new Exception("Account not found.");

            var buyerUserId = account.UserId;

            var offer = await _context.Offer
                .Include(o => o.Product)
                .FirstOrDefaultAsync(o => o.OfferId == request.OfferId);

            if (offer == null) throw new Exception("Offer not found.");
            if (offer.BuyerId != buyerUserId) throw new Exception("This offer does not belong to you.");
            if (offer.Status != "Accepted") throw new Exception("Offer must be accepted before checkout.");
            if (offer.ExpiresAt.HasValue && offer.ExpiresAt.Value < DateTime.UtcNow)
                throw new Exception("This offer has expired.");

            var product = offer.Product ?? throw new Exception("Product not found.");
            var address = await _context.Address.FirstOrDefaultAsync(a => a.AddressId == request.AddressId);
            if (address == null) throw new Exception("Address not found.");

            // Calculate shipping fee using checkout service
            var feeResult = await _checkoutService.CalculateShippingFeeAsync(new CalculateFeeRequestDto
            {
                ProductId = product.ProductId,
                AddressId = request.AddressId
            });

            var offerPrice = offer.OfferPrice ?? product.Price ?? 0;
            var shippingFee = feeResult.ShippingFee;
            var totalAmount = offerPrice + shippingFee;

            string initialStatus = string.Equals(request.PaymentMethod, "vnpay", StringComparison.OrdinalIgnoreCase)
                ? RetradeBE.Models.Enums.OrderStatusEnum.AwaitingPayment.ToString()
                : RetradeBE.Models.Enums.OrderStatusEnum.Pending.ToString();

            var now = DateTime.UtcNow.AddHours(7);
            var random = new Random().Next(10, 99).ToString();
            var orderCode = $"ORD{random}{now:yyyyMMddHHmm}";

            var addressSnapshot = await _checkoutService.GetAddressSnapshotPublicAsync(address);

            var order = new Order
            {
                OrderId = Guid.NewGuid().ToString(),
                OrderCode = orderCode,
                UserId = buyerUserId,
                SellerId = product.SellerId,
                ProductId = product.ProductId,
                OfferId = offer.OfferId,
                Quantity = 1,
                UnitPrice = offerPrice,
                ShippingFee = shippingFee,
                TotalAmount = totalAmount,
                FinalAmount = totalAmount,
                AddressSnapshot = addressSnapshot,
                Status = initialStatus,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                ShippingProvider = "GHN",
                ExpectedDeliveryTime = DateTime.UtcNow.AddDays(5)
            };

            _context.Order.Add(order);

            // Mark product as sold / decrement stock
            if (product.StockQuantity.HasValue)
            {
                product.StockQuantity -= 1;
                if (product.StockQuantity <= 0)
                {
                    product.StockQuantity = 0;
                    product.Status = RetradeBE.Models.Enums.ProductStatusEnum.Sold.ToString();
                }
                _context.Product.Update(product);
            }

            // Mark offer as completed
            offer.Status = "Completed";

            // Remove from wishlist
            var wishlist = await _context.Wishlist
                .FirstOrDefaultAsync(w => w.UserId == buyerUserId && w.Status == "Active" && w.IsDeleted != true);
            if (wishlist != null)
            {
                var wishlistItem = await _context.WishlistItem
                    .FirstOrDefaultAsync(wi => wi.WishlistId == wishlist.WishlistId && wi.ProductId == product.ProductId);
                if (wishlistItem != null) _context.WishlistItem.Remove(wishlistItem);
            }

            await _context.SaveChangesAsync();

            return order.OrderId;
        }

        private static OfferDto MapToDto(RetradeBE.Models.Offer offer, User? buyer, Product? product, string? mainImageUrl)
        {
            return new OfferDto
            {
                OfferId = offer.OfferId,
                BuyerId = offer.BuyerId,
                BuyerName = buyer != null ? $"{buyer.FirstName} {buyer.LastName}".Trim() : null,
                ProductId = offer.ProductId,
                ProductName = product?.Name,
                ProductImageUrl = mainImageUrl,
                OriginalPrice = product?.Price,
                OfferPrice = offer.OfferPrice,
                Message = offer.Message,
                ExpiresAt = offer.ExpiresAt,
                Status = offer.Status,
                CreatedAt = offer.CreatedAt
            };
        }
    }
}
