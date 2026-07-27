namespace RetradeBE.Models.DTOs
{
    public class SendMessageRequestDto
    {
        public string Message { get; set; } = null!;
        public string? MessageType { get; set; } = "Text";
    }

    public class CreateChatRoomRequestDto
    {
        public string? ProductId { get; set; }
        public string? SellerId { get; set; }
    }

    public class ChatParticipantDto
    {
        public string? UserId { get; set; }
        public string? DisplayName { get; set; }
        public string? Email { get; set; }
        public string? AvatarUrl { get; set; }
    }

    public class ChatMessageDto
    {
        public string ChatId { get; set; } = null!;
        public string? RoomId { get; set; }
        public string? SenderId { get; set; }
        public string? SenderName { get; set; }
        public string? SenderAvatarUrl { get; set; }
        public string? Message { get; set; }
        public string? MessageType { get; set; }
        public bool IsRead { get; set; }
        public DateTime? ReadAt { get; set; }
        public DateTime? CreatedAt { get; set; }
    }

    public class ChatRoomListDto
    {
        public string RoomId { get; set; } = null!;
        public string? BuyerId { get; set; }
        public string? SellerId { get; set; }
        public string? ProductId { get; set; }
        public string? ProductName { get; set; }
        public string? ProductImageUrl { get; set; }
        public ChatParticipantDto? Buyer { get; set; }
        public ChatParticipantDto? Seller { get; set; }
        public ChatParticipantDto? OtherParticipant { get; set; }
        public ChatMessageDto? LastMessage { get; set; }
        public int UnreadCount { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
