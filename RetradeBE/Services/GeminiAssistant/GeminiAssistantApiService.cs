using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using RetradeBE.Config;
using RetradeBE.Models.DTOs.Gemini;

namespace RetradeBE.Services.GeminiAssistant
{
    public class GeminiAssistantApiService : IGeminiAssistantApiService
    {
        private const string SearchProductsFunctionName = "search_products";
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly GeminiSettings _settings;
        private readonly IConfiguration _configuration;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public GeminiAssistantApiService(
            IHttpClientFactory httpClientFactory,
            IOptions<GeminiSettings> settings,
            IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _settings = settings.Value;
            _configuration = configuration;
        }

        public async Task<GeminiGenerateContentResponseDto> GenerateContentAsync(
            IReadOnlyList<GeminiContentDto> contents,
            CancellationToken cancellationToken = default)
        {
            var apiKey = ResolveApiKey();
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new InvalidOperationException("Gemini API key is not configured.");
            }

            var request = new GeminiGenerateContentRequestDto
            {
                SystemInstruction = new GeminiContentDto
                {
                    Parts = new List<GeminiPartDto>
                    {
                        new() { Text = GeminiAssistantSystemPrompt.Value }
                    }
                },
                Contents = contents.ToList(),
                Tools = new List<GeminiToolDto>
                {
                    BuildProductSearchTool()
                },
                ToolConfig = new GeminiToolConfigDto
                {
                    FunctionCallingConfig = new GeminiFunctionCallingConfigDto
                    {
                        Mode = "AUTO"
                    }
                },
                GenerationConfig = new GeminiGenerationConfigDto
                {
                    Temperature = 0.2,
                    MaxOutputTokens = 1200
                }
            };

            var rawBaseUrl = _configuration["Gemini:BaseUrl"] ?? _settings.BaseUrl ?? "https://generativelanguage.googleapis.com/v1beta/models";
            var baseUrl = rawBaseUrl.TrimEnd('/');
            var primaryModel = string.IsNullOrWhiteSpace(_configuration["Gemini:Model"] ?? _settings.Model) ? "gemini-2.5-flash" : (_configuration["Gemini:Model"] ?? _settings.Model!).Trim();
            
            var candidateModels = new List<string> { primaryModel };
            if (!candidateModels.Contains("gemini-2.5-flash")) candidateModels.Add("gemini-2.5-flash");
            if (!candidateModels.Contains("gemini-flash-latest")) candidateModels.Add("gemini-flash-latest");
            if (!candidateModels.Contains("gemini-2.0-flash")) candidateModels.Add("gemini-2.0-flash");

            var payload = JsonSerializer.Serialize(request, JsonOptions);
            var client = _httpClientFactory.CreateClient();
            var maxRetryAttempts = Math.Max(0, _settings.MaxRetryAttempts);
            Exception? lastException = null;

            foreach (var currentModel in candidateModels)
            {
                var url = $"{baseUrl}/{Uri.EscapeDataString(currentModel)}:generateContent";

                for (var attempt = 0; attempt <= maxRetryAttempts; attempt++)
                {
                    try
                    {
                        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                        timeoutCts.CancelAfter(TimeSpan.FromSeconds(Math.Max(5, _settings.RequestTimeoutSeconds)));
                        using var httpRequest = BuildHttpRequest(url, payload, apiKey);
                        using var response = await client.SendAsync(httpRequest, timeoutCts.Token);
                        var body = await response.Content.ReadAsStringAsync(timeoutCts.Token);

                        if (!response.IsSuccessStatusCode)
                        {
                            var message = TryGetGeminiErrorMessage(body) ?? response.ReasonPhrase ?? "Gemini request failed.";
                            lastException = new InvalidOperationException($"Gemini API error ({(int)response.StatusCode}) for model '{currentModel}': {message}");

                            if (IsTransientFailure(response.StatusCode) && attempt < maxRetryAttempts)
                            {
                                await DelayBeforeRetryAsync(attempt, cancellationToken);
                                continue;
                            }

                            // If this model failed with non-retriable error or out of retries, try next candidate model
                            break;
                        }

                        var result = JsonSerializer.Deserialize<GeminiGenerateContentResponseDto>(body, JsonOptions);
                        if (result == null)
                        {
                            throw new InvalidOperationException("Gemini returned an empty response.");
                        }

                        return result;
                    }
                    catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
                    {
                        lastException = ex;
                        if (attempt < maxRetryAttempts)
                        {
                            await DelayBeforeRetryAsync(attempt, cancellationToken);
                        }
                    }
                }
            }

