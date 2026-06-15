namespace RetradeBE.Models.DTOs
{
    /// <summary>
    /// Attribute DTO cho Category
    /// </summary>
    public class AttributeDto
    {
        public string? AttributeId { get; set; }
        public string? Name { get; set; }
        public string? DataType { get; set; }
        public bool? IsRequired { get; set; }
        public decimal? MinValue { get; set; }
        public decimal? MaxValue { get; set; }
        public string? Unit { get; set; }
        public int? DisplayOrder { get; set; }
        public bool? IsFilterable { get; set; }
        public bool? IsSearchable { get; set; }
    }

    /// <summary>
    /// DTO cho tạo mới Category cùng Attributes
    /// </summary>
    public class CategoryCreateDto
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? ParentId { get; set; }
        public List<AttributeCreateDto>? Attributes { get; set; } = new List<AttributeCreateDto>();
    }

    /// <summary>
    /// DTO cho tạo mới Attribute
    /// </summary>
    public class AttributeCreateDto
    {
        public string? Name { get; set; }
        public string? DataType { get; set; }
        public bool? IsRequired { get; set; }
        public decimal? MinValue { get; set; }
        public decimal? MaxValue { get; set; }
        public string? Unit { get; set; }
        public int? DisplayOrder { get; set; }
        public bool? IsFilterable { get; set; }
        public bool? IsSearchable { get; set; }
    }

    /// <summary>
    /// DTO cho cập nhật Category cùng Attributes
    /// </summary>
    public class CategoryUpdateDto
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? ParentId { get; set; }
        public List<AttributeUpdateDto>? Attributes { get; set; } = new List<AttributeUpdateDto>();
    }

    /// <summary>
    /// DTO cho cập nhật Attribute
    /// </summary>
    public class AttributeUpdateDto
    {
        public string? AttributeId { get; set; }
        public string? Name { get; set; }
        public string? DataType { get; set; }
        public bool? IsRequired { get; set; }
        public decimal? MinValue { get; set; }
        public decimal? MaxValue { get; set; }
        public string? Unit { get; set; }
        public int? DisplayOrder { get; set; }
        public bool? IsFilterable { get; set; }
        public bool? IsSearchable { get; set; }
    }

    /// <summary>
    /// DTO cho response Category (kèm Attributes)
    /// </summary>
    public class CategoryResponseDto
    {
        public string? CategoryId { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? Status { get; set; }
        public string? ParentId { get; set; }
        public string? ImageUrl { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<AttributeDto>? Attributes { get; set; } = new List<AttributeDto>();
    }
}
