using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using RetradeBE.Data;
using RetradeBE.Hubs;
using RetradeBE.Models;
using RetradeBE.Models.DTOs;
using RetradeBE.Services.Checkout;
using RetradeBE.Repositories;
namespace RetradeBE.Services.Offer
{
    public class OfferService : IOfferService
    {
        private readonly ICheckoutService _checkoutService;
        private readonly IHubContext<OrderHub> _orderHub;
        private readonly IOfferRepository _repo;
        private readonly IAccountRepository _accountRepo;
        private readonly IProductRepository _productRepo;
        private readonly IUserRepository _userRepo;
        private readonly IAddressRepository _addressRepo;
        private readonly IOrderRepository _orderRepo;
        private readonly IWishlistRepository _wishlistRepo;
        private readonly INotificationService _notificationService;

        public OfferService(
            ICheckoutService checkoutService, 
            IHubContext<OrderHub> orderHub, 
            IOfferRepository repo,
            IAccountRepository accountRepo,
            IProductRepository productRepo,
            IUserRepository userRepo,
            IAddressRepository addressRepo,
            IOrderRepository orderRepo,
            IWishlistRepository wishlistRepo,
            INotificationService notificationService)
        {
            _checkoutService = checkoutService;
            _orderHub = orderHub;
            _repo = repo;
            _accountRepo = accountRepo;
            _productRepo = productRepo;
            _userRepo = userRepo;
            _addressRepo = addressRepo;
            _orderRepo = orderRepo;
            _wishlistRepo = wishlistRepo;
            _notificationService = notificationService;
        }

