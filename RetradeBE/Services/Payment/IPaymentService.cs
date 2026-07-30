using RetradeBE.Models.DTOs;

namespace RetradeBE.Services;

public interface IPaymentService
{
    Task<CreateVnPayPaymentResponseDto> CreateVnPayPaymentUrlAsync(string accountId, CreateVnPayPaymentRequestDto request, string ipAddress, string? overrideCallbackUrl = null);

    Task<VnPayReturnResponseDto> ProcessVnPayCallbackAsync(HttpRequest request);
}
