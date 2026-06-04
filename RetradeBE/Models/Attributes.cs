using System;
using System.Collections.Generic;

namespace RetradeBE.Models;

public partial class Attributes
{
    public string AttributeId { get; set; } = null!;

    public string? CategoryId { get; set; }

    public string? Name { get; set; }

    public string? DataType { get; set; }

    public bool? IsRequired { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool? IsDeleted { get; set; }

    public virtual Category? Category { get; set; }

    public virtual ICollection<ProductAttribute> ProductAttribute { get; set; } = new List<ProductAttribute>();
}
