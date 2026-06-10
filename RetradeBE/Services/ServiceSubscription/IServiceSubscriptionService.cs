using RetradeBE.Models.DTOs;

namespace RetradeBE.Services;

public interface IServiceSubscriptionService
{
    Task<IEnumerable<ServiceSubscriptionDto>> GetAvailableAsync();
    
    Task<IEnumerable<MyServiceDto>> GetMyActiveSubscriptionsAsync(string accountId);

    Task<CreateVnPayPaymentResponseDto> CreatePurchasePaymentUrlAsync(string accountId, string serviceId, string ipAddress);
}
