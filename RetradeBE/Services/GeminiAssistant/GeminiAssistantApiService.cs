using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
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

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public GeminiAssistantApiService(IHttpClientFactory httpClientFactory, IOptions<GeminiSettings> settings)
        {
            _httpClientFactory = httpClientFactory;
            _settings = settings.Value;
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
                        Mode = "AUTO",
                        AllowedFunctionNames = new List<string> { SearchProductsFunctionName }
                    }
                },
                GenerationConfig = new GeminiGenerationConfigDto
                {
                    Temperature = 0.2,
                    MaxOutputTokens = 1200
                }
            };

            var baseUrl = (_settings.BaseUrl ?? string.Empty).TrimEnd('/');
            var model = string.IsNullOrWhiteSpace(_settings.Model) ? "gemini-2.0-flash" : _settings.Model.Trim();
            var url = $"{baseUrl}/{Uri.EscapeDataString(model)}:generateContent";
            var payload = JsonSerializer.Serialize(request, JsonOptions);
            var client = _httpClientFactory.CreateClient();
            var maxRetryAttempts = Math.Max(0, _settings.MaxRetryAttempts);

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
                        if (IsTransientFailure(response.StatusCode) && attempt < maxRetryAttempts)
                        {
                            await DelayBeforeRetryAsync(attempt, cancellationToken);
                            continue;
                        }

                        var message = TryGetGeminiErrorMessage(body) ?? response.ReasonPhrase ?? "Gemini request failed.";
                        throw new InvalidOperationException($"Gemini API error ({(int)response.StatusCode}): {message}");
                    }

                    var result = JsonSerializer.Deserialize<GeminiGenerateContentResponseDto>(body, JsonOptions);
                    if (result == null)
                    {
                        throw new InvalidOperationException("Gemini returned an empty response.");
                    }

                    return result;
                }
                catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested && attempt < maxRetryAttempts)
                {
                    await DelayBeforeRetryAsync(attempt, cancellationToken);
                }
                catch (HttpRequestException) when (attempt < maxRetryAttempts)
                {
                    await DelayBeforeRetryAsync(attempt, cancellationToken);
                }
            }

            throw new InvalidOperationException("Gemini request timed out or could not connect.");
        }

        private string? ResolveApiKey()
        {
            return !string.IsNullOrWhiteSpace(_settings.ApiKey)
                ? _settings.ApiKey
                : Environment.GetEnvironmentVariable("GEMINI_API_KEY");
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
