namespace RetradeBE.Models.DTOs
{
    public class NotificationDto
    {
        public string NotificationId { get; set; } = null!;
        public string? UserId { get; set; }
        public string? Title { get; set; }
        public string? Message { get; set; }
        public string? Type { get; set; }
        public string? ReferenceId { get; set; }
        public bool? IsRead { get; set; }
        public DateTime? ReadAt { get; set; }
        public DateTime? CreatedAt { get; set; }
    }

    public class NotificationQueryDto
    {
        public string? Type { get; set; }
        public bool? IsRead { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class CreateNotificationDto
    {
        public string UserId { get; set; } = null!;
        public string Title { get; set; } = null!;
        public string Message { get; set; } = null!;
        public string Type { get; set; } = null!;
        public string? ReferenceId { get; set; }
    }
}
