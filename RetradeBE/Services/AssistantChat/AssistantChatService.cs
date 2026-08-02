using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RetradeBE.Models;
using RetradeBE.Models.DTOs.AssistantChat;
using RetradeBE.Models.DTOs.Gemini;
using RetradeBE.Models.Enums;
using RetradeBE.Repositories;
using RetradeBE.Services.GeminiAssistant;
using RetradeBE.Services;

namespace RetradeBE.Services.AssistantChat
{
    public class AssistantChatService : IAssistantChatService
    {
        private const string UserRole = "user";
        private const string ModelRole = "model";
        private const string FunctionRole = "function";
        private const string SearchProductsFunctionName = "search_products";

        private readonly IAssistantChatSessionRepository _chatSessionRepository;
        private readonly IAssistantChatMessageRepository _chatMessageRepository;
        private readonly IAccountRepository _accountRepository;
        private readonly IProductRepository _productRepository;
        private readonly IPurchaseService _purchaseService;
        private readonly IGeminiAssistantApiService _geminiApiService;
        private readonly ILogger<AssistantChatService> _logger;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public AssistantChatService(
            IAssistantChatSessionRepository chatSessionRepository,
            IAssistantChatMessageRepository chatMessageRepository,
            IAccountRepository accountRepository,
            IProductRepository productRepository,
            IPurchaseService purchaseService,
            IGeminiAssistantApiService geminiApiService,
            ILogger<AssistantChatService> logger)
        {
            _chatSessionRepository = chatSessionRepository;
            _chatMessageRepository = chatMessageRepository;
            _accountRepository = accountRepository;
            _productRepository = productRepository;
            _purchaseService = purchaseService;
            _geminiApiService = geminiApiService;
            _logger = logger;
        }

        public async Task<AssistantChatResponseDto> SendChatAssistantAsync(
            string? accountId,
            AssistantChatRequestDto request,
            CancellationToken cancellationToken = default)
        {
            var userId = await ResolveUserIdAsync(accountId);
            var message = (request.Message ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(message))
            {
                throw new ArgumentException("Message is required.");
            }

            if (message.Length > 2000)
            {
                throw new ArgumentException("Message is too long.");
            }

            var now = DateTime.UtcNow;
            var session = await GetOrCreateSessionAsync(userId, request.SessionId, message, now);

            await _chatMessageRepository.AddAsync(new ChatMessage
            {
                MessageId = RetradeBE.Utils.IdGenerator.GenerateId("amsg"),
                SessionId = session.SessionId,
                Role = UserRole,
                Content = message,
                CreatedAt = now
            });

            session.LastMessageAt = now;
            await _chatSessionRepository.UpdateAsync(session);

            var history = await _chatMessageRepository.GetBySessionIdAsync(session.SessionId);
            var geminiContents = BuildGeminiContents(history);
            var orderProducts = await InjectUserOrderContextAsync(userId, geminiContents, cancellationToken);
            var suggestedProducts = new List<AssistantProductSuggestionDto>();
            if (orderProducts != null && orderProducts.Count > 0)
            {
                suggestedProducts.AddRange(orderProducts);
            }
            string finalText;

            try
            {
                finalText = await GenerateGeminiResponseAsync(geminiContents, suggestedProducts, session.SessionId, cancellationToken);
            }
            catch (InvalidOperationException ex) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "Gemini assistant chat request failed.");
                finalText = await BuildOfflineAssistantResponseAsync(message, userId, suggestedProducts, cancellationToken);
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Assistant chat request failed.");
                finalText = "i18n:chat.assistant_error_unavailable";
            }
            if (string.IsNullOrWhiteSpace(finalText))
            {
                finalText = "i18n:chat.assistant_offline_general";
            }

            var assistantMessage = new ChatMessage
            {
                MessageId = RetradeBE.Utils.IdGenerator.GenerateId("amsg"),
                SessionId = session.SessionId,
                Role = ModelRole,
                Content = finalText,
                CreatedAt = DateTime.UtcNow
            };

            await _chatMessageRepository.AddAsync(assistantMessage);
            session.LastMessageAt = assistantMessage.CreatedAt;
            await _chatSessionRepository.UpdateAsync(session);

