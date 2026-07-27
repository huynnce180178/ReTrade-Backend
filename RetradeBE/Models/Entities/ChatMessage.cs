using System;

namespace RetradeBE.Models;

public partial class ChatMessage
{
    public string MessageId { get; set; } = null!;

    public string? SessionId { get; set; }

    public string? Role { get; set; }

    public string? Content { get; set; }

    public string? FunctionName { get; set; }

    public string? FunctionCallId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ChatSession? Session { get; set; }
}
