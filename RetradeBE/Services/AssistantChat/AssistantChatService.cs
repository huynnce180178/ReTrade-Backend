using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
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
            var hasImage = !string.IsNullOrWhiteSpace(request.ImageBase64);

            if (string.IsNullOrWhiteSpace(message) && !hasImage)
            {
                throw new ArgumentException("Message or image is required.");
            }

            if (message.Length > 2000)
            {
                throw new ArgumentException("Message is too long.");
            }

            var storedContent = hasImage
                ? (!string.IsNullOrWhiteSpace(message)
                    ? $"![Attached Image]({request.ImageBase64.Trim()})\n{message}"
                    : $"![Attached Image]({request.ImageBase64.Trim()})")
                : message;

            var now = DateTime.UtcNow;
            var sessionTitle = !string.IsNullOrWhiteSpace(message)
                ? (message.Length > 80 ? message[..80] : message)
                : "Image Query";
            var session = await GetOrCreateSessionAsync(userId, request.SessionId, sessionTitle, now);

            await _chatMessageRepository.AddAsync(new ChatMessage
            {
                MessageId = RetradeBE.Utils.IdGenerator.GenerateId("amsg"),
                SessionId = session.SessionId,
                Role = UserRole,
                Content = storedContent,
                CreatedAt = now
            });

            session.LastMessageAt = now;
            await _chatSessionRepository.UpdateAsync(session);

            var lang = DetectUserMessageLanguage(message, request.Language);
            var isEnglish = lang == "en";

            var history = await _chatMessageRepository.GetBySessionIdAsync(session.SessionId);
            var geminiContents = BuildGeminiContents(history);
            var orderProducts = await InjectUserOrderContextAsync(userId, geminiContents, lang, cancellationToken);
            var suggestedProducts = new List<AssistantProductSuggestionDto>();
            if (orderProducts != null && orderProducts.Count > 0)
            {
                suggestedProducts.AddRange(orderProducts);
            }
            await InjectProductSearchContextAsync(message, geminiContents, suggestedProducts, lang, cancellationToken);

            var langDirective = isEnglish
                ? "[SYSTEM LANGUAGE DIRECTIVE]: The user interface language is currently set to ENGLISH. You MUST respond completely in English. Translate all labels, status names, and titles into English."
                : "[SYSTEM LANGUAGE DIRECTIVE]: The user interface language is currently set to VIETNAMESE. You MUST respond completely in Vietnamese.";

            geminiContents.Insert(0, new GeminiContentDto
            {
                Role = UserRole,
                Parts = new List<GeminiPartDto> { new() { Text = langDirective } }
            });

            string finalText;

            try
            {
                finalText = await GenerateGeminiResponseAsync(geminiContents, suggestedProducts, session.SessionId, cancellationToken);
                if (IsDomainBoundaryDecline(finalText) && suggestedProducts.Count > 0)
                {
                    finalText = BuildProductSuggestionResponse(message, suggestedProducts);
                }
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "Gemini assistant request failed. Falling back to local assistant response.");
                finalText = await BuildOfflineAssistantResponseAsync(message, userId, suggestedProducts, cancellationToken);
            }
            if (string.IsNullOrWhiteSpace(finalText))
            {
                finalText = "i18n:chat.assistant_offline_general";
            }

            if (suggestedProducts.Count == 0)
            {
                var fallbackArgs = BuildHeuristicProductSearchArgs(message);
                if (fallbackArgs != null)
                {
                    var fallbackProducts = await SearchProductsAsync(fallbackArgs, cancellationToken);
                    AddDistinctProducts(suggestedProducts, fallbackProducts);
                }
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

            var messages = session.ChatMessage
                .OrderBy(m => m.CreatedAt)
                .ToList();

            var result = new List<AssistantChatMessageDto>();
            for (int i = 0; i < messages.Count; i++)
            {
                var m = messages[i];
                if (m.Role == FunctionRole)
                {
                    continue;
                }

                var dto = new AssistantChatMessageDto
                {
                    MessageId = m.MessageId,
                    SessionId = m.SessionId,
                    Role = m.Role,
                    Content = m.Content,
                    CreatedAt = m.CreatedAt,
                    Products = new List<AssistantProductSuggestionDto>()
                };

                if (m.Role == ModelRole)
                {
                    var precedingFunc = messages
                        .Take(i)
                        .LastOrDefault(x => x.Role == FunctionRole && x.FunctionName == SearchProductsFunctionName);

                    if (precedingFunc != null && !string.IsNullOrWhiteSpace(precedingFunc.Content))
                    {
                        try
                        {
                            using var doc = JsonDocument.Parse(precedingFunc.Content);
                            if (doc.RootElement.TryGetProperty("products", out var prodsElement) && prodsElement.ValueKind == JsonValueKind.Array)
                            {
                                var prods = JsonSerializer.Deserialize<List<AssistantProductSuggestionDto>>(prodsElement.GetRawText(), JsonOptions);
                                if (prods != null && prods.Count > 0)
                                {
                                    dto.Products = prods.GroupBy(p => p.ProductId).Select(g => g.First()).ToList();
                                }
                            }
                        }
                        catch
                        {
                            // ignore json parse exception
                        }
                    }
                }

                result.Add(dto);
            }

            return result;
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
                    Parts = ParseGeminiParts(m.Content)
                })
                .ToList();
        }

        private static List<GeminiPartDto> ParseGeminiParts(string? content)
        {
            var parts = new List<GeminiPartDto>();
            if (string.IsNullOrWhiteSpace(content))
            {
                parts.Add(new GeminiPartDto { Text = string.Empty });
                return parts;
            }

            var imgMatch = Regex.Match(content, @"!\[.*?\]\((data:(?<mime>image\/[a-zA-Z0-9+\-]+);base64,(?<data>[A-Za-z0-9+/=]+))\)");
            if (imgMatch.Success)
            {
                var mimeType = imgMatch.Groups["mime"].Value;
                var base64Data = imgMatch.Groups["data"].Value;

                parts.Add(new GeminiPartDto
                {
                    InlineData = new GeminiInlineDataDto
                    {
                        MimeType = string.IsNullOrWhiteSpace(mimeType) ? "image/jpeg" : mimeType,
                        Data = base64Data
                    }
                });

                var textWithoutImg = content.Replace(imgMatch.Value, "").Trim();
                if (!string.IsNullOrWhiteSpace(textWithoutImg))
                {
                    parts.Add(new GeminiPartDto { Text = textWithoutImg });
                }
            }
            else
            {
                parts.Add(new GeminiPartDto { Text = content });
            }

            return parts;
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
            var lang = DetectUserMessageLanguage(message, null);
            var normalized = NormalizeForMatch(message);

            if (ContainsAny(normalized, "purchase history", "order history", "my orders", "lich su mua", "don hang", "mua hang"))
            {
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return lang == "vi"
                        ? "Bạn vui lòng đăng nhập để xem lịch sử đơn hàng của mình nhé!"
                        : "Please log in to view your order history.";
                }

                var recentOrders = await _purchaseService.QueryByBuyerId(userId)
                    .Take(5)
                    .ToListAsync(cancellationToken);

                if (recentOrders.Count > 0)
                {
                    var sb = new StringBuilder();
                    sb.AppendLine(lang == "vi" ? "Dưới đây là các đơn hàng gần đây của bạn trên ReTrade:\n" : "Here are your recent orders on ReTrade:\n");
                    foreach (var o in recentOrders)
                    {
                        var pId = o.ProductId;
                        var pName = o.ProductName ?? "ReTrade Product";
                        var imgUrl = pId != null && suggestedProducts.FirstOrDefault(x => x.ProductId == pId)?.MainImageUrl != null
                            ? suggestedProducts.First(x => x.ProductId == pId).MainImageUrl
                            : null;

                        if (!string.IsNullOrWhiteSpace(imgUrl))
                        {
                            sb.AppendLine($"![{pName}]({imgUrl})");
                        }
                        sb.AppendLine($"- **{pName}** | Order Code: #{o.OrderCode ?? o.OrderId} | Total: {o.FinalAmount ?? o.TotalAmount ?? 0:N0} VND | Status: {TranslateOrderStatus(o.Status)}");
                        sb.AppendLine($"[View Details](/purchase-history/{o.OrderId})\n");
                    }
                    sb.AppendLine("[View All Orders](/purchase-history)");
                    return sb.ToString();
                }

                return lang == "vi"
                    ? "Bạn chưa có đơn hàng nào gần đây trên ReTrade."
                    : "You don't have any recent orders on ReTrade.";
            }

            if (ContainsAny(normalized, "auction", "bid", "dau gia", "tra gia"))
            {
                return lang == "vi"
                    ? "Bạn có thể tham gia các phiên đấu giá trực tuyến hấp dẫn tại mục Đấu Giá của ReTrade. Hãy đặt giá và theo dõi thời gian nhé!"
                    : "You can join online auctions in the ReTrade Auction Hub. Place your bids and track the timer!";
            }

            if (ContainsAny(normalized, "sell", "selling", "post product", "list product", "dang ban", "ban san pham", "rao ban"))
            {
                return lang == "vi"
                    ? "Để đăng bán sản phẩm, bạn truy cập vào Kênh Người Bán (Seller Center) -> Quản lý sản phẩm -> Thêm sản phẩm mới nhé!"
                    : "To sell an item, go to Seller Center -> My Products -> Add New Product!";
            }

            if (ContainsAny(normalized, "wishlist", "favorite", "favourite", "yeu thich"))
            {
                return lang == "vi"
                    ? "Bạn có thể lưu các sản phẩm quan tâm vào Danh sách yêu thích bằng cách nhấn vào biểu tượng trái tim ở từng sản phẩm."
                    : "You can save items of interest to your Wishlist by clicking the heart icon on any product.";
            }

            // Default & product search intent fallback
            var searchArgs = BuildHeuristicProductSearchArgs(message) ?? new ProductSearchToolArgs { RawMessage = message, Limit = 5 };
            var products = await SearchProductsAsync(searchArgs, cancellationToken);
            AddDistinctProducts(suggestedProducts, products);

            var colorTerms = GetColorTerms(normalized);
            var isColorSearch = colorTerms.Count > 0;
            var hasExactColorMatch = isColorSearch && products.Any(p =>
            {
                var fullText = $"{p.Name} {p.Description} {p.CategoryName}";
                var tokens = Regex.Split(NormalizeForMatch(fullText), @"[^\w\d]+").ToHashSet(StringComparer.OrdinalIgnoreCase);
                return colorTerms.Any(c => tokens.Contains(NormalizeForMatch(c)));
            });

            if (products.Count > 0)
            {
                var sb = new StringBuilder();
                if (isColorSearch && !hasExactColorMatch)
                {
                    sb.AppendLine(lang == "vi"
                        ? "Hiện tại ReTrade chưa có sản phẩm khớp màu sắc bạn tìm. Bạn tham khảo các sản phẩm nổi bật dưới đây nhé:\n"
                        : "Currently ReTrade does not have items in this exact color. Here are featured items you may like:\n");
                }
                else
                {
                    sb.AppendLine(lang == "vi"
                        ? "Dưới đây là các sản phẩm phù hợp trên ReTrade cho bạn:\n"
                        : "Here are the products matching your request on ReTrade:\n");
                }

                foreach (var p in products)
                {
                    if (!string.IsNullOrWhiteSpace(p.MainImageUrl))
                    {
                        sb.AppendLine($"![{p.Name}]({p.MainImageUrl})");
                    }
                    sb.AppendLine($"### {p.Name}");
                    sb.AppendLine(lang == "vi"
                        ? $"- Giá: {p.Price ?? 0:N0} VND"
                        : $"- Price: {p.Price ?? 0:N0} VND");
                    sb.AppendLine(lang == "vi"
                        ? $"- Tình trạng: {p.Condition ?? "Good"}"
                        : $"- Condition: {p.Condition ?? "Good"}");
                    sb.AppendLine(lang == "vi"
                        ? $"- Người bán: {p.SellerName ?? "ReTrade Seller"}"
                        : $"- Seller: {p.SellerName ?? "ReTrade Seller"}");
                    sb.AppendLine(lang == "vi"
                        ? $"[Xem chi tiết](/product/{p.ProductId}) [Thêm yêu thích](/product/{p.ProductId}?action=wishlist) [Mua ngay](/product/{p.ProductId}?action=buy)\n"
                        : $"[View Details](/product/{p.ProductId}) [Add to Wishlist](/product/{p.ProductId}?action=wishlist) [Buy Now](/product/{p.ProductId}?action=buy)\n");
                }

                return sb.ToString();
            }

            if (isColorSearch)
            {
                return lang == "vi"
                    ? "Hiện tại ReTrade chưa có sản phẩm khớp màu sắc bạn tìm. Bạn có thể thử tìm kiếm từ khóa khác hoặc tham khảo các danh mục sản phẩm khác nhé!"
                    : "Currently ReTrade does not have items in this color. Feel free to search with other keywords or browse categories!";
            }

            return lang == "vi"
                ? "Chào bạn! Tôi là Trợ lý ReTrade. Hiện chưa tìm thấy sản phẩm khớp chính xác, bạn có thể thử từ khóa khác hoặc hỏi về đấu giá, đơn hàng nhé!"
                : "Hello! I am ReTrade Assistant. Currently no exact products matched, feel free to search with other keywords or ask about orders and auctions!";
        }

        private async Task InjectProductSearchContextAsync(
            string message,
            List<GeminiContentDto> geminiContents,
            List<AssistantProductSuggestionDto> suggestedProducts,
            string lang,
            CancellationToken cancellationToken)
        {
            var args = BuildHeuristicProductSearchArgs(message);
            if (args == null)
            {
                return;
            }

            var products = await SearchProductsAsync(args, cancellationToken);
            AddDistinctProducts(suggestedProducts, products);

            var contextText = products.Count > 0
                ? "[Relevant ReTrade Product Search Results from Database]:\n" +
                  string.Join("\n", products.Select((p, index) =>
                      $"- Product {index + 1}: Name: {p.Name} | MainImageUrl: {p.MainImageUrl ?? "N/A"} | Price: {p.Price ?? 0:N0} VND | Category: {p.CategoryName ?? "N/A"} | Condition: {p.Condition ?? "N/A"} | Stock: {p.StockQuantity ?? 0} | Seller: {p.SellerName ?? "N/A"} | Description: {p.Description ?? "N/A"} | ProductId: {p.ProductId}"))
                : "[Relevant ReTrade Product Search Results from Database]: No matching products were found.";

            contextText += lang == "en"
                ? "\nInstruction: Respond strictly in ENGLISH. For product recommendations, use English field labels (- Price: [Price] VND, - Condition: [Condition], - Seller: [Seller Name]) and English action buttons ([View Details](/product/ID) [Add to Wishlist](/product/ID?action=wishlist) [Buy Now](/product/ID?action=buy)). Only mention products listed above."
                : "\nInstruction: Respond strictly in VIETNAMESE. For product recommendations, use Vietnamese field labels (- Giá: [Price] VND, - Tình trạng: [Condition], - Người bán: [Seller Name]) and Vietnamese action buttons ([Xem chi tiết](/product/ID) [Thêm yêu thích](/product/ID?action=wishlist) [Mua ngay](/product/ID?action=buy)). Only mention products listed above.";

            geminiContents.Insert(0, new GeminiContentDto
            {
                Role = UserRole,
                Parts = new List<GeminiPartDto>
                {
                    new() { Text = contextText }
                }
            });
        }

        private static ProductSearchToolArgs? BuildHeuristicProductSearchArgs(string message)
        {
            var raw = (message ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(raw))
            {
                return null;
            }

            var normalized = NormalizeForMatch(raw);

            // Filter out purely non-product conversational questions (e.g. system help, greetings, orders)
            var isPureOrderQuery = ContainsAny(normalized, "don hang", "đơn hàng", "don cua toi", "đơn của tôi", "order history", "my orders");
            var isPureOffTopicQuery = ContainsAny(normalized, "thoi tiet", "hien tai", "1 + 1", "lap trinh", "code");
            if (isPureOrderQuery || isPureOffTopicQuery)
            {
                return null;
            }

            var (minPrice, maxPrice) = ExtractPriceRange(raw);

            var stopWords = new[] { "mau", "cần", "can", "tim", "tìm", "mua", "cho", "toi", "tôi", "ban", "bạn", "giup", "giúp", "loai", "loại", "co", "có", "khong", "không", "nao", "nào", "goi y", "tu van", "nhu cau", "san pham", "sản phẩm", "duoi", "dưới", "under", "tren", "trên", "over", "khoang", "khoảng", "tam", "tầm", "gia", "giá", "den", "đến", "tu", "từ", "hay", "đẹp", "dap", "tốt", "tot", "rẻ", "re", "mới", "moi", "ạ", "a", "nhé", "nhe", "nha", "ơi", "oi", "với", "voi" };
            var cleanedMessage = raw;

            // Strip price expressions like "dưới 100k", "< 100k", "100k"
            cleanedMessage = Regex.Replace(cleanedMessage, @"(duoi|dưới|under|<|>|tren|trên|over|khoang|khoảng|tam|tầm)\s*\d+[\d\.,]*\s*(k|k|tr|trieu|triệu|m|vnd|vnđ)?", "", RegexOptions.IgnoreCase);
            cleanedMessage = Regex.Replace(cleanedMessage, @"\b\d+\s*(k|k|tr|trieu|triệu|m)\b", "", RegexOptions.IgnoreCase);

            foreach (var sw in stopWords)
            {
                cleanedMessage = Regex.Replace(cleanedMessage, $@"\b{Regex.Escape(sw)}\b", "", RegexOptions.IgnoreCase);
            }
            cleanedMessage = Regex.Replace(cleanedMessage, @"\s+", " ").Trim();

            return new ProductSearchToolArgs
            {
                RawMessage = raw,
                Keyword = string.IsNullOrWhiteSpace(cleanedMessage) ? raw : cleanedMessage,
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                MinStorageGb = ExtractMinStorageGb(normalized),
                Limit = 8
            };
        }

        private static (decimal? minPrice, decimal? maxPrice) ExtractPriceRange(string rawMessage)
        {
            decimal? minPrice = null;
            decimal? maxPrice = null;
            var norm = NormalizeForMatch(rawMessage);

            var underMatch = Regex.Match(norm, @"(duoi|under|<|nho hon|thap hon)\s*(?<num>\d+[\d\.,]*)\s*(?<unit>k|tr|trieu|m|vnd)?", RegexOptions.IgnoreCase);
            if (underMatch.Success && TryParsePriceValue(underMatch.Groups["num"].Value, underMatch.Groups["unit"].Value, out var valUnder))
            {
                maxPrice = valUnder;
            }

            var overMatch = Regex.Match(norm, @"(tren|over|>|lon hon|cao hon)\s*(?<num>\d+[\d\.,]*)\s*(?<unit>k|tr|trieu|m|vnd)?", RegexOptions.IgnoreCase);
            if (overMatch.Success && TryParsePriceValue(overMatch.Groups["num"].Value, overMatch.Groups["unit"].Value, out var valOver))
            {
                minPrice = valOver;
            }

            if (maxPrice == null && minPrice == null)
            {
                var standaloneK = Regex.Match(norm, @"(?<num>\d+)\s*k\b", RegexOptions.IgnoreCase);
                if (standaloneK.Success && decimal.TryParse(standaloneK.Groups["num"].Value, out var kVal))
                {
                    maxPrice = kVal < 1000 ? kVal * 1000 : kVal;
                }
            }

            return (minPrice, maxPrice);
        }

        private static bool TryParsePriceValue(string numStr, string unitStr, out decimal price)
        {
            price = 0;
            numStr = numStr.Replace(".", "").Replace(",", "");
            if (!decimal.TryParse(numStr, out var rawNum))
            {
                return false;
            }

            var u = (unitStr ?? string.Empty).ToLowerInvariant();
            if (u == "k")
            {
                price = rawNum * 1000;
            }
            else if (u == "tr" || u == "trieu" || u == "m")
            {
                price = rawNum * 1000000;
            }
            else
            {
                price = rawNum < 1000 ? rawNum * 1000 : rawNum;
            }

            return true;
        }

        private static int? ExtractMinStorageGb(string normalizedMessage)
        {
            var match = Regex.Match(normalizedMessage, @"(?<value>\d{2,4})\s*(gb|g)\b", RegexOptions.IgnoreCase);
            if (!match.Success || !int.TryParse(match.Groups["value"].Value, out var value))
            {
                return null;
            }

            return value;
        }

        private static bool IsDomainBoundaryDecline(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            return text.Contains("specialized strictly", StringComparison.OrdinalIgnoreCase) ||
                   text.Contains("Please ask me questions related to ReTrade", StringComparison.OrdinalIgnoreCase);
        }

        private static string BuildProductSuggestionResponse(string message, List<AssistantProductSuggestionDto> products)
        {
            var normalized = NormalizeForMatch(message);
            var isVietnamese = ContainsAny(normalized, "toi", "muon", "mua", "dien thoai", "san pham", "dung luong", "tro len", "tim");

            if (isVietnamese)
            {
                return products.Count == 0
                    ? "Mình chưa tìm thấy sản phẩm phù hợp trong dữ liệu ReTrade hiện tại."
                    : "Mình tìm thấy một số sản phẩm ReTrade phù hợp với yêu cầu của bạn:";
            }

            return products.Count == 0
                ? "I couldn't find matching products in the current ReTrade database."
                : "I found some ReTrade products that match your request:";
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
            string lang,
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
                    .Include(p => p.ProductImage)
                        .ThenInclude(pi => pi.Image)
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

                var isEnglish = lang == "en";
                var orderSummaries = new List<string>();
                foreach (var o in recentOrders)
                {
                    var pId = o.ProductId;
                    var pName = o.ProductName ?? (pId != null && productsDict.TryGetValue(pId, out var prod) ? prod.Name : "ReTrade Product");
                    var imgUrl = pId != null && productsDict.TryGetValue(pId, out var prodImg) ? prodImg.MainImageUrl : null;

                    var summary = isEnglish
                        ? $"- Order Code: #{o.OrderCode ?? o.OrderId} | Product: {pName} | Total: {o.FinalAmount ?? o.TotalAmount ?? 0:N0} VND | Status: {TranslateOrderStatus(o.Status)}"
                        : $"- Mã đơn hàng: #{o.OrderCode ?? o.OrderId} | Sản phẩm: {pName} | Tổng cộng: {o.FinalAmount ?? o.TotalAmount ?? 0:N0} VND | Trạng thái: {TranslateOrderStatus(o.Status)}";

                    if (!string.IsNullOrWhiteSpace(imgUrl))
                    {
                        summary += $" | ImageUrl: {imgUrl}";
                    }
                    if (!string.IsNullOrWhiteSpace(o.OrderId))
                    {
                        summary += isEnglish
                            ? $" | Link: [View Details](/purchase-history/{o.OrderId})"
                            : $" | Link: [Xem chi tiết](/purchase-history/{o.OrderId})";
                    }
                    orderSummaries.Add(summary);

                    if (!string.IsNullOrWhiteSpace(pId) && productsDict.TryGetValue(pId, out var itemDto) && !resultProducts.Any(x => x.ProductId == pId))
                    {
                        resultProducts.Add(itemDto);
                    }
                }

                var contextText = "[Current User's Real Order Data from ReTrade System]:\n" + string.Join("\n", orderSummaries) +
                    (isEnglish
                        ? "\nNote: For each order with an ImageUrl, YOU MUST include the image using markdown `![Product Name](ImageUrl)` before the order details. Format: Product Image markdown, Order Code, Product Name, Total Amount, Order Status. Always include markdown links like [View Details](/purchase-history/ORDER_ID) and [View All Orders](/purchase-history)."
                        : "\nNote: Với mỗi đơn hàng có ImageUrl, BẮT BUỘC chèn hình ảnh bằng markdown `![Tên sản phẩm](ImageUrl)` phía trước thông tin đơn hàng. Định dạng: Ảnh sản phẩm markdown, Mã đơn hàng, Tên sản phẩm, Tổng cộng, Trạng thái. Always include markdown links like [Xem chi tiết](/purchase-history/ORDER_ID) and [Xem tất cả đơn hàng](/purchase-history).");

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
            var limit = Math.Clamp(args.Limit ?? 8, 1, 15);

            var baseQuery = _productRepository.Query()
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.Seller)
                .Include(p => p.ProductImage).ThenInclude(pi => pi.Image)
                .Include(p => p.ProductAttribute).ThenInclude(pa => pa.Attribute)
                .Where(p =>
                    (p.Status == accepted || p.Status == ready) &&
                    p.StockQuantity > 0 &&
                    p.IsDeleted != true &&
                    (p.Category == null || p.Category.Status == "Active") &&
                    (p.Seller == null || p.Seller.IsDeleted != true));

            if (args.MinPrice.HasValue && args.MinPrice.Value > 0)
            {
                baseQuery = baseQuery.Where(p => p.Price >= args.MinPrice.Value);
            }
            if (args.MaxPrice.HasValue && args.MaxPrice.Value > 0)
            {
                baseQuery = baseQuery.Where(p => p.Price <= args.MaxPrice.Value);
            }

            var domain = (args.CategoryDomain ?? args.Category ?? string.Empty).Trim().ToLower();
            if (!string.IsNullOrWhiteSpace(domain))
            {
                var categoryMatchQuery = baseQuery.Where(p => p.Category != null &&
                    (p.Category.Name.ToLower().Contains(domain) || domain.Contains(p.Category.Name.ToLower())));

                if (await categoryMatchQuery.AnyAsync(cancellationToken))
                {
                    baseQuery = categoryMatchQuery;
                }
            }

            var allActive = await baseQuery.ToListAsync(cancellationToken);
            if (allActive.Count == 0 && (args.MinPrice.HasValue || args.MaxPrice.HasValue))
            {
                var priceQuery = _productRepository.Query()
                    .AsNoTracking()
                    .Include(p => p.Category)
                    .Include(p => p.Seller)
                    .Include(p => p.ProductImage).ThenInclude(pi => pi.Image)
                    .Include(p => p.ProductAttribute).ThenInclude(pa => pa.Attribute)
                    .Where(p => (p.Status == accepted || p.Status == ready) && p.StockQuantity > 0 && p.IsDeleted != true);

                if (args.MinPrice.HasValue && args.MinPrice.Value > 0) priceQuery = priceQuery.Where(p => p.Price >= args.MinPrice.Value);
                if (args.MaxPrice.HasValue && args.MaxPrice.Value > 0) priceQuery = priceQuery.Where(p => p.Price <= args.MaxPrice.Value);

                allActive = await priceQuery.ToListAsync(cancellationToken);
            }

            if (allActive.Count == 0)
            {
                allActive = await _productRepository.Query()
                    .AsNoTracking()
                    .Include(p => p.Category)
                    .Include(p => p.Seller)
                    .Include(p => p.ProductImage).ThenInclude(pi => pi.Image)
                    .Include(p => p.ProductAttribute).ThenInclude(pa => pa.Attribute)
                    .Where(p => (p.Status == accepted || p.Status == ready) && p.StockQuantity > 0 && p.IsDeleted != true)
                    .ToListAsync(cancellationToken);
            }

            if (allActive.Count == 0)
            {
                return new List<AssistantProductSuggestionDto>();
            }

            var rawMessage = (args.RawMessage ?? args.Keyword ?? string.Empty).Trim();
            var normMessage = NormalizeForMatch(rawMessage);
            var searchTokens = TokenizeAndClean(rawMessage);

            if (searchTokens.Count == 0 && !string.IsNullOrWhiteSpace(normMessage))
            {
                searchTokens = Regex.Split(normMessage, @"[^\w\d]+")
                    .Where(w => !string.IsNullOrWhiteSpace(w) && w.Length > 1)
                    .ToList();
            }

            var scoredProductsList = allActive.Select(p =>
            {
                var pName = p.Name ?? string.Empty;
                var pDesc = p.Description ?? string.Empty;
                var pCatName = p.Category?.Name ?? string.Empty;
                var pCatDesc = p.Category?.Description ?? string.Empty;
                var pAttrValues = string.Join(" ", p.ProductAttribute.Select(pa => $"{pa.Attribute?.Name} {pa.Value}"));

                var fullProductText = $"{pName} {pCatName} {pCatDesc} {pAttrValues} {pDesc}";
                var normFullText = NormalizeForMatch(fullProductText);

                var fullTextWordTokens = Regex.Split(normFullText, @"[^\w\d]+")
                    .Where(w => !string.IsNullOrWhiteSpace(w))
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                var nameWordTokens = Regex.Split(NormalizeForMatch(pName), @"[^\w\d]+")
                    .Where(w => !string.IsNullOrWhiteSpace(w))
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                var catWordTokens = Regex.Split(NormalizeForMatch(pCatName), @"[^\w\d]+")
                    .Where(w => !string.IsNullOrWhiteSpace(w))
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                int matchedTokenCount = 0;
                int score = 0;
                bool matchedCategory = false;

                foreach (var token in searchTokens)
                {
                    var normToken = NormalizeForMatch(token);
                    if (string.IsNullOrWhiteSpace(normToken) || normToken.Length <= 1) continue;

                    bool tokenMatched = false;

                    if (catWordTokens.Contains(normToken) || NormalizeForMatch(pCatName).Contains(normToken))
                    {
                        score += 300;
                        matchedCategory = true;
                        tokenMatched = true;
                    }

                    if (nameWordTokens.Contains(normToken) || pName.Contains(token, StringComparison.OrdinalIgnoreCase))
                    {
                        score += 150;
                        tokenMatched = true;
                    }
                    else if (fullTextWordTokens.Contains(normToken) || normFullText.Contains(normToken))
                    {
                        score += 50;
                        tokenMatched = true;
                    }
                    else if (IsBilingualSynonymMatch(normToken, normFullText, fullTextWordTokens))
                    {
                        score += 80;
                        tokenMatched = true;
                    }

                    if (tokenMatched)
                    {
                        matchedTokenCount++;
                    }
                }

                return new
                {
                    Product = p,
                    Score = score,
                    MatchedTokenCount = matchedTokenCount,
                    MatchedCategory = matchedCategory
                };
            })
            .Where(x => x.Score > 0)
            .ToList();

            if (scoredProductsList.Count == 0)
            {
                return allActive.Take(limit).Select(p => MapToAssistantProductDto(p)).ToList();
            }

            int maxMatchedTokens = scoredProductsList.Max(x => x.MatchedTokenCount);
            var candidateList = scoredProductsList;

            if (maxMatchedTokens > 1)
            {
                var topTokenMatches = scoredProductsList.Where(x => x.MatchedTokenCount >= maxMatchedTokens - 1).ToList();
                if (topTokenMatches.Count > 0)
                {
                    candidateList = topTokenMatches;
                }
            }

            var categoryMatches = candidateList.Where(x => x.MatchedCategory).ToList();
            if (categoryMatches.Count > 0)
            {
                candidateList = categoryMatches;
            }

            int maxScore = candidateList.Max(x => x.Score);
            if (maxScore > 0)
            {
                int threshold = Math.Max(30, (int)(maxScore * 0.30));
                var filtered = candidateList.Where(x => x.Score >= threshold).ToList();
                if (filtered.Count > 0)
                {
                    candidateList = filtered;
                }
            }

            var topScored = candidateList
                .OrderByDescending(x => x.Score)
                .ThenByDescending(x => x.MatchedTokenCount)
                .ThenByDescending(x => x.Product.CreatedAt)
                .Select(x => x.Product)
                .Take(limit)
                .ToList();

            return topScored.Select(p => MapToAssistantProductDto(p)).ToList();
        }

        private static bool IsBilingualSynonymMatch(string normToken, string normFullText, HashSet<string> fullTextWordTokens)
        {
            // Leather / Da
            if (normToken == "da" && (fullTextWordTokens.Contains("leather") || normFullText.Contains("leather"))) return true;
            if (normToken == "leather" && (fullTextWordTokens.Contains("da") || normFullText.Contains("da"))) return true;

            // Jackets / Áo khoác
            if ((normToken == "khoac" || normToken == "ao khoac") &&
                (normFullText.Contains("jacket") || normFullText.Contains("coat") || normFullText.Contains("blazer") || normFullText.Contains("parka"))) return true;
            if ((normToken == "jacket" || normToken == "coat" || normToken == "blazer") && normFullText.Contains("khoac")) return true;

            // Denim / Jean / Bò
            if ((normToken == "jean" || normToken == "jeans" || normToken == "bo") &&
                (normFullText.Contains("denim") || normFullText.Contains("jean") || normFullText.Contains("jeans"))) return true;
            if (normToken == "denim" && (normFullText.Contains("jean") || normFullText.Contains("jeans") || normFullText.Contains("bo"))) return true;

            // Shirts / Áo thun / Áo phông
            if ((normToken == "thun" || normToken == "phong" || normToken == "ao") &&
                (normFullText.Contains("tee") || normFullText.Contains("t-shirt") || normFullText.Contains("shirt") || normFullText.Contains("top"))) return true;
            if ((normToken == "tee" || normToken == "shirt" || normToken == "top") && (normFullText.Contains("thun") || normFullText.Contains("phong") || normFullText.Contains("ao"))) return true;

            // Shoes / Giày
            if ((normToken == "giay" || normToken == "sneaker") &&
                (normFullText.Contains("shoes") || normFullText.Contains("sneakers") || normFullText.Contains("footwear"))) return true;
            if ((normToken == "shoes" || normToken == "sneakers") && normFullText.Contains("giay")) return true;

            // Books / Sách / Novel
            if ((normToken == "sach" || normToken == "truyen") &&
                (normFullText.Contains("book") || normFullText.Contains("bookstore") || normFullText.Contains("novel") || normFullText.Contains("sach") || normFullText.Contains("truyen"))) return true;
            if (normToken == "book" && (normFullText.Contains("sach") || normFullText.Contains("truyen") || normFullText.Contains("book"))) return true;

            // Watch / Đồng hồ
            if (normToken == "dong ho" && (normFullText.Contains("watch") || normFullText.Contains("timepiece"))) return true;
            if (normToken == "watch" && normFullText.Contains("dong ho")) return true;

            // Perfume / Nước hoa
            if (normToken == "nuoc hoa" && (normFullText.Contains("perfume") || normFullText.Contains("fragrance"))) return true;
            if (normToken == "perfume" && normFullText.Contains("nuoc hoa")) return true;

            // Keyboard / Bàn phím
            if (normToken == "ban phim" && normFullText.Contains("keyboard")) return true;
            if (normToken == "keyboard" && normFullText.Contains("ban phim")) return true;

            // Headphones / Earbuds / Tai nghe
            if ((normToken == "tai nghe" || normToken == "tai") &&
                (normFullText.Contains("headphone") || normFullText.Contains("headphones") || normFullText.Contains("earbuds") || normFullText.Contains("earphone") || normFullText.Contains("iem"))) return true;
            if ((normToken == "headphone" || normToken == "earbuds" || normToken == "iem") && normFullText.Contains("tai")) return true;

            // Phone / Điện thoại
            if ((normToken == "dien thoai" || normToken == "dt") &&
                (normFullText.Contains("phone") || normFullText.Contains("smartphone") || normFullText.Contains("iphone"))) return true;
            if ((normToken == "phone" || normToken == "iphone") && (normFullText.Contains("dien thoai") || normFullText.Contains("dt"))) return true;

            return false;
        }

        private static HashSet<string> GetColorTerms(string normMessage)
        {
            var colors = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (ContainsAny(normMessage, "do", "red"))
            {
                colors.Add("do"); colors.Add("đỏ"); colors.Add("red");
            }
            if (ContainsAny(normMessage, "den", "black"))
            {
                colors.Add("den"); colors.Add("đen"); colors.Add("black");
            }
            if (ContainsAny(normMessage, "trang", "white"))
            {
                colors.Add("trang"); colors.Add("trắng"); colors.Add("white");
            }
            if (ContainsAny(normMessage, "xanh", "blue", "green"))
            {
                colors.Add("xanh"); colors.Add("blue"); colors.Add("green");
            }
            if (ContainsAny(normMessage, "vang", "yellow"))
            {
                colors.Add("vang"); colors.Add("vàng"); colors.Add("yellow");
            }
            return colors;
        }

        private static List<string> TokenizeAndClean(string input)
        {
            var stopWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "mau", "màu", "can", "cần", "tim", "tìm", "mua", "cho", "toi", "tôi", "ban", "bạn", "giup", "giúp",
                "loai", "loại", "co", "có", "khong", "không", "nao", "nào", "goi y", "tu van", "nhu cau", "san pham", "sản phẩm"
            };

            var words = Regex.Split(input, @"[^\w\d\+]+")
                .Where(w => !string.IsNullOrWhiteSpace(w) && w.Length > 1 && !stopWords.Contains(w))
                .ToList();

            return words;
        }

        private static AssistantProductSuggestionDto MapToAssistantProductDto(Product p)
        {
            return new AssistantProductSuggestionDto
            {
                ProductId = p.ProductId,
                Name = p.Name,
                Description = p.Description != null && p.Description.Length > 250 ? p.Description.Substring(0, 250) + "..." : p.Description,
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
            };
        }

        private sealed class ProductSearchToolArgs
        {
            public string? RawMessage { get; init; }
            public string? Keyword { get; init; }
            public string? Category { get; init; }
            public string? CategoryDomain { get; init; }
            public decimal? MinPrice { get; init; }
            public decimal? MaxPrice { get; init; }
            public string? Condition { get; init; }
            public int? Limit { get; init; }
            public int? MinStorageGb { get; init; }

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
                    Limit = GetInt(args, "limit"),
                    MinStorageGb = GetInt(args, "minStorageGb")
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

        private static List<string> BuildAllowedStorageValues(int minStorageGb)
        {
            var knownStorageValues = new[] { 32, 64, 128, 256, 512, 1024, 2048 };

            return knownStorageValues
                .Where(value => value >= minStorageGb)
                .SelectMany(value => new[]
                {
                    value.ToString(CultureInfo.InvariantCulture),
                    $"{value.ToString(CultureInfo.InvariantCulture)}gb",
                    $"{value.ToString(CultureInfo.InvariantCulture)} gb"
                })
                .ToList();
        }

        private static string DetectUserMessageLanguage(string message, string? defaultLang)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return string.Equals(defaultLang, "en", StringComparison.OrdinalIgnoreCase) ? "en" : "vi";
            }

            var normalized = message.ToLowerInvariant();

            var vietnameseDiacritics = new[]
            {
                'à','á','ạ','ả','ã','â','ầ','ấ','ậ','ẩ','ẫ','ă','ằ','ắ','ặ','ẳ','ẵ',
                'è','é','ẹ','ẻ','ẽ','ê','ề','ế','ệ','ể','ễ',
                'ì','í','ị','ỉ','ĩ',
                'ò','ó','ọ','ỏ','õ','ô','ồ','ố','ộ','ổ','ỗ','ơ','ờ','ớ','ợ','ở','ỡ',
                'ù','ú','ụ','ủ','ũ','ư','ừ','ứ','ự','ử','ữ',
                'ỳ','ý','ỵ','ỷ','ỹ','đ'
            };

            if (normalized.Any(c => vietnameseDiacritics.Contains(c)))
            {
                return "vi";
            }

            var unaccentedViWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "cho", "toi", "tôi", "mua", "ban", "bán", "can", "cần", "tim", "tìm", "co", "có", "khong", "không",
                "nao", "nào", "gia", "giá", "bao", "nhieu", "nhiều", "ao", "áo", "quan", "quần", "giay", "giày",
                "tui", "túi", "dep", "đẹp", "re", "rẻ", "tot", "tốt", "xem", "goi", "gợi", "y", "ý", "nhu", "như",
                "cau", "cầu", "san", "sản", "pham", "phẩm", "do", "đỏ", "den", "đen", "trang", "trắng", "xanh", "vang", "vàng"
            };

            var words = Regex.Split(normalized, @"[^\w\d]+")
                .Where(w => !string.IsNullOrWhiteSpace(w))
                .ToList();

            if (words.Any(w => unaccentedViWords.Contains(w)))
            {
                return "vi";
            }

            var englishWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "the", "is", "are", "you", "have", "show", "me", "find", "buy", "sell",
                "shirt", "shoes", "bag", "laptop", "phone", "price", "how", "much", "what",
                "which", "red", "black", "white", "blue", "green", "yellow", "jacket", "recommend", "suggestion"
            };

            if (words.Any(w => englishWords.Contains(w)))
            {
                return "en";
            }

            return string.Equals(defaultLang, "en", StringComparison.OrdinalIgnoreCase) ? "en" : "vi";
        }
    }
}
