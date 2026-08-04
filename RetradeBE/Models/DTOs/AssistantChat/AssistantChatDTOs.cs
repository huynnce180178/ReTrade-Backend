namespace RetradeBE.Models.DTOs.AssistantChat
{
    public class AssistantChatRequestDto
    {
        public string? SessionId { get; set; }
        public string Message { get; set; } = null!;
        public string? Language { get; set; }
    }

    public class AssistantChatResponseDto
    {
        public string SessionId { get; set; } = null!;
        public string MessageId { get; set; } = null!;
        public string Role { get; set; } = "model";
        public string Content { get; set; } = null!;
        public DateTime? CreatedAt { get; set; }
        public List<AssistantProductSuggestionDto> Products { get; set; } = new();
    }

    public class AssistantChatMessageDto
    {
        public string MessageId { get; set; } = null!;
        public string? SessionId { get; set; }
        public string? Role { get; set; }
        public string? Content { get; set; }
        public DateTime? CreatedAt { get; set; }
    }

    public class AssistantProductSuggestionDto
    {
        public string ProductId { get; set; } = null!;
        public string? Name { get; set; }
        public string? CategoryName { get; set; }
        public decimal? Price { get; set; }
        public int? StockQuantity { get; set; }
        public string? Status { get; set; }
        public string? Condition { get; set; }
        public string? MainImageUrl { get; set; }
        public string? SellerId { get; set; }
        public string? SellerName { get; set; }
    }
}
