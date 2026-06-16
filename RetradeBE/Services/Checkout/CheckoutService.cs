using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RetradeBE.Data;
using RetradeBE.Models;
using RetradeBE.Models.DTOs;
using RetradeBE.Services.Ghn;

namespace RetradeBE.Services.Checkout
{
    public class CheckoutService : ICheckoutService
    {
        private readonly AppDbContext _context;
        private readonly IGhnService _ghnService;

        public CheckoutService(AppDbContext context, IGhnService ghnService)
        {
            _context = context;
            _ghnService = ghnService;
        }

        public async Task<CalculateFeeResponseDto> CalculateShippingFeeAsync(CalculateFeeRequestDto request)
        {
            var product = await _context.Product.FirstOrDefaultAsync(p => p.ProductId == request.ProductId);
            if (product == null)
                throw new Exception("Product not found");

            var address = await _context.Address.FirstOrDefaultAsync(a => a.AddressId == request.AddressId);
            if (address == null)
                throw new Exception("Address not found");

            if (!address.DistrictId.HasValue)
                throw new Exception("Address does not have a valid DistrictId");

            // Lấy địa chỉ người bán (Seller)
            var sellerAddress = await _context.Address.FirstOrDefaultAsync(a => a.UserId == product.SellerId && a.IsDefault == true);
            if (sellerAddress == null)
            {
                // Nếu không có địa chỉ mặc định, lấy địa chỉ đầu tiên của seller
                sellerAddress = await _context.Address.FirstOrDefaultAsync(a => a.UserId == product.SellerId);
                if (sellerAddress == null)
                    throw new Exception("Seller address not found. Cannot calculate shipping fee.");
            }

            var ghnRequest = new GhnCalculateFeeRequest
            {
                ServiceTypeId = 2, // 2 = Hàng nhẹ, 5 = Hàng nặng
                FromDistrictId = sellerAddress.DistrictId,
                FromWardCode = sellerAddress.WardCode,
                ToDistrictId = address.DistrictId.Value,
                ToWardCode = address.WardCode,
                Weight = product.WeightGram ?? 1000,
                Length = product.LengthCm ?? 20,
                Width = product.WidthCm ?? 20,
                Height = product.HeightCm ?? 20,
                InsuranceValue = product.Price.HasValue ? (int)product.Price.Value : 0
            };

            // Limit insurance value up to 5,000,000 as per GHN docs
            if (ghnRequest.InsuranceValue > 5000000)
            {
                ghnRequest.InsuranceValue = 5000000;
            }

            GhnCalculateFeeResponse ghnResponse;
            try
            {
                ghnResponse = await _ghnService.CalculateFeeAsync(ghnRequest);
            }
            catch
            {
                // Try fallback to ServiceTypeId 5 (Traditional Delivery) if 2 is unsupported for this route
                ghnRequest.ServiceTypeId = 5;
                ghnResponse = await _ghnService.CalculateFeeAsync(ghnRequest);
            }

            if (ghnResponse.Code != 200 || ghnResponse.Data == null)
            {
                throw new Exception($"Failed to calculate fee: {ghnResponse.Message}");
            }

            return new CalculateFeeResponseDto
            {
                ShippingFee = ghnResponse.Data.Total
            };
        }

        public async Task<string> ProcessCheckoutAsync(CheckoutRequestDto request, string accountId)
        {
            // Lookup UserId from AccountId
            var account = await _context.Account.FirstOrDefaultAsync(a => a.AccountId == accountId);
            if (account == null || string.IsNullOrEmpty(account.UserId))
                throw new Exception("Account not found or not linked to a user.");

            var userId = account.UserId;

            var product = await _context.Product.FirstOrDefaultAsync(p => p.ProductId == request.ProductId);
            if (product == null)
                throw new Exception("Product not found");

            var address = await _context.Address.FirstOrDefaultAsync(a => a.AddressId == request.AddressId);
            if (address == null)
                throw new Exception("Address not found");

            if (product.StockQuantity < request.Quantity)
                throw new Exception("Not enough stock");

            // Calculate Fee again to verify
            var feeResult = await CalculateShippingFeeAsync(new CalculateFeeRequestDto
            {
                ProductId = request.ProductId,
                AddressId = request.AddressId
            });

            var subtotal = (product.Price ?? 0) * request.Quantity;
            var totalAmount = subtotal + feeResult.ShippingFee;

            string initialStatus = string.Equals(request.PaymentMethod, "vnpay", StringComparison.OrdinalIgnoreCase) 
                ? RetradeBE.Models.Enums.OrderStatusEnum.AwaitingPayment.ToString() 
                : RetradeBE.Models.Enums.OrderStatusEnum.Pending.ToString();

            var now = DateTime.UtcNow.AddHours(7);
            var random = new Random().Next(10, 99).ToString();
            var orderCode = $"ORD{random}{now:yyyyMMddHHmm}";

            var order = new Order
            {
                OrderId = Guid.NewGuid().ToString(),
                OrderCode = orderCode,
                UserId = userId,
                SellerId = product.SellerId,
                ProductId = product.ProductId,
                Quantity = request.Quantity,
                UnitPrice = product.Price,
                ShippingFee = feeResult.ShippingFee,
                TotalAmount = totalAmount,
                FinalAmount = totalAmount,
                AddressSnapshot = $"{address.ReceiverName} - {address.ReceiverPhone} - {address.Street}, {address.WardCode}, {address.DistrictId}, {address.ProvinceId}",
                Status = initialStatus,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                ShippingProvider = "GHN"
            };

            _context.Order.Add(order);
            
            // Optionally decrement stock
            if (product.StockQuantity.HasValue)
            {
                product.StockQuantity -= request.Quantity;
                if (product.StockQuantity <= 0)
                {
                    product.StockQuantity = 0;
                    product.Status = RetradeBE.Models.Enums.ProductStatusEnum.Sold.ToString();
                }
                _context.Product.Update(product);
            }

            // Remove product from user's wishlist if it exists
            var wishlist = await _context.Wishlist
                .FirstOrDefaultAsync(w => w.UserId == userId && w.Status == "Active" && w.IsDeleted != true);
            if (wishlist != null)
            {
                var wishlistItem = await _context.WishlistItem
                    .FirstOrDefaultAsync(wi => wi.WishlistId == wishlist.WishlistId && wi.ProductId == request.ProductId);
                if (wishlistItem != null)
                {
                    _context.WishlistItem.Remove(wishlistItem);
                }
            }

            await _context.SaveChangesAsync();

            return order.OrderId;
        }
    }
}
