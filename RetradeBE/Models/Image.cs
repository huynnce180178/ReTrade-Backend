using System;
using System.Collections.Generic;

namespace RetradeBE.Models;

public partial class Image
{
    public string ImageId { get; set; } = null!;

    public string? ImageUrl { get; set; }

    public string? AltText { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<CategoryImage> CategoryImage { get; set; } = new List<CategoryImage>();

    public virtual ICollection<ProductImage> ProductImage { get; set; } = new List<ProductImage>();
}
