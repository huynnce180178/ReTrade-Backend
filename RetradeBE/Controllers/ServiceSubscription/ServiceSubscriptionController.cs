using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetradeBE.Services;

namespace RetradeBE.Controllers.ServiceSubscription;

[Route("api/[controller]")]
[ApiController]
public class ServiceSubscriptionController : ControllerBase
{
    private readonly IServiceSubscriptionService _serviceSubscriptionService;

    public ServiceSubscriptionController(IServiceSubscriptionService serviceSubscriptionService)
    {
        _serviceSubscriptionService = serviceSubscriptionService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAvailable()
    {
        var packages = await _serviceSubscriptionService.GetAvailableAsync();
        return Ok(packages);
    }

    [Authorize]
    [HttpGet("my")]
    public async Task<IActionResult> GetMyActiveSubscriptions()
    {
        var accountId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(accountId))
        {
            return Unauthorized();
        }

        var mySubscriptions = await _serviceSubscriptionService.GetMyActiveSubscriptionsAsync(accountId);
        return Ok(mySubscriptions);
    }

    [Authorize]
    [HttpPost("{serviceId}/purchase")]
    public async Task<IActionResult> Purchase(string serviceId)
    {
        var accountId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(accountId))
        {
            return Unauthorized();
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
        var callbackUrl = $"{Request.Scheme}://{Request.Host}/api/Payment/vnpay-return";
        var response = await _serviceSubscriptionService.CreatePurchasePaymentUrlAsync(accountId, serviceId, ipAddress, callbackUrl);
        return Ok(response);
    }
}
