using System;
using System.Collections.Generic;

namespace RetradeBE.Models;

public partial class Chat
{
    public string ChatId { get; set; } = null!;

    public string? RoomId { get; set; }

    public string? SenderId { get; set; }

    public string? Message { get; set; }

    public string? MessageType { get; set; }

    public bool? IsRead { get; set; }

    public bool? IsRecalled { get; set; }

    public DateTime? RecalledAt { get; set; }

    public bool? DeletedForSender { get; set; }

    public bool? DeletedForReceiver { get; set; }

    public DateTime? ReadAt { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool? IsDeleted { get; set; }

    public virtual ChatRoom? Room { get; set; }

    public virtual User? Sender { get; set; }
}
