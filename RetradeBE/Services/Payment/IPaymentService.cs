using RetradeBE.Models.DTOs;

namespace RetradeBE.Services;

public interface IPaymentService
{
    Task<CreateVnPayPaymentResponseDto> CreateVnPayPaymentUrlAsync(string accountId, CreateVnPayPaymentRequestDto request, string ipAddress);

    Task<VnPayReturnResponseDto> ProcessVnPayCallbackAsync(HttpRequest request);
}
