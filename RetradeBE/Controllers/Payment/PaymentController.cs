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

    public PaymentController(IPaymentService paymentService, IConfiguration configuration)
    {
        _paymentService = paymentService;
        _configuration = configuration;
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
            result = new VnPayReturnResponseDto
            {
                IsSuccess = false,
                Status = "Failed",
                Message = ex.Message
            };
        }

        if (string.IsNullOrWhiteSpace(frontendUrl))
        {
            return Ok(result);
        }

        var redirectUrl =
            $"{frontendUrl}?success={result.IsSuccess.ToString().ToLowerInvariant()}" +
            $"&paymentId={Uri.EscapeDataString(result.PaymentId)}" +
            $"&status={Uri.EscapeDataString(result.Status)}" +
            $"&message={Uri.EscapeDataString(result.Message)}" +
            $"&amount={result.Amount.ToString(System.Globalization.CultureInfo.InvariantCulture)}";

        if (!string.IsNullOrWhiteSpace(result.OrderId))
        {
            redirectUrl += $"&orderId={Uri.EscapeDataString(result.OrderId)}";
        }

        if (!string.IsNullOrWhiteSpace(result.AuctionId))
        {
            redirectUrl += $"&auctionId={Uri.EscapeDataString(result.AuctionId)}";
        }

        if (!string.IsNullOrWhiteSpace(result.TransactionNo))
        {
            redirectUrl += $"&transactionNo={Uri.EscapeDataString(result.TransactionNo)}";
        }

        if (!string.IsNullOrWhiteSpace(result.ResponseCode))
        {
            redirectUrl += $"&responseCode={Uri.EscapeDataString(result.ResponseCode)}";
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
}
