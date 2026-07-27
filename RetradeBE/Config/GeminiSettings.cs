namespace RetradeBE.Config
{
    public class GeminiSettings
    {
        public string? ApiKey { get; set; }
        public string Model { get; set; } = "gemini-2.0-flash";
        public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com/v1beta/models";
        public int RequestTimeoutSeconds { get; set; } = 20;
        public int MaxRetryAttempts { get; set; } = 1;
    }
}
