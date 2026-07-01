using System;
using System.Collections.Generic;

namespace RetradeBE.Models;

public partial class Report
{
    public string ReportId { get; set; } = null!;

    public string ReporterId { get; set; } = null!;

    public string TargetType { get; set; } = null!;

    public string TargetId { get; set; } = null!;

    public string? Reason { get; set; }

    public string? Description { get; set; }

    public string? Status { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual User Reporter { get; set; } = null!;
}