        public async Task<OfferDto> MakeOfferAsync(string accountId, MakeOfferRequestDto request)
        {
            var account = await _accountRepo.Query().FirstOrDefaultAsync(a => a.AccountId == accountId);
            if (account == null || string.IsNullOrEmpty(account.UserId))
                throw new Exception("Account not found or not linked to a user.");

            var buyerUserId = account.UserId;

            var product = await _productRepo.Query()
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
            var existingOffer = await _repo.Query().FirstOrDefaultAsync(o =>
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

            await _repo.AddAsync(offer);

            await _notificationService.CreateAndSendAsync(new CreateNotificationDto
            {
                UserId = product.SellerId,
                Title = "New Offer Received",
                Message = $"You received a new offer of {request.OfferPrice:N0} VND for '{product.Name}'.",
                Type = "Offer",
                ReferenceId = offer.OfferId
            });

            var buyer = await _userRepo.GetByIdAsync(buyerUserId);
            var mainImage = product.ProductImage
                .OrderBy(pi => pi.SortOrder)
                .Select(pi => pi.Image?.ImageUrl)
                .FirstOrDefault();

            return MapToDto(offer, buyer, product, mainImage);
        }

        public async Task<List<OfferDto>> GetMyOffersAsync(string accountId, string? productId = null)
        {
            var account = await _accountRepo.Query().FirstOrDefaultAsync(a => a.AccountId == accountId);
            if (account == null || string.IsNullOrEmpty(account.UserId))
                return new List<OfferDto>();

            var buyerUserId = account.UserId;

            var query = _repo.Query()
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
            var product = await _productRepo.Query().FirstOrDefaultAsync(p => p.ProductId == productId);
            if (product == null || product.SellerId != sellerId)
                throw new Exception("Product not found or you are not the seller.");

            var offers = await _repo.Query()
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

        public async Task<OfferDto> RespondToOfferAsync(string sellerId, string offerId, bool accept)
        {
            var offer = await _repo.GetByIdAsync(offerId);

            if (offer == null) throw new Exception("Offer not found.");
            if (offer.Product?.SellerId != sellerId) throw new Exception("You are not authorized to manage this offer.");
            if (offer.Status != "Pending") throw new Exception("Only pending offers can be accepted or rejected.");

            offer.Status = accept ? "Accepted" : "Rejected";
            await _repo.UpdateAsync(offer);

            await _notificationService.CreateAndSendAsync(new CreateNotificationDto
            {
                UserId = offer.BuyerId,
                Title = accept ? "Offer Accepted" : "Offer Rejected",
                Message = $"Your offer for '{offer.Product?.Name}' was {(accept ? "accepted" : "rejected")} by the seller.",
                Type = "Offer",
                ReferenceId = offer.OfferId
            });

            var mainImage = offer.Product?.ProductImage
                .OrderBy(pi => pi.SortOrder)
                .Select(pi => pi.Image?.ImageUrl)
                .FirstOrDefault();

            return MapToDto(offer, offer.Buyer, offer.Product, mainImage);
        }

        public async Task<OfferDto> CancelOfferAsync(string buyerUserId, string offerId)
        {
            var offer = await _repo.GetByIdAsync(offerId);
            if (offer == null) throw new Exception("Offer not found.");
            if (offer.BuyerId != buyerUserId) throw new Exception("You are not authorized to cancel this offer.");
            if (offer.Status != "Pending") throw new Exception("Only pending offers can be cancelled.");

            offer.Status = "Cancelled";
            await _repo.UpdateAsync(offer);

            var mainImage = offer.Product?.ProductImage
                .OrderBy(pi => pi.SortOrder)
                .Select(pi => pi.Image?.ImageUrl)
                .FirstOrDefault();

            return MapToDto(offer, offer.Buyer, offer.Product, mainImage);
        }

        public async Task<string> CheckoutFromOfferAsync(OfferCheckoutRequestDto request, string accountId)
        {
            var account = await _accountRepo.Query().FirstOrDefaultAsync(a => a.AccountId == accountId);
            if (account == null || string.IsNullOrEmpty(account.UserId))
                throw new Exception("Account not found.");

            var buyerUserId = account.UserId;

            var offer = await _repo.Query()
                .Include(o => o.Product)
                .FirstOrDefaultAsync(o => o.OfferId == request.OfferId);

            if (offer == null) throw new Exception("Offer not found.");
            if (offer.BuyerId != buyerUserId) throw new Exception("This offer does not belong to you.");
            if (offer.Status != "Accepted" && offer.Status != "CounterOffer") throw new Exception("Offer must be accepted or counter-offered before checkout.");
            if (offer.ExpiresAt.HasValue && offer.ExpiresAt.Value < DateTime.UtcNow)
                throw new Exception("This offer has expired.");

            var product = offer.Product ?? throw new Exception("Product not found.");
            var address = await _addressRepo.Query().FirstOrDefaultAsync(a => a.AddressId == request.AddressId);
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
                OrderId = RetradeBE.Utils.IdGenerator.GenerateOrderId(new Random().Next(1, 9999)),
                OrderCode = orderCode,
                BuyerId = buyerUserId,
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

            await _orderRepo.AddAsync(order);

            // Mark product as sold / decrement stock
            if (product.StockQuantity.HasValue)
            {
                product.StockQuantity -= 1;
                if (product.StockQuantity <= 0)
                {
                    product.StockQuantity = 0;
                    product.Status = RetradeBE.Models.Enums.ProductStatusEnum.Sold.ToString();
                }
                await _productRepo.UpdateAsync(product);
            }

            // Mark offer as completed
            offer.Status = "Completed";
            await _repo.UpdateAsync(offer);

            // Remove from wishlist
            var wishlist = await _wishlistRepo.GetOrCreateActiveWishlistAsync(buyerUserId);
            if (wishlist != null)
            {
                var wishlistItem = await _wishlistRepo.GetItemByProductAsync(wishlist.WishlistId, product.ProductId);
                if (wishlistItem != null) await _wishlistRepo.RemoveItemAsync(wishlistItem);
            }

            return order.OrderId;
        }
        public async Task<List<OfferDto>> GetOffersBySellerAsync(string sellerUserId)
        {
            var offers = await _repo
                .GetOffersBySellerAsync(sellerUserId);
            return offers
                .OrderByDescending(x => x.CreatedAt)
                .Select(o =>
                {
                    var mainImage = o.Product?
                        .ProductImage
                        .OrderBy(pi => pi.SortOrder)
                        .Select(pi => pi.Image!.ImageUrl)
                        .FirstOrDefault();

                    return MapToDto(
                        o,
                        o.Buyer,
                        o.Product,
                        mainImage);
                })
                .ToList();
        }

        public async Task<OfferDto> CounterOfferAsync(string sellerId, CounterOfferDto request)
        {
            var offer = await _repo.GetByIdAsync(request.OfferId);

            if (offer == null)
                throw new Exception("Offer not found.");

            if (offer.Product == null)
                throw new Exception("Product not found.");

            if (offer.Product.SellerId != sellerId)
                throw new Exception("You are not authorized to manage this offer.");

            if (offer.Status != "Pending" &&
                offer.Status != "CounterOffer")
                throw new Exception("Only pending offers can be countered.");

            if (request.CounterPrice <= 0)
                throw new Exception("Counter price must be greater than 0.");
            if (request.CounterPrice <= offer.OfferPrice)
                throw new Exception("Counter offer price must be greater than the buyer's offer.");

            if (request.CounterPrice >= offer.Product!.Price)
                throw new Exception("Counter offer price must be lower than the product price.");

            offer.OfferPrice = request.CounterPrice;
            offer.Status = "CounterOffer";

            await _repo.UpdateAsync(offer);

            await _notificationService.CreateAndSendAsync(new CreateNotificationDto
            {
                UserId = offer.BuyerId,
                Title = "Counter Offer Received",
                Message = $"The seller made a counter offer of {request.CounterPrice:N0} VND for '{offer.Product?.Name}'.",
                Type = "Offer",
                ReferenceId = offer.OfferId
            });

            var mainImage = offer.Product?.ProductImage
                .OrderBy(pi => pi.SortOrder)
                .Select(pi => pi.Image?.ImageUrl)
                .FirstOrDefault();

            return MapToDto(
                offer,
                offer.Buyer,
                offer.Product,
                mainImage);
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
