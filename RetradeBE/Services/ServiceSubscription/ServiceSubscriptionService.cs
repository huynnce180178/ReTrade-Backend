using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using RetradeBE.Hubs;
using RetradeBE.Models;
using RetradeBE.Models.DTOs;
using RetradeBE.Models.Enums;
using RetradeBE.Repositories;
using RetradeBE.Repositories.AccountRole;

namespace RetradeBE.Services;

public class ServiceSubscriptionService : IServiceSubscriptionService
{
    private readonly IServiceSubscriptionRepository _serviceSubscriptionRepo;
    private readonly IMyServiceRepository _myServiceRepo;
    private readonly IAccountRepository _accountRepo;
    private readonly IAccountRoleRepository _accountRoleRepo;
    private readonly IPaymentService _paymentService;
    private readonly IHubContext<AccountHub> _accountHub;
    private readonly INotificationService _notificationService;

    public ServiceSubscriptionService(
        IServiceSubscriptionRepository serviceSubscriptionRepo,
        IMyServiceRepository myServiceRepo,
        IAccountRepository accountRepo,
        IAccountRoleRepository accountRoleRepo,
        IPaymentService paymentService,
        IHubContext<AccountHub> accountHub,
        INotificationService notificationService)
    {
        _serviceSubscriptionRepo = serviceSubscriptionRepo;
        _myServiceRepo = myServiceRepo;
        _accountRepo = accountRepo;
        _accountRoleRepo = accountRoleRepo;
        _paymentService = paymentService;
        _accountHub = accountHub;
        _notificationService = notificationService;
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

    public async Task<CreateVnPayPaymentResponseDto> CreatePurchasePaymentUrlAsync(string accountId, string serviceId, string ipAddress, string? overrideCallbackUrl = null)
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

        return await _paymentService.CreateVnPayPaymentUrlAsync(accountId, request, ipAddress, overrideCallbackUrl);
    }

    public async Task<bool> GrantAdminSellerSubscriptionAsync(string accountId)
    {
        var account = await _accountRepo.GetByIdAsync(accountId);
        if (account == null || string.IsNullOrWhiteSpace(account.UserId))
        {
            return false;
        }

        var sellerRoleName = RoleEnum.Seller.ToString();
        var allRoles = await _accountRoleRepo.GetAllRolesAsync();
        var sellerRole = allRoles.FirstOrDefault(r => string.Equals(r.Name, sellerRoleName, StringComparison.OrdinalIgnoreCase));
        if (sellerRole != null)
        {
            var userRoles = await _accountRoleRepo.GetRolesByAccountIdAsync(accountId);
            if (!userRoles.Any(r => r.RoleId == sellerRole.RoleId))
            {
                await _accountRoleRepo.AssignRoleAsync(accountId, sellerRole.RoleId);
            }
        }

        var sellerPackage = await _serviceSubscriptionRepo.Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ServiceId == "SERVICE_UPGRADE_SELLER" || x.ServiceId == "sub_20260701_100001" || x.TargetRole == "Seller");

        var serviceId = sellerPackage?.ServiceId ?? "SERVICE_UPGRADE_SELLER";
        var now = DateTime.UtcNow;

        var existingSub = await _myServiceRepo.Query()
            .FirstOrDefaultAsync(x => x.UserId == account.UserId && (x.ServiceId == serviceId || x.Status == "Active"));

        if (existingSub != null)
        {
            existingSub.Status = "Active";
            existingSub.StartDate = now;
            existingSub.EndDate = now.AddYears(100); // Admin unlimited
            existingSub.UpdatedAt = now;
            await _myServiceRepo.UpdateAsync(existingSub);
        }
        else
        {
            var newSub = new MyService
            {
                UserSubId = Utils.IdGenerator.GenerateId("usub"),
                UserId = account.UserId,
                ServiceId = serviceId,
                StartDate = now,
                EndDate = now.AddYears(100), // Admin unlimited
                Status = "Active",
                CreatedAt = now
            };
            await _myServiceRepo.AddAsync(newSub);
        }

        try
        {
            await _notificationService.CreateAndSendAsync(new CreateNotificationDto
            {
                UserId = account.UserId,
                Title = "Nâng cấp gói Seller thành công",
                Message = "Tài khoản của bạn đã được quản trị viên cấp gói nâng cấp Seller không giới hạn.",
                Type = "System",
                ReferenceId = accountId
            });
        }
        catch
        {
            // Non-blocking notification error handling
        }

        await _accountHub.Clients
            .Group(AccountHub.GetAccountGroupName(accountId))
            .SendAsync("ForceLogout", "Tài khoản của bạn đã được Quản trị viên nâng cấp gói Seller không giới hạn. Hệ thống tự động đăng xuất.");

        return true;
    }

    public async Task<bool> RevokeSellerSubscriptionAsync(string accountId)
    {
        var account = await _accountRepo.GetByIdAsync(accountId);
        if (account == null || string.IsNullOrWhiteSpace(account.UserId))
        {
            return false;
        }

        var sellerRoleName = RoleEnum.Seller.ToString();
        var allRoles = await _accountRoleRepo.GetAllRolesAsync();
        var sellerRole = allRoles.FirstOrDefault(r => string.Equals(r.Name, sellerRoleName, StringComparison.OrdinalIgnoreCase));
        if (sellerRole != null)
        {
            await _accountRoleRepo.RemoveRoleAsync(accountId, sellerRole.RoleId);
        }

        var activeSubs = await _myServiceRepo.Query()
            .Where(x => x.UserId == account.UserId && x.Status == "Active")
            .ToListAsync();

        foreach (var sub in activeSubs)
        {
            sub.Status = "Expired";
            sub.UpdatedAt = DateTime.UtcNow;
            await _myServiceRepo.UpdateAsync(sub);
        }

        try
        {
            await _notificationService.CreateAndSendAsync(new CreateNotificationDto
            {
                UserId = account.UserId,
                Title = "Hủy gói Seller",
                Message = "Gói đăng ký Seller và quyền Người bán của bạn đã được quản trị viên thu hồi.",
                Type = "System",
                ReferenceId = accountId
            });
        }
        catch
        {
            // Non-blocking notification error handling
        }

        await _accountHub.Clients
            .Group(AccountHub.GetAccountGroupName(accountId))
            .SendAsync("ForceLogout", "Gói đăng ký Seller và quyền Người bán của bạn đã bị Quản trị viên thu hồi. Hệ thống tự động đăng xuất.");

        return true;
    }
}
