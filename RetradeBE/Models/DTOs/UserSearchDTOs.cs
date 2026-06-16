namespace RetradeBE.Models.DTOs
{
    public class UserSearchResponseDto
    {
        public string SearchId { get; set; } = null!;
        public string? Keyword { get; set; }
        public string? CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public DateTime? CreatedAt { get; set; }
    }

    public class UserSearchCreateDto
    {
        public string? Keyword { get; set; }
        public string? CategoryId { get; set; }
    }
}
