using System;
using System.Collections.Generic;

namespace RetradeBE.Models;

public partial class Review
{
    public string ReviewId { get; set; } = null!;

    public string? ReviewerId { get; set; }

    public string? SellerId { get; set; }

    public string? OrderId { get; set; }

    public int? Rating { get; set; }

    public string? Comment { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Order? Order { get; set; }

    public virtual ICollection<ReviewReport> ReviewReport { get; set; } = new List<ReviewReport>();

    public virtual User? Reviewer { get; set; }

    public virtual User? Seller { get; set; }
}
