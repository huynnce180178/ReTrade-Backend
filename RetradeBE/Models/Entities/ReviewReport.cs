using System;

namespace RetradeBE.Models;

public partial class ReviewReport
{
    public string ReviewReportId { get; set; } = null!;

    public string ReviewId { get; set; } = null!;

    public string ReporterId { get; set; } = null!;

    public string? Reason { get; set; }

    public string? Description { get; set; }

    public string? Status { get; set; }

    public string? ReviewedBy { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Review Review { get; set; } = null!;

    public virtual User Reporter { get; set; } = null!;

    public virtual User? ReviewedByNavigation { get; set; }
}