            return new AssistantChatResponseDto
            {
                SessionId = session.SessionId,
                MessageId = assistantMessage.MessageId,
                Role = ModelRole,
                Content = finalText,
                CreatedAt = assistantMessage.CreatedAt,
                Products = suggestedProducts
                    .GroupBy(p => p.ProductId)
                    .Select(g => g.First())
                    .ToList()
            };
        }

        public async Task<List<AssistantChatMessageDto>> GetSessionHistoryAsync(string? accountId, string sessionId)
        {
            var userId = await ResolveUserIdAsync(accountId);
            var session = await _chatSessionRepository.GetOwnedSessionAsync(userId, sessionId);
            if (session == null)
            {
                throw new KeyNotFoundException("Assistant chat session not found.");
            }

            return session.ChatMessage
                .Where(m => m.Role != FunctionRole)
                .OrderBy(m => m.CreatedAt)
                .Select(m => new AssistantChatMessageDto
                {
                    MessageId = m.MessageId,
                    SessionId = m.SessionId,
                    Role = m.Role,
                    Content = m.Content,
                    CreatedAt = m.CreatedAt
                })
                .ToList();
        }

        private async Task<ChatSession> GetOrCreateSessionAsync(string? userId, string? sessionId, string firstMessage, DateTime now)
        {
            if (!string.IsNullOrWhiteSpace(sessionId))
            {
                var existing = await _chatSessionRepository.GetOwnedSessionAsync(userId, sessionId);
                if (existing != null)
                {
                    return existing;
                }
            }

            var session = new ChatSession
            {
                SessionId = RetradeBE.Utils.IdGenerator.GenerateId("ases"),
                UserId = userId,
                Title = firstMessage.Length > 80 ? firstMessage[..80] : firstMessage,
                StartedAt = now,
                LastMessageAt = now,
                IsActive = true
            };

            await _chatSessionRepository.AddAsync(session);
            return session;
        }

        private async Task<string?> ResolveUserIdAsync(string? accountId)
        {
            if (string.IsNullOrWhiteSpace(accountId))
            {
                return null;
            }

            var account = await _accountRepository.GetByIdAsync(accountId);
            if (account == null || string.IsNullOrWhiteSpace(account.UserId))
            {
                throw new UnauthorizedAccessException("Account is not linked to a user.");
            }

            return account.UserId;
        }

        private async Task<string> GenerateGeminiResponseAsync(
            List<GeminiContentDto> contents,
            List<AssistantProductSuggestionDto> suggestedProducts,
            string sessionId,
            CancellationToken cancellationToken)
        {
            const int maxFunctionRounds = 3;

            for (var round = 0; round <= maxFunctionRounds; round++)
            {
                var response = await _geminiApiService.GenerateContentAsync(contents, cancellationToken);
                var functionCalls = ExtractSearchProductFunctionCalls(response).ToList();

                if (functionCalls.Count == 0)
                {
                    return ExtractText(response);
                }

                var modelContent = response.Candidates?.FirstOrDefault()?.Content;
                if (modelContent != null)
                {
                    contents.Add(modelContent);
                }

                var functionResponseParts = new List<GeminiPartDto>();
                foreach (var functionCall in functionCalls)
                {
                    var args = ProductSearchToolArgs.FromGeminiArgs(functionCall.Args);
                    var products = await SearchProductsAsync(args, cancellationToken);
                    suggestedProducts.AddRange(products);

                    var responsePayload = new
                    {
                        products,
                        count = products.Count,
                        rule = "Only these database products are eligible to mention. If count is 0, say no matching products were found."
                    };

                    functionResponseParts.Add(new GeminiPartDto
                    {
                        FunctionResponse = new GeminiFunctionResponseDto
                        {
                            Id = functionCall.Id,
                            Name = SearchProductsFunctionName,
                            Response = responsePayload
                        }
                    });

                    await _chatMessageRepository.AddAsync(new ChatMessage
                    {
                        MessageId = RetradeBE.Utils.IdGenerator.GenerateId("amsg"),
                        SessionId = sessionId,
                        Role = FunctionRole,
                        FunctionName = SearchProductsFunctionName,
                        FunctionCallId = functionCall.Id,
                        Content = JsonSerializer.Serialize(new { args, products }, JsonOptions),
                        CreatedAt = DateTime.UtcNow
                    });
                }

                contents.Add(new GeminiContentDto
                {
                    Role = UserRole,
                    Parts = functionResponseParts
                });
            }

            return "Minh da tim san pham theo yeu cau nhung chua tao duoc cau tra loi cuoi. Ban thu hoi lai ngan gon hon nhe.";
        }

        private static List<GeminiContentDto> BuildGeminiContents(List<ChatMessage> history)
        {
            return history
                .Where(m => m.Role == UserRole || m.Role == ModelRole)
                .OrderBy(m => m.CreatedAt)
                .TakeLast(30)
                .Select(m => new GeminiContentDto
                {
                    Role = m.Role == ModelRole ? ModelRole : UserRole,
                    Parts = new List<GeminiPartDto>
                    {
                        new() { Text = m.Content ?? string.Empty }
                    }
                })
                .ToList();
        }

        private static IEnumerable<GeminiFunctionCallDto> ExtractSearchProductFunctionCalls(GeminiGenerateContentResponseDto response)
        {
            var parts = response.Candidates?.FirstOrDefault()?.Content?.Parts;
            if (parts == null)
            {
                yield break;
            }

            foreach (var part in parts)
            {
                var functionCall = part.FunctionCall;
                if (functionCall != null &&
                    string.Equals(functionCall.Name, SearchProductsFunctionName, StringComparison.OrdinalIgnoreCase))
                {
                    yield return functionCall;
                }
            }
        }

        private static string ExtractText(GeminiGenerateContentResponseDto response)
        {
            var parts = response.Candidates?.FirstOrDefault()?.Content?.Parts;
            if (parts == null)
            {
                return string.Empty;
            }

            return string.Join(
                    string.Empty,
                    parts
                        .Select(p => p.Text)
                        .Where(text => !string.IsNullOrWhiteSpace(text)))
                .Trim();
        }

        private async Task<string> BuildOfflineAssistantResponseAsync(
            string message,
            string? userId,
            List<AssistantProductSuggestionDto> suggestedProducts,
            CancellationToken cancellationToken)
        {
            var normalized = NormalizeForMatch(message);

            if (ContainsAny(normalized, "purchase history", "order history", "my orders", "lich su mua", "don hang", "mua hang"))
            {
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return "i18n:chat.assistant_offline_purchase_login";
                }

                return suggestedProducts.Count > 0
                    ? "i18n:chat.assistant_offline_purchase_found"
                    : "i18n:chat.assistant_offline_purchase_empty";
            }

            if (ContainsAny(normalized, "auction", "bid", "dau gia", "tra gia"))
            {
                return "i18n:chat.assistant_offline_auction_help";
            }

            if (ContainsAny(normalized, "sell", "selling", "post product", "list product", "dang ban", "ban san pham", "rao ban"))
            {
                return "i18n:chat.assistant_offline_selling_help";
            }

            if (ContainsAny(normalized, "wishlist", "favorite", "favourite", "yeu thich"))
            {
                return "i18n:chat.assistant_offline_wishlist_help";
            }

            if (ContainsAny(
                normalized,
                "product",
                "products",
                "featured",
                "latest",
                "search",
                "find",
                "san pham",
                "noi bat",
                "moi nhat",
                "tim",
                "iphone",
                "phone",
                "laptop",
                "macbook",
                "camera",
                "computer",
                "clothing",
                "sneaker",
                "vespa"))
            {
                var products = await SearchProductsAsync(new ProductSearchToolArgs { Limit = 5 }, cancellationToken);
                AddDistinctProducts(suggestedProducts, products);
                return products.Count > 0
                    ? "i18n:chat.assistant_offline_products"
                    : "i18n:chat.assistant_offline_no_products";
            }

            return "i18n:chat.assistant_offline_general";
        }

        private static void AddDistinctProducts(
            List<AssistantProductSuggestionDto> target,
            IEnumerable<AssistantProductSuggestionDto> products)
        {
            foreach (var product in products)
            {
                if (!target.Any(current => current.ProductId == product.ProductId))
                {
                    target.Add(product);
                }
            }
        }

        private static bool ContainsAny(string value, params string[] patterns)
        {
            return patterns.Any(pattern => value.Contains(pattern, StringComparison.OrdinalIgnoreCase));
        }

        private static string NormalizeForMatch(string value)
        {
            var normalized = (value ?? string.Empty).Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder(normalized.Length);

            foreach (var character in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
                {
                    builder.Append(character switch
                    {
                        'đ' or 'Đ' => 'd',
                        _ => char.ToLowerInvariant(character)
                    });
                }
            }

            return builder.ToString().Normalize(NormalizationForm.FormC);
        }

        private static string BuildGeminiFailureMessage(InvalidOperationException exception)
        {
            var message = exception.Message;
            var normalized = message.ToLowerInvariant();

            if (normalized.Contains("api key is not configured"))
            {
                return "i18n:chat.assistant_error_key_missing";
            }

            if (normalized.Contains("api key not valid") ||
                normalized.Contains("api_key_invalid") ||
                normalized.Contains("invalid api key") ||
                normalized.Contains("unauthenticated") ||
                normalized.Contains("access_token_type_unsupported") ||
                normalized.Contains("401"))
            {
                return "i18n:chat.assistant_error_key_invalid";
            }

            if (normalized.Contains("permission") || normalized.Contains("forbidden") || normalized.Contains("403"))
            {
                return "i18n:chat.assistant_error_permission";
            }

            if (normalized.Contains("quota") || normalized.Contains("429"))
            {
                return "i18n:chat.assistant_error_quota";
            }

            if (normalized.Contains("models/") && (normalized.Contains("not found") || normalized.Contains("404")))
            {
                return "i18n:chat.assistant_error_model";
            }

            return "i18n:chat.assistant_error_unavailable";
        }

        private async Task<List<AssistantProductSuggestionDto>> InjectUserOrderContextAsync(
            string? userId,
            List<GeminiContentDto> geminiContents,
            CancellationToken cancellationToken)
        {
            var resultProducts = new List<AssistantProductSuggestionDto>();
            if (string.IsNullOrWhiteSpace(userId))
            {
                return resultProducts;
            }

            try
            {
                var recentOrders = await _purchaseService.QueryByBuyerId(userId)
                    .Take(5)
                    .ToListAsync(cancellationToken);

                if (recentOrders.Count == 0)
                {
                    geminiContents.Insert(0, new GeminiContentDto
                    {
                        Role = UserRole,
                        Parts = new List<GeminiPartDto>
                        {
                            new() { Text = "[System Context]: Current user has no orders placed on ReTrade." }
                        }
                    });
                    return resultProducts;
                }

                var productIds = recentOrders
                    .Select(o => o.ProductId)
                    .Where(id => !string.IsNullOrWhiteSpace(id))
                    .Distinct()
                    .ToList();

                var productsDict = await _productRepository.Query()
                    .AsNoTracking()
                    .Where(p => productIds.Contains(p.ProductId))
                    .Select(p => new AssistantProductSuggestionDto
                    {
                        ProductId = p.ProductId,
                        Name = p.Name,
                        CategoryName = p.Category != null ? p.Category.Name : null,
                        Price = p.Price,
                        StockQuantity = p.StockQuantity,
                        Status = p.Status,
                        Condition = p.Condition,
                        SellerId = p.SellerId,
                        SellerName = p.Seller != null ? $"{p.Seller.FirstName} {p.Seller.LastName}".Trim() : null,
                        MainImageUrl = p.ProductImage
                            .Where(pi => pi.IsMain == true)
                            .Select(pi => pi.Image.ImageUrl)
                            .FirstOrDefault()
                            ?? p.ProductImage
                                .OrderBy(pi => pi.SortOrder)
                                .Select(pi => pi.Image.ImageUrl)
                                .FirstOrDefault()
                    })
                    .ToDictionaryAsync(p => p.ProductId, cancellationToken);

                var orderSummaries = new List<string>();
                foreach (var o in recentOrders)
                {
                    var pId = o.ProductId;
                    var pName = o.ProductName ?? (pId != null && productsDict.TryGetValue(pId, out var prod) ? prod.Name : "ReTrade Product");
                    var summary = $"- Order Code: #{o.OrderCode ?? o.OrderId} | Product: {pName} | Total: {o.FinalAmount ?? o.TotalAmount ?? 0:N0} VND | Status: {TranslateOrderStatus(o.Status)} | Date: {(o.CreatedAt.HasValue ? o.CreatedAt.Value.ToString("dd/MM/yyyy HH:mm") : "N/A")}";
                    if (!string.IsNullOrWhiteSpace(pId))
                    {
                        summary += $" | Link: [View Details](/product/{pId})";
                    }
                    orderSummaries.Add(summary);

                    if (!string.IsNullOrWhiteSpace(pId) && productsDict.TryGetValue(pId, out var itemDto) && !resultProducts.Any(x => x.ProductId == pId))
                    {
                        resultProducts.Add(itemDto);
                    }
                }

                var contextText = "[Current User's Real Order Data from ReTrade System]:\n" + string.Join("\n", orderSummaries) +
                    "\nNote: Present each order with product name, order code, total price, and status. Always include markdown links like [View Details](/product/PRODUCT_ID) and [View All Orders](/purchase-history) so the user can click to view details.";

                geminiContents.Insert(0, new GeminiContentDto
                {
                    Role = UserRole,
                    Parts = new List<GeminiPartDto>
                    {
                        new() { Text = contextText }
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch user order context for AI assistant.");
            }

            return resultProducts;
        }

        private static string TranslateOrderStatus(string? status)
        {
            return status switch
            {
                "AwaitingPayment" => "Awaiting Payment",
                "Pending" => "Processing",
                "Confirmed" => "Confirmed",
                "Shipping" => "In Transit / Shipping",
                "Delivered" => "Delivered",
                "Completed" => "Completed",
                "Cancelled" => "Cancelled",
                "ReturnRequested" => "Return Requested",
                "Returned" => "Returned",
                "ReturnRejected" => "Return Rejected",
                "DeliveryFailed" => "Delivery Failed",
                _ => status ?? "Unknown"
            };
        }

        private async Task<List<AssistantProductSuggestionDto>> SearchProductsAsync(
            ProductSearchToolArgs args,
            CancellationToken cancellationToken)
        {
            var accepted = ProductStatusEnum.Accepted.ToString();
            var ready = ProductStatusEnum.Ready.ToString();
            var limit = Math.Clamp(args.Limit ?? 5, 1, 10);

            var query = _productRepository.Query()
                .AsNoTracking()
                .Where(p =>
                    (p.Status == accepted || p.Status == ready) &&
                    p.StockQuantity > 0 &&
                    (p.Category == null || p.Category.Status == "Active") &&
                    (p.Seller == null || p.Seller.IsDeleted != true));

            if (!string.IsNullOrWhiteSpace(args.Keyword))
            {
                var keyword = args.Keyword.Trim().ToLower();
                query = query.Where(p =>
                    (p.Name != null && p.Name.ToLower().Contains(keyword)) ||
                    (p.Description != null && p.Description.ToLower().Contains(keyword)) ||
                    (p.Category != null && p.Category.Name != null && p.Category.Name.ToLower().Contains(keyword)) ||
                    p.ProductAttribute.Any(pa => pa.IsDeleted != true && pa.Value != null && pa.Value.ToLower().Contains(keyword)));
            }

            if (!string.IsNullOrWhiteSpace(args.Category))
            {
                var category = args.Category.Trim().ToLower();
                query = query.Where(p => p.Category != null && p.Category.Name != null && p.Category.Name.ToLower().Contains(category));
            }

            if (!string.IsNullOrWhiteSpace(args.Condition))
            {
                var condition = args.Condition.Trim().ToLower();
                query = query.Where(p => p.Condition != null && p.Condition.ToLower() == condition);
            }

            if (args.MinPrice.HasValue)
            {
                query = query.Where(p => p.Price.HasValue && p.Price >= args.MinPrice.Value);
            }

            if (args.MaxPrice.HasValue)
            {
                query = query.Where(p => p.Price.HasValue && p.Price <= args.MaxPrice.Value);
            }

            return await query
                .OrderBy(p => p.Price ?? decimal.MaxValue)
                .ThenByDescending(p => p.CreatedAt)
                .Take(limit)
                .Select(p => new AssistantProductSuggestionDto
                {
                    ProductId = p.ProductId,
                    Name = p.Name,
                    CategoryName = p.Category != null ? p.Category.Name : null,
                    Price = p.Price,
                    StockQuantity = p.StockQuantity,
                    Status = p.Status,
                    Condition = p.Condition,
                    SellerId = p.SellerId,
                    SellerName = p.Seller != null ? $"{p.Seller.FirstName} {p.Seller.LastName}".Trim() : null,
                    MainImageUrl = p.ProductImage
                        .Where(pi => pi.IsMain == true)
                        .Select(pi => pi.Image.ImageUrl)
                        .FirstOrDefault()
                        ?? p.ProductImage
                            .OrderBy(pi => pi.SortOrder)
                            .Select(pi => pi.Image.ImageUrl)
                            .FirstOrDefault()
                })
                .ToListAsync(cancellationToken);
        }

        private sealed class ProductSearchToolArgs
        {
            public string? Keyword { get; init; }
            public string? Category { get; init; }
            public decimal? MinPrice { get; init; }
            public decimal? MaxPrice { get; init; }
            public string? Condition { get; init; }
            public int? Limit { get; init; }

            public static ProductSearchToolArgs FromGeminiArgs(Dictionary<string, JsonElement>? args)
            {
                return From(args);
            }

            public static ProductSearchToolArgs FromJson(string? json)
            {
                if (string.IsNullOrWhiteSpace(json))
                {
                    return new ProductSearchToolArgs();
                }

                try
                {
                    var args = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json, JsonOptions);
                    return From(args);
                }
                catch (JsonException)
                {
                    return new ProductSearchToolArgs();
                }
            }

            private static ProductSearchToolArgs From(Dictionary<string, JsonElement>? args)
            {
                if (args == null)
                {
                    return new ProductSearchToolArgs();
                }

                return new ProductSearchToolArgs
                {
                    Keyword = GetString(args, "keyword"),
                    Category = GetString(args, "category"),
                    MinPrice = GetDecimal(args, "minPrice"),
                    MaxPrice = GetDecimal(args, "maxPrice"),
                    Condition = GetString(args, "condition"),
                    Limit = GetInt(args, "limit")
                };
            }

            private static string? GetString(Dictionary<string, JsonElement> args, string key)
            {
                return TryGetValue(args, key, out var value) && value.ValueKind == JsonValueKind.String
                    ? value.GetString()
                    : null;
            }

            private static decimal? GetDecimal(Dictionary<string, JsonElement> args, string key)
            {
                if (!TryGetValue(args, key, out var value))
                {
                    return null;
                }

                if (value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var number))
                {
                    return number;
                }

                if (value.ValueKind == JsonValueKind.String && decimal.TryParse(value.GetString(), out var parsed))
                {
                    return parsed;
                }

                return null;
            }

            private static int? GetInt(Dictionary<string, JsonElement> args, string key)
            {
                if (!TryGetValue(args, key, out var value))
                {
                    return null;
                }

                if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number))
                {
                    return number;
                }

                if (value.ValueKind == JsonValueKind.String && int.TryParse(value.GetString(), out var parsed))
                {
                    return parsed;
                }

                return null;
            }

            private static bool TryGetValue(Dictionary<string, JsonElement> args, string key, out JsonElement value)
            {
                if (args.TryGetValue(key, out value))
                {
                    return true;
                }

                var match = args.FirstOrDefault(kvp => string.Equals(kvp.Key, key, StringComparison.OrdinalIgnoreCase));
                value = match.Value;
                return !string.IsNullOrEmpty(match.Key);
            }
        }
    }
}
