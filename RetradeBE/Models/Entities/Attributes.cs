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

    //VALIDATION (number only)
    public decimal? MinValue { get; set; }
    public decimal? MaxValue { get; set; }

    // DISPLAY INFO
    public string? Unit { get; set; }
    public int? DisplayOrder { get; set; }

    // UI CONTROL (filter/search)
    public bool IsFilterable { get; set; }
    public bool IsSearchable { get; set; }

    // AUDIT
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool? IsDeleted { get; set; }

    //RELATIONSHIP
    public virtual Category? Category { get; set; }
    public virtual ICollection<ProductAttribute> ProductAttribute { get; set; }
        = new List<ProductAttribute>();
}