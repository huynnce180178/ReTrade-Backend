using System;
using System.Collections.Generic;

namespace RetradeBE.Models;

public partial class ProductAttribute
{
    public string ProductAttributeId { get; set; } = null!;

    public string? ProductId { get; set; }

    public string? AttributeId { get; set; }

    public string? Value { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool? IsDeleted { get; set; }

    public virtual Attribute? Attribute { get; set; }

    public virtual Product? Product { get; set; }
}
