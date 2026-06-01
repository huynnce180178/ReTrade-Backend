using System;
using System.Collections.Generic;

namespace RetradeBE.Models;

public partial class ProductImage
{
    public string ProductId { get; set; } = null!;

    public string ImageId { get; set; } = null!;

    public bool? IsMain { get; set; }

    public int? SortOrder { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Image Image { get; set; } = null!;

    public virtual Product Product { get; set; } = null!;
}
