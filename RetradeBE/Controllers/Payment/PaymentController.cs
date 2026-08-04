using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetradeBE.Models.DTOs;
using RetradeBE.Services;

namespace RetradeBE.Controllers.Payment;

[Route("api/[controller]")]
[ApiController]
public class PaymentController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PaymentController> _logger;

    public PaymentController(
        IPaymentService paymentService,
        IConfiguration configuration,
        ILogger<PaymentController> logger)
    {
        _paymentService = paymentService;
        _configuration = configuration;
        _logger = logger;
    }

    [Authorize]
    [HttpPost("vnpay/create-payment-url")]
    public async Task<IActionResult> CreateVnPayPaymentUrl([FromBody] CreateVnPayPaymentRequestDto request)
    {
        var accountId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(accountId))
        {
            return Unauthorized();
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
        var callbackUrl = $"{Request.Scheme}://{Request.Host}/api/Payment/vnpay-return";
        var response = await _paymentService.CreateVnPayPaymentUrlAsync(accountId, request, ipAddress, callbackUrl);
        return Ok(response);
    }

    [AllowAnonymous]
    [HttpGet("vnpay-return")]
    public async Task<IActionResult> VnPayReturn()
    {
        var frontendUrl = _configuration["VNPAY:FrontendReturnUrl"];
        VnPayReturnResponseDto result;

        try
        {
            result = await _paymentService.ProcessVnPayCallbackAsync(Request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process VNPAY return callback.");
            result = new VnPayReturnResponseDto
            {
                IsSuccess = false,
                PaymentId = Request.Query.TryGetValue("vnp_TxnRef", out var txnRef) ? txnRef.ToString() : string.Empty,
                Status = "Failed",
                Message = ex.Message,
                TransactionNo = Request.Query.TryGetValue("vnp_TransactionNo", out var transactionNo) ? transactionNo.ToString() : null,
                TransactionStatus = Request.Query.TryGetValue("vnp_TransactionStatus", out var transactionStatus) ? transactionStatus.ToString() : null,
                ResponseCode = Request.Query.TryGetValue("vnp_ResponseCode", out var responseCode) ? responseCode.ToString() : null,
                Amount = TryParseVnPayAmount(Request.Query.TryGetValue("vnp_Amount", out var amount) ? amount.ToString() : null)
            };
        }

        if (string.IsNullOrWhiteSpace(frontendUrl))
        {
            return Ok(result);
        }

        var redirectUrl =
            $"{frontendUrl}?success={result.IsSuccess.ToString().ToLowerInvariant()}" +
            $"&paymentId={Escape(result.PaymentId)}" +
            $"&status={Escape(result.Status)}" +
            $"&message={Escape(result.Message)}" +
            $"&amount={result.Amount.ToString(System.Globalization.CultureInfo.InvariantCulture)}";

        if (!string.IsNullOrWhiteSpace(result.OrderId))
        {
            redirectUrl += $"&orderId={Escape(result.OrderId)}";
        }

        if (!string.IsNullOrWhiteSpace(result.AuctionId))
        {
            redirectUrl += $"&auctionId={Escape(result.AuctionId)}";
        }

        if (!string.IsNullOrWhiteSpace(result.TransactionNo))
        {
            redirectUrl += $"&transactionNo={Escape(result.TransactionNo)}";
        }

        if (!string.IsNullOrWhiteSpace(result.ResponseCode))
        {
            redirectUrl += $"&responseCode={Escape(result.ResponseCode)}";
        }

        return Redirect(redirectUrl);
    }

    [AllowAnonymous]
    [HttpGet("vnpay-ipn")]
    public async Task<IActionResult> VnPayIpn()
    {
        try
        {
            var result = await _paymentService.ProcessVnPayCallbackAsync(Request);

            return Ok(new
            {
                RspCode = result.IsSuccess ? "00" : "01",
                Message = result.IsSuccess ? "Confirm Success" : "Confirm Failed"
            });
        }
        catch
        {
            return Ok(new
            {
                RspCode = "97",
                Message = "Invalid Signature"
            });
        }
    }

    private static string Escape(string? value)
    {
        return Uri.EscapeDataString(value ?? string.Empty);
    }

    private static decimal TryParseVnPayAmount(string? amount)
    {
        return decimal.TryParse(amount, out var parsedAmount) ? parsedAmount / 100m : 0m;
    }
}
