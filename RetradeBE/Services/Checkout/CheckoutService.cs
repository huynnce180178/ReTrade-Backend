using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using RetradeBE.Data;
using RetradeBE.Hubs;
using RetradeBE.Models;
using RetradeBE.Models.DTOs;
using RetradeBE.Services.Ghn;

namespace RetradeBE.Services.Checkout
{
    public class CheckoutService : ICheckoutService
    {
        private readonly AppDbContext _context;
        private readonly IGhnService _ghnService;
        private readonly IHubContext<OrderHub> _orderHub;

        public CheckoutService(
            AppDbContext context,
            IGhnService ghnService,
            IHubContext<OrderHub> orderHub)
        {
            _context = context;
            _ghnService = ghnService;
            _orderHub = orderHub;
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

            var addressSnapshot = await GetAddressSnapshotAsync(address);

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
                AddressSnapshot = addressSnapshot,
                Status = initialStatus,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                ShippingProvider = "GHN",
                ExpectedDeliveryTime = DateTime.UtcNow.AddDays(5)
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
            await NotifySellerOrderChangedAsync(order, "Created");

            return order.OrderId;
        }

        private async Task NotifySellerOrderChangedAsync(Order order, string eventType)
        {
            if (string.IsNullOrWhiteSpace(order.SellerId))
            {
                return;
            }

            await _orderHub.Clients
                .Group(OrderHub.GetSellerOrderGroupName(order.SellerId))
                .SendAsync("SellerOrderStatusChanged", new
                {
                    EventType = eventType,
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

        private async Task<string> GetAddressSnapshotAsync(Address address)
            => await GetAddressSnapshotPublicAsync(address);

        public async Task<string> GetAddressSnapshotPublicAsync(Address address)
        {
            var receiverName = address.ReceiverName ?? "";
            var receiverPhone = address.ReceiverPhone ?? "";
            var street = address.Street ?? "";
            var provinceName = address.ProvinceId?.ToString() ?? "";
            var districtName = address.DistrictId?.ToString() ?? "";
            var wardName = address.WardCode ?? "";

            try
            {
                if (address.ProvinceId.HasValue)
                {
                    var provincesObj = await _ghnService.GetProvincesAsync();
                    var provincesJson = JsonSerializer.Serialize(provincesObj);
                    using var doc = JsonDocument.Parse(provincesJson);
                    if (doc.RootElement.ValueKind == JsonValueKind.Array)
                    {
                        var provinceEl = doc.RootElement.EnumerateArray()
                            .FirstOrDefault(p => {
                                if (p.TryGetProperty("ProvinceID", out var idProp) && idProp.TryGetInt32(out var id))
                                    return id == address.ProvinceId.Value;
                                if (p.TryGetProperty("provinceID", out var idProp2) && idProp2.TryGetInt32(out var id2))
                                    return id2 == address.ProvinceId.Value;
                                return false;
                            });
                        if (provinceEl.ValueKind != JsonValueKind.Undefined)
                        {
                            if (provinceEl.TryGetProperty("ProvinceName", out var nameProp))
                                provinceName = nameProp.GetString() ?? provinceName;
                            else if (provinceEl.TryGetProperty("provinceName", out var nameProp2))
                                provinceName = nameProp2.GetString() ?? provinceName;
                        }
                    }
                }

                if (address.ProvinceId.HasValue && address.DistrictId.HasValue)
                {
                    var districtsObj = await _ghnService.GetDistrictsAsync(address.ProvinceId.Value);
                    var districtsJson = JsonSerializer.Serialize(districtsObj);
                    using var doc = JsonDocument.Parse(districtsJson);
                    if (doc.RootElement.ValueKind == JsonValueKind.Array)
                    {
                        var districtEl = doc.RootElement.EnumerateArray()
                            .FirstOrDefault(d => {
                                if (d.TryGetProperty("DistrictID", out var idProp) && idProp.TryGetInt32(out var id))
                                    return id == address.DistrictId.Value;
                                if (d.TryGetProperty("districtID", out var idProp2) && idProp2.TryGetInt32(out var id2))
                                    return id2 == address.DistrictId.Value;
                                return false;
                            });
                        if (districtEl.ValueKind != JsonValueKind.Undefined)
                        {
                            if (districtEl.TryGetProperty("DistrictName", out var nameProp))
                                districtName = nameProp.GetString() ?? districtName;
                            else if (districtEl.TryGetProperty("districtName", out var nameProp2))
                                districtName = nameProp2.GetString() ?? districtName;
                        }
                    }
                }

                if (address.DistrictId.HasValue && !string.IsNullOrEmpty(address.WardCode))
                {
                    var wardsObj = await _ghnService.GetWardsAsync(address.DistrictId.Value);
                    var wardsJson = JsonSerializer.Serialize(wardsObj);
                    using var doc = JsonDocument.Parse(wardsJson);
                    if (doc.RootElement.ValueKind == JsonValueKind.Array)
                    {
                        var wardEl = doc.RootElement.EnumerateArray()
                            .FirstOrDefault(w => {
                                if (w.TryGetProperty("WardCode", out var codeProp))
                                    return codeProp.GetString() == address.WardCode;
                                if (w.TryGetProperty("wardCode", out var codeProp2))
                                    return codeProp2.GetString() == address.WardCode;
                                return false;
                            });
                        if (wardEl.ValueKind != JsonValueKind.Undefined)
                        {
                            if (wardEl.TryGetProperty("WardName", out var nameProp))
                                wardName = nameProp.GetString() ?? wardName;
                            else if (wardEl.TryGetProperty("wardName", out var nameProp2))
                                wardName = nameProp2.GetString() ?? wardName;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error resolving address names from GHN: {ex.Message}");
            }

            return $"{receiverName} - {receiverPhone} - {street}, {wardName}, {districtName}, {provinceName}";
        }
    }
}
