using Microsoft.EntityFrameworkCore;
using RetradeBE.Data;
using RetradeBE.Models.DTOs;

namespace RetradeBE.Services;

public class ServiceSubscriptionService : IServiceSubscriptionService
{
    private readonly AppDbContext _context;
    private readonly IPaymentService _paymentService;

    public ServiceSubscriptionService(AppDbContext context, IPaymentService paymentService)
    {
        _context = context;
        _paymentService = paymentService;
    }

    public async Task<IEnumerable<ServiceSubscriptionDto>> GetAvailableAsync()
    {
        return await _context.ServiceSubscription
            .AsNoTracking()
            .OrderByDescending(x => x.ServiceId == "SERVICE_UPGRADE_SELLER")
            .ThenBy(x => x.Price)
            .Select(x => new ServiceSubscriptionDto
            {
                ServiceId = x.ServiceId,
                Name = x.Name ?? string.Empty,
                TargetRole = x.TargetRole ?? string.Empty,
                Price = x.Price ?? 0m,
                DurationDays = x.DurationDays ?? 0,
                BenefitsDescription = x.BenefitsDescription ?? string.Empty
            })
            .ToListAsync();
    }

    public async Task<IEnumerable<MyServiceDto>> GetMyActiveSubscriptionsAsync(string accountId)
    {
        var account = await _context.Account.AsNoTracking().FirstOrDefaultAsync(a => a.AccountId == accountId);
        if (account == null || string.IsNullOrWhiteSpace(account.UserId))
        {
            return Enumerable.Empty<MyServiceDto>();
        }

        var now = DateTime.UtcNow;
        return await _context.MyService
            .AsNoTracking()
            .Where(x => x.UserId == account.UserId && x.Status == "Active" && x.EndDate >= now)
            .Select(x => new MyServiceDto
            {
                ServiceId = x.ServiceId ?? string.Empty,
                UserSubId = x.UserSubId,
                StartDate = x.StartDate,
                EndDate = x.EndDate,
                Status = x.Status ?? string.Empty
            })
            .ToListAsync();
    }

    public async Task<CreateVnPayPaymentResponseDto> CreatePurchasePaymentUrlAsync(string accountId, string serviceId, string ipAddress)
    {
        var service = await _context.ServiceSubscription
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ServiceId == serviceId);

        if (service == null)
        {
            throw new InvalidOperationException("Service package not found.");
        }

        if (!service.Price.HasValue || service.Price.Value <= 0)
        {
            throw new InvalidOperationException("Service package price is invalid.");
        }

        var request = new CreateVnPayPaymentRequestDto
        {
            ServiceId = service.ServiceId,
            Amount = service.Price.Value,
            OrderDescription = $"Thanh toan goi {service.Name} ({service.ServiceId})",
            Locale = "vn"
        };

        return await _paymentService.CreateVnPayPaymentUrlAsync(accountId, request, ipAddress);
    }
}
