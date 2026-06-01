using System;
using System.Collections.Generic;

namespace RetradeBE.Models;

public partial class Notification
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

    public bool? IsDeleted { get; set; }

    public virtual User? User { get; set; }
}
