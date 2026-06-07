using System;
using System.Collections.Generic;

namespace RetradeBE.Models;

public partial class Banner
{
    public string BannerId { get; set; } = null!;

    public string? Title { get; set; }

    public string? ImageUrl { get; set; }

    public string? RedirectUrl { get; set; }

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }
}
