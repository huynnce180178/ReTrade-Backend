namespace RetradeBE.Models.DTOs
{
    public class ReviewCreateDto
    {
        public string OrderId { get; set; } = null!;
        public int Rating { get; set; }
        public string? Comment { get; set; }
    }

    public class ReviewResponseDto
    {
        public string ReviewId { get; set; } = null!;
        public string? ReviewerId { get; set; }
        public string? SellerId { get; set; }
        public string? OrderId { get; set; }
        public int? Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