            throw lastException ?? new InvalidOperationException("Gemini request failed across all available models.");
        }

        private string? ResolveApiKey()
        {
            var configKey = _configuration["Gemini:ApiKey"];
            if (!string.IsNullOrWhiteSpace(configKey))
            {
                return configKey.Trim();
            }

            var envKey = _configuration["GEMINI_API_KEY"] ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY");
            if (!string.IsNullOrWhiteSpace(envKey))
            {
                return envKey.Trim();
            }

            return !string.IsNullOrWhiteSpace(_settings.ApiKey) ? _settings.ApiKey.Trim() : null;
        }

        private static HttpRequestMessage BuildHttpRequest(string url, string payload, string apiKey)
        {
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
            httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpRequest.Headers.Add("x-goog-api-key", apiKey);
            httpRequest.Content = new StringContent(payload, Encoding.UTF8, "application/json");
            return httpRequest;
        }

        private static bool IsTransientFailure(System.Net.HttpStatusCode statusCode)
        {
            var status = (int)statusCode;
            return status == 429 || status >= 500;
        }

        private static Task DelayBeforeRetryAsync(int attempt, CancellationToken cancellationToken)
        {
            var delay = TimeSpan.FromMilliseconds(400 * Math.Pow(2, attempt));
            return Task.Delay(delay, cancellationToken);
        }

        private static GeminiToolDto BuildProductSearchTool()
        {
            return new GeminiToolDto
            {
                FunctionDeclarations = new List<GeminiFunctionDeclarationDto>
                {
                    new()
                    {
                        Name = SearchProductsFunctionName,
                        Description = "Search real ReTrade products from the database. Use this before recommending, comparing, listing, or naming any product.",
                        Parameters = new GeminiSchemaDto
                        {
                            Type = "object",
                            Properties = new Dictionary<string, GeminiSchemaDto>
                            {
                                ["keyword"] = new()
                                {
                                    Type = "string",
                                    Description = "Product keyword or natural-language phrase, for example phone, laptop, keyboard, shirt."
                                },
                                ["category"] = new()
                                {
                                    Type = "string",
                                    Description = "Optional category name mentioned by the user."
                                },
                                ["minPrice"] = new()
                                {
                                    Type = "number",
                                    Description = "Minimum price in VND."
                                },
                                ["maxPrice"] = new()
                                {
                                    Type = "number",
                                    Description = "Maximum price in VND."
                                },
                                ["condition"] = new()
                                {
                                    Type = "string",
                                    Description = "Optional condition such as New, LikeNew, Used."
                                },
                                ["limit"] = new()
                                {
                                    Type = "integer",
                                    Description = "Maximum number of products to return. Use 5 by default and never exceed 10."
                                },
                                ["minStorageGb"] = new()
                                {
                                    Type = "integer",
                                    Description = "Minimum storage capacity in GB when the user asks for phone/computer storage, for example 64 for '64GB trở lên'."
                                }
                            }
                        }
                    }
                }
            };
        }

        private static string? TryGetGeminiErrorMessage(string body)
        {
            try
            {
                var error = JsonSerializer.Deserialize<GeminiErrorResponseDto>(body, JsonOptions);
                return error?.Error?.Message;
            }
            catch (JsonException)
            {
                return null;
            }
        }
    }
}
