using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.SignalR;
using RetradeBE.Config;
using RetradeBE.Data;
using RetradeBE.Hubs;
using RetradeBE.Models;
using RetradeBE.Models.DTOs;

namespace RetradeBE.Services;

public class PaymentService : IPaymentService
{
    private const string PaymentMethod = "VNPAY";
    private const string AuctionDepositPaymentPrefix = "AUCTION_DEPOSIT:";
    private readonly AppDbContext _context;
    private readonly VnPaySettings _vnPaySettings;
    private readonly ILogger<PaymentService> _logger;
    private readonly IHubContext<OrderHub> _orderHub;
    private readonly IHubContext<AuctionHub> _auctionHub;

    public PaymentService(
        AppDbContext context,
        IOptions<VnPaySettings> vnPaySettings,
        ILogger<PaymentService> logger,
        IHubContext<OrderHub> orderHub,
        IHubContext<AuctionHub> auctionHub)
    {
        _context = context;
        _vnPaySettings = vnPaySettings.Value;
        _logger = logger;
        _orderHub = orderHub;
        _auctionHub = auctionHub;
    }

    public async Task<CreateVnPayPaymentResponseDto> CreateVnPayPaymentUrlAsync(
        string accountId,
        CreateVnPayPaymentRequestDto request,
        string ipAddress)
    {
        ValidateSettings();

        var account = await _context.Account
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.AccountId == accountId);

        if (account == null || string.IsNullOrWhiteSpace(account.UserId))
        {
            throw new InvalidOperationException("Account not found.");
        }

        // Validate order (nếu là payment cho đơn hàng)
        if (!string.IsNullOrWhiteSpace(request.OrderId))
        {
            var orderExists = await _context.Order.AnyAsync(x => x.OrderId == request.OrderId);
            if (!orderExists)
            {
                throw new InvalidOperationException("Order not found.");
            }
        }

        // Validate service subscription (nếu là payment nâng cấp gói)
        if (!string.IsNullOrWhiteSpace(request.ServiceId))
        {
            var serviceExists = await _context.ServiceSubscription.AnyAsync(x => x.ServiceId == request.ServiceId);
            if (!serviceExists)
            {
                throw new InvalidOperationException("Service package not found.");
            }
        }

        if (!string.IsNullOrWhiteSpace(request.AuctionDepositId))
        {
            var deposit = await _context.AuctionDeposit
                .FirstOrDefaultAsync(x => x.AuctionDepositId == request.AuctionDepositId && x.UserId == account.UserId);

            if (deposit == null)
            {
                throw new InvalidOperationException("Auction deposit not found.");
            }

            if (deposit.Status == "Paid")
            {
                throw new InvalidOperationException("Auction deposit is already paid.");
            }

            if (deposit.DepositAmount != request.Amount)
            {
                throw new InvalidOperationException("Payment amount does not match auction deposit.");
            }
        }

        var paymentId = $"PAY_{Guid.NewGuid():N}";
        var createDate = DateTime.UtcNow.AddHours(7);
        var amount = Convert.ToInt64(decimal.Round(request.Amount * 100, 0, MidpointRounding.AwayFromZero));
        var locale = string.IsNullOrWhiteSpace(request.Locale) ? _vnPaySettings.Locale : request.Locale.Trim().ToLowerInvariant();

        var payment = new Payment
        {
            PaymentId = paymentId,
            OrderId = request.OrderId,
            ServiceId = request.ServiceId,
            UserId = account.UserId,
            Amount = request.Amount,
            PaymentMethod = PaymentMethod,
            ProviderTransactionId = string.IsNullOrWhiteSpace(request.AuctionDepositId)
                ? null
                : $"{AuctionDepositPaymentPrefix}{request.AuctionDepositId}",
            Status = "Pending",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Payment.Add(payment);
        await _context.SaveChangesAsync();

        var queryParams = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["vnp_Version"] = _vnPaySettings.Version,
            ["vnp_Command"] = _vnPaySettings.Command,
            ["vnp_TmnCode"] = _vnPaySettings.TmnCode,
            ["vnp_Amount"] = amount.ToString(CultureInfo.InvariantCulture),
            ["vnp_CreateDate"] = createDate.ToString("yyyyMMddHHmmss"),
            ["vnp_CurrCode"] = _vnPaySettings.CurrencyCode,
            ["vnp_IpAddr"] = NormalizeIpAddress(ipAddress),
            ["vnp_Locale"] = locale,
            ["vnp_OrderInfo"] = request.OrderDescription.Trim(),
            ["vnp_OrderType"] = "other",
            ["vnp_ReturnUrl"] = _vnPaySettings.CallbackUrl,
            ["vnp_TxnRef"] = paymentId,
            ["vnp_ExpireDate"] = createDate.AddMinutes(15).ToString("yyyyMMddHHmmss")
        };

