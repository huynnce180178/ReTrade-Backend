using System;
using System.Collections.Generic;

namespace RetradeBE.Models;

public partial class UserReport
{
    public string ReportId { get; set; } = null!;

    public string? ReporterId { get; set; }

    public string? TargetType { get; set; }

    public string? TargetId { get; set; }

    public string? Reason { get; set; }

    public string? Description { get; set; }

    public string? Status { get; set; }

    public string? ReviewedBy { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual User? Reporter { get; set; }

    public virtual User? ReviewedByNavigation { get; set; }
}
