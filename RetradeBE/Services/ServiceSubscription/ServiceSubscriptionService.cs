using Microsoft.EntityFrameworkCore;
using RetradeBE.Models.DTOs;
using RetradeBE.Repositories;

namespace RetradeBE.Services;

public class ServiceSubscriptionService : IServiceSubscriptionService
{
    private readonly IServiceSubscriptionRepository _serviceSubscriptionRepo;
    private readonly IMyServiceRepository _myServiceRepo;
    private readonly IAccountRepository _accountRepo;
    private readonly IPaymentService _paymentService;

    public ServiceSubscriptionService(
        IServiceSubscriptionRepository serviceSubscriptionRepo,
        IMyServiceRepository myServiceRepo,
        IAccountRepository accountRepo,
        IPaymentService paymentService)
    {
        _serviceSubscriptionRepo = serviceSubscriptionRepo;
        _myServiceRepo = myServiceRepo;
        _accountRepo = accountRepo;
        _paymentService = paymentService;
    }

    public async Task<IEnumerable<ServiceSubscriptionDto>> GetAvailableAsync()
    {
        return await _serviceSubscriptionRepo.Query()
            .AsNoTracking()
            .OrderByDescending(x => x.ServiceId == "SERVICE_UPGRADE_SELLER" || x.ServiceId == "sub_20260701_100001")
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
        var account = await _accountRepo.Query().AsNoTracking().FirstOrDefaultAsync(a => a.AccountId == accountId);
        if (account == null || string.IsNullOrWhiteSpace(account.UserId))
        {
            return Enumerable.Empty<MyServiceDto>();
        }

        var now = DateTime.UtcNow;
        return await _myServiceRepo.Query()
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
        var service = await _serviceSubscriptionRepo.Query()
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
            OrderDescription = $"Payment for package {service.Name} ({service.ServiceId})",
            Locale = "vn"
        };

        return await _paymentService.CreateVnPayPaymentUrlAsync(accountId, request, ipAddress);
    }
}
