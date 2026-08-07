using System.Text.Json;
using System.Text.Json.Serialization;

namespace RetradeBE.Models.DTOs.Gemini
{
    public class GeminiGenerateContentRequestDto
    {
        public List<GeminiContentDto> Contents { get; set; } = new();
        public GeminiContentDto? SystemInstruction { get; set; }
        public List<GeminiToolDto>? Tools { get; set; }
        public GeminiToolConfigDto? ToolConfig { get; set; }
        public GeminiGenerationConfigDto? GenerationConfig { get; set; }
    }

    public class GeminiGenerateContentResponseDto
    {
        public List<GeminiCandidateDto>? Candidates { get; set; }
        public GeminiPromptFeedbackDto? PromptFeedback { get; set; }
    }

    public class GeminiCandidateDto
    {
        public GeminiContentDto? Content { get; set; }
        public string? FinishReason { get; set; }
    }

    public class GeminiPromptFeedbackDto
    {
        public string? BlockReason { get; set; }
    }

    public class GeminiContentDto
    {
        public string? Role { get; set; }
        public List<GeminiPartDto> Parts { get; set; } = new();
    }

    public class GeminiPartDto
    {
        public string? Text { get; set; }
        public GeminiInlineDataDto? InlineData { get; set; }
        public GeminiFunctionCallDto? FunctionCall { get; set; }
        public GeminiFunctionResponseDto? FunctionResponse { get; set; }
    }

    public class GeminiInlineDataDto
    {
        [JsonPropertyName("mimeType")]
        public string MimeType { get; set; } = "image/jpeg";

        [JsonPropertyName("data")]
        public string Data { get; set; } = null!;
    }

    public class GeminiFunctionCallDto
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public Dictionary<string, JsonElement>? Args { get; set; }
    }

    public class GeminiFunctionResponseDto
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public object? Response { get; set; }
    }

    public class GeminiToolDto
    {
        public List<GeminiFunctionDeclarationDto> FunctionDeclarations { get; set; } = new();
    }

    public class GeminiFunctionDeclarationDto
    {
        public string Name { get; set; } = null!;
        public string Description { get; set; } = null!;
        public GeminiSchemaDto Parameters { get; set; } = new();
    }

    public class GeminiSchemaDto
    {
        public string Type { get; set; } = null!;
        public string? Description { get; set; }
        public Dictionary<string, GeminiSchemaDto>? Properties { get; set; }
        public List<string>? Required { get; set; }
        public List<string>? Enum { get; set; }
    }

    public class GeminiToolConfigDto
    {
        public GeminiFunctionCallingConfigDto? FunctionCallingConfig { get; set; }
    }

    public class GeminiFunctionCallingConfigDto
    {
        public string Mode { get; set; } = "AUTO";
        public List<string>? AllowedFunctionNames { get; set; }
    }

    public class GeminiGenerationConfigDto
    {
        public double Temperature { get; set; } = 0.2;
        public int MaxOutputTokens { get; set; } = 1024;
    }

    public class GeminiErrorResponseDto
    {
        public GeminiErrorDto? Error { get; set; }
    }

    public class GeminiErrorDto
    {
        public int Code { get; set; }
        public string? Message { get; set; }
        public string? Status { get; set; }
    }
}
