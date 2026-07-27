using System;
using System.Collections.Generic;

namespace RetradeBE.Models;

public partial class ChatSession
{
    public string SessionId { get; set; } = null!;

    public string? UserId { get; set; }

    public string? Title { get; set; }

    public DateTime? StartedAt { get; set; }

    public DateTime? LastMessageAt { get; set; }

    public bool? IsActive { get; set; }

    public virtual ICollection<ChatMessage> ChatMessage { get; set; } = new List<ChatMessage>();

    public virtual User? User { get; set; }
}
