using System;
using System.Collections.Generic;

namespace RetradeBE.Models;

public partial class CategoryImage
{
    public string CategoryId { get; set; } = null!;

    public string ImageId { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public virtual Category Category { get; set; } = null!;

    public virtual Image Image { get; set; } = null!;
}
