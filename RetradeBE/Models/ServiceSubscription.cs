using System;
using System.Collections.Generic;

namespace RetradeBE.Models;

public partial class ServiceSubscription
{
    public string ServiceId { get; set; } = null!;

    public string? Name { get; set; }

    public string? TargetRole { get; set; }

    public decimal? Price { get; set; }

    public int? DurationDays { get; set; }

    public string? BenefitsDescription { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<MyService> MyServices { get; set; } = new List<MyService>();
}
