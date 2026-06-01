using System;
using System.Collections.Generic;

namespace RetradeBE.Models;

public partial class MyService
{
    public string UserSubId { get; set; } = null!;

    public string? UserId { get; set; }

    public string? ServiceId { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ServiceSubscription? Service { get; set; }

    public virtual User? User { get; set; }
}