        if (!string.IsNullOrWhiteSpace(request.BankCode))
        {
            queryParams["vnp_BankCode"] = request.BankCode.Trim().ToUpperInvariant();
        }

        // VNPAY uses URL-encoded values for both the query string and the HMAC hash data
        var queryString = BuildQueryString(queryParams);
        var secureHash = ComputeHmacSha512(_vnPaySettings.HashSecret, queryString);
        
        var paymentUrl = $"{_vnPaySettings.BaseUrl}?{queryString}&vnp_SecureHash={secureHash}";

        return new CreateVnPayPaymentResponseDto
        {
            PaymentId = paymentId,
            PaymentUrl = paymentUrl
        };
    }

    public async Task<VnPayReturnResponseDto> ProcessVnPayCallbackAsync(HttpRequest request)
    {
        ValidateSettings();

        // Parse query from IQueryCollection for data extraction
        var query = request.Query;
        var allParams = query
            .Where(x => x.Key.StartsWith("vnp_", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(
                x => x.Key,
                x => x.Value.ToString(),
                StringComparer.Ordinal);

        var receivedSecureHash = allParams.TryGetValue("vnp_SecureHash", out var hash) ? hash : string.Empty;

        // Bỏ qua ASP.NET URL-decoding bằng cách parse trực tiếp chuỗi QueryString gốc
        var rawQuery = request.QueryString.Value ?? string.Empty;
        if (rawQuery.StartsWith("?")) rawQuery = rawQuery.Substring(1);

        var signData = string.Join("&", rawQuery.Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Where(p => p.StartsWith("vnp_", StringComparison.OrdinalIgnoreCase))
            .Where(p => !p.StartsWith("vnp_SecureHash=", StringComparison.OrdinalIgnoreCase))
            .Where(p => !p.StartsWith("vnp_SecureHashType=", StringComparison.OrdinalIgnoreCase))
            .OrderBy(p => p.Split('=')[0], StringComparer.Ordinal));

        var computedHash = ComputeHmacSha512(_vnPaySettings.HashSecret, signData);

        if (!string.Equals(receivedSecureHash, computedHash, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Invalid VNPAY signature. Received: {receivedSecureHash}, Computed: {computedHash}, SignData: {signData}");
        }

        var paymentId = allParams.TryGetValue("vnp_TxnRef", out var txnRef) ? txnRef : string.Empty;
        var responseCode = allParams.TryGetValue("vnp_ResponseCode", out var rc) ? rc : null;
        var transactionStatus = allParams.TryGetValue("vnp_TransactionStatus", out var ts) ? ts : null;
        var transactionNo = allParams.TryGetValue("vnp_TransactionNo", out var tn) ? tn : null;
        var amountRaw = allParams.TryGetValue("vnp_Amount", out var amountValue) ? amountValue : "0";
        var amount = decimal.TryParse(amountRaw, out var parsedAmount)
            ? parsedAmount / 100m
            : 0m;

        var payment = await _context.Payment.FirstOrDefaultAsync(x => x.PaymentId == paymentId);
        if (payment == null)
        {
            throw new InvalidOperationException("Payment not found.");
        }

        var isSuccess = responseCode == "00" && transactionStatus == "00";
        var auctionDepositRef = payment.ProviderTransactionId != null
            && payment.ProviderTransactionId.StartsWith(AuctionDepositPaymentPrefix, StringComparison.OrdinalIgnoreCase)
                ? payment.ProviderTransactionId.Substring(AuctionDepositPaymentPrefix.Length)
                : null;

        payment.Status = isSuccess ? "Success" : "Failed";
        payment.ProviderTransactionId = transactionNo;
        payment.UpdatedAt = DateTime.UtcNow;

        // Nếu là payment mua gói subscription thì kích hoạt
        if (isSuccess && !string.IsNullOrWhiteSpace(payment.ServiceId))
        {
            await ActivateSubscriptionAsync(payment.UserId!, payment.ServiceId, payment.Amount ?? 0);
        }

        // Nếu là payment cho đơn hàng thì cập nhật trạng thái đơn hàng
        AuctionDeposit? updatedDeposit = null;
        string? auctionId = null;

        if (!string.IsNullOrWhiteSpace(auctionDepositRef))
        {
            var deposit = await _context.AuctionDeposit
                .FirstOrDefaultAsync(x => x.AuctionDepositId == auctionDepositRef);
            if (deposit != null)
            {
                deposit.Status = isSuccess ? "Paid" : "Failed";
                updatedDeposit = deposit;
                auctionId = deposit.AuctionId;
            }
        }

        Order? updatedOrder = null;

        if (isSuccess && !string.IsNullOrWhiteSpace(payment.OrderId))
        {
            var order = await _context.Order
                .Include(o => o.User)
                .Include(o => o.Product)
                    .ThenInclude(p => p!.ProductImage)
                    .ThenInclude(pi => pi.Image)
                .FirstOrDefaultAsync(o => o.OrderId == payment.OrderId);
            if (order != null)
            {
                order.Status = RetradeBE.Models.Enums.OrderStatusEnum.Pending.ToString();
                order.UpdatedAt = DateTime.UtcNow;
                updatedOrder = order;
            }
        }

        await _context.SaveChangesAsync();
        if (updatedOrder != null)
        {
            await NotifySellerOrderChangedAsync(updatedOrder, "PaymentConfirmed");
        }
        if (updatedDeposit != null)
        {
            await NotifyAuctionDepositChangedAsync(updatedDeposit, isSuccess ? "DepositPaid" : "DepositFailed");
        }

        return new VnPayReturnResponseDto
        {
            IsSuccess = isSuccess,
            PaymentId = payment.PaymentId,
            OrderId = payment.OrderId,
            AuctionId = auctionId,
            Status = payment.Status ?? string.Empty,
            Message = isSuccess ? "Payment completed successfully." : MapVnPayMessage(responseCode, transactionStatus),
            TransactionNo = transactionNo,
            TransactionStatus = transactionStatus,
            ResponseCode = responseCode,
            Amount = amount
        };
    }

    private async Task NotifySellerOrderChangedAsync(Order order, string eventType)
    {
        if (string.IsNullOrWhiteSpace(order.SellerId))
        {
            return;
        }

        var productImageUrl = order.Product?.ProductImage
            .Where(pi => pi.IsMain == true)
            .Select(pi => pi.Image.ImageUrl)
            .FirstOrDefault()
            ?? order.Product?.ProductImage
                .OrderBy(pi => pi.SortOrder)
                .Select(pi => pi.Image.ImageUrl)
                .FirstOrDefault();

        await _orderHub.Clients
            .Group(OrderHub.GetSellerOrderGroupName(order.SellerId))
            .SendAsync("SellerOrderStatusChanged", new
            {
                EventType = eventType,
                order.OrderId,
                order.OrderCode,
                order.ProductId,
                ProductName = order.Product?.Name,
                ProductImageUrl = productImageUrl,
                BuyerId = order.UserId,
                BuyerName = order.User != null ? $"{order.User.FirstName} {order.User.LastName}".Trim() : null,
                BuyerEmail = order.User?.Email,
                order.SellerId,
                order.Quantity,
                order.UnitPrice,
                order.TotalAmount,
                order.ShippingFee,
                order.DiscountAmount,
                order.FinalAmount,
                order.Status,
                order.TrackingCode,
                order.ShippingProvider,
                order.ExpectedDeliveryTime,
                order.CreatedAt,
                order.UpdatedAt
            });
    }

    private async Task NotifyAuctionDepositChangedAsync(AuctionDeposit deposit, string eventType)
    {
        if (string.IsNullOrWhiteSpace(deposit.AuctionId) || string.IsNullOrWhiteSpace(deposit.UserId))
        {
            return;
        }

        await _auctionHub.Clients
            .Group(AuctionHub.GetAuctionUserGroupName(deposit.AuctionId, deposit.UserId))
            .SendAsync("AuctionDepositChanged", new
            {
                EventType = eventType,
                Deposit = new
                {
                    deposit.AuctionDepositId,
                    deposit.AuctionId,
                    deposit.UserId,
                    deposit.DepositAmount,
                    PolicyAccepted = deposit.PolicyAccepted == true,
                    deposit.Status,
                    deposit.CreatedAt,
                    MaxBidAmount = Math.Max(0, (deposit.DepositAmount ?? 0) - 20000m),
                    CanBid = deposit.Status == "Paid" && deposit.PolicyAccepted == true
                }
            });
    }

    /// <summary>
    /// Kích hoạt gói subscription: tạo MyService và gán role Seller cho account.
    /// </summary>
    private async Task ActivateSubscriptionAsync(string userId, string serviceId, decimal amount)
    {
        var service = await _context.ServiceSubscription
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ServiceId == serviceId);

        if (service == null)
        {
            _logger.LogWarning("ActivateSubscription: ServiceId {ServiceId} not found.", serviceId);
            return;
        }

        var now = DateTime.UtcNow;
        var durationDays = service.DurationDays ?? 365;

        // 1. Tạo bản ghi MyService
        var myService = new MyService
        {
            UserSubId = $"SUB_{Guid.NewGuid():N}",
            UserId = userId,
            ServiceId = serviceId,
            StartDate = now,
            EndDate = now.AddDays(durationDays),
            Status = "Active",
            CreatedAt = now,
            UpdatedAt = now
        };
        _context.MyService.Add(myService);

        // 2. Tìm account của user
        var account = await _context.Account
            .FirstOrDefaultAsync(a => a.UserId == userId);

        if (account == null)
        {
            _logger.LogWarning("ActivateSubscription: Account for UserId {UserId} not found.", userId);
            return;
        }

        // 3. Gán role Seller nếu là gói nâng cấp
        if (serviceId == "SERVICE_UPGRADE_SELLER")
        {
            var targetRoleName = "Seller";
            var targetRole = await _context.Role
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Name != null && r.Name.ToLower() == targetRoleName.ToLower());

            if (targetRole == null)
            {
                targetRole = await _context.Role.AsNoTracking().FirstOrDefaultAsync(r => r.RoleId == 3);
                if (targetRole == null)
                {
                    targetRole = new Role { RoleId = 3, Name = "Seller" };
                    _context.Role.Add(targetRole);
                    await _context.SaveChangesAsync();
                }
            }

            if (targetRole != null)
            {
                var alreadyHasRole = await _context.AccountRole
                    .AnyAsync(ar => ar.AccountId == account.AccountId && ar.RoleId == targetRole.RoleId);

                if (!alreadyHasRole)
                {
                    _context.AccountRole.Add(new Models.AccountRole
                    {
                        AccountId = account.AccountId,
                        RoleId = targetRole.RoleId,
                        CreatedAt = now
                    });
                    _logger.LogInformation(
                        "ActivateSubscription: Assigned role '{Role}' to AccountId {AccountId}.",
                        targetRole.Name, account.AccountId);
                }
            }
            else
            {
                _logger.LogWarning(
                    "ActivateSubscription: Role '{Role}' not found in database.", targetRoleName);
            }
        }
    }

    private void ValidateSettings()
    {
        if (string.IsNullOrWhiteSpace(_vnPaySettings.TmnCode) ||
            string.IsNullOrWhiteSpace(_vnPaySettings.HashSecret) ||
            string.IsNullOrWhiteSpace(_vnPaySettings.BaseUrl) ||
            string.IsNullOrWhiteSpace(_vnPaySettings.CallbackUrl) ||
            string.IsNullOrWhiteSpace(_vnPaySettings.IpnUrl))
        {
            throw new InvalidOperationException("VNPAY settings are missing. Please update appsettings.");
        }
    }

    private static string BuildQueryString(IEnumerable<KeyValuePair<string, string>> data)
    {
        return string.Join("&", data
            .Where(x => !string.IsNullOrEmpty(x.Value))
            .Select(x => $"{WebUtility.UrlEncode(x.Key)}={WebUtility.UrlEncode(x.Value)}"));
    }

    private static string ComputeHmacSha512(string key, string inputData)
    {
        var keyBytes = Encoding.UTF8.GetBytes(key);
        var inputBytes = Encoding.UTF8.GetBytes(inputData);
        using var hmac = new HMACSHA512(keyBytes);
        var hashBytes = hmac.ComputeHash(inputBytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    private static string NormalizeIpAddress(string ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
        {
            return "127.0.0.1";
        }

        if (ipAddress == "::1")
        {
            return "127.0.0.1";
        }

        return ipAddress;
    }

    private static string MapVnPayMessage(string? responseCode, string? transactionStatus)
    {
        if (responseCode == "00" && transactionStatus == "00")
        {
            return "Payment completed successfully.";
        }

        return responseCode switch
        {
            "07" => "Transaction suspected of fraud.",
            "09" => "Account not eligible for transaction.",
            "10" => "Incorrect card information verification.",
            "11" => "Payment window expired.",
            "12" => "Card or account is locked.",
            "13" => "Incorrect OTP code.",
            "24" => "Customer canceled the transaction.",
            "51" => "Insufficient account balance.",
            "65" => "Transaction limit exceeded.",
            "75" => "Payment bank is under maintenance.",
            "79" => "Incorrect payment password entered too many times.",
            _ => "Payment failed."
        };
    }
}
