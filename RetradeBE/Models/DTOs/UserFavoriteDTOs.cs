namespace RetradeBE.Models.DTOs
{
    public class UserFavoriteResponseDto
    {
        public string FavoriteId { get; set; } = null!;
        public string? CategoryId { get; set; }
        public string? ProductId { get; set; }
        public string? CategoryName { get; set; }
        public string? CategoryImageUrl { get; set; }
        public DateTime? CreatedAt { get; set; }
    }

    public class UserFavoriteCreateDto
    {
        public string CategoryId { get; set; } = null!;
    }
}

