using RetradeBE.Models.DTOs.Gemini;

namespace RetradeBE.Services.GeminiAssistant
{
    public interface IGeminiAssistantApiService
    {
        Task<GeminiGenerateContentResponseDto> GenerateContentAsync(
            IReadOnlyList<GeminiContentDto> contents,
            CancellationToken cancellationToken = default);
    }
}
