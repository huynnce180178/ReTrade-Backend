using System;
using System.Collections.Generic;

namespace RetradeBE.Models.DTOs
{
    public class ProductAttributeValueDto
    {
        public string? AttributeId { get; set; }
        public string? AttributeName { get; set; }
        public string? Value { get; set; }
        public string? DataType { get; set; }
        public string? Unit { get; set; }
    }

    public class ProductImageDto
    {
        public string? ImageId { get; set; }
        public string? ImageUrl { get; set; }
        public string? AltText { get; set; }
        public bool? IsMain { get; set; }
        public int? SortOrder { get; set; }
    }

    public class ProductCreateDto
    {
        public string CategoryId { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public string? Condition { get; set; }
        public decimal? Price { get; set; }
        public int? StockQuantity { get; set; }
        public int? WeightGram { get; set; }
        public int? LengthCm { get; set; }
        public int? WidthCm { get; set; }
        public int? HeightCm { get; set; }
        public bool IsForAuction { get; set; }
        public List<ProductAttributeValueDto> Attributes { get; set; } = new List<ProductAttributeValueDto>();
        public List<ProductImageDto> Images { get; set; } = new List<ProductImageDto>();
    }

    public class ProductUpdateDto
    {
        public string CategoryId { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public string? Condition { get; set; }
        public decimal? Price { get; set; }
        public int? StockQuantity { get; set; }
        public int? WeightGram { get; set; }
        public int? LengthCm { get; set; }
        public int? WidthCm { get; set; }
        public int? HeightCm { get; set; }
        public List<ProductAttributeValueDto> Attributes { get; set; } = new List<ProductAttributeValueDto>();
        public List<ProductImageDto> Images { get; set; } = new List<ProductImageDto>();
    }

    public class ProductResponseDto
    {
        public string ProductId { get; set; } = null!;
        public string? SellerId { get; set; }
        public string? SellerName { get; set; }
        public string? CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? Condition { get; set; }
        public decimal? Price { get; set; }
        public int? StockQuantity { get; set; }
        public int? WeightGram { get; set; }
        public int? LengthCm { get; set; }
        public int? WidthCm { get; set; }
        public int? HeightCm { get; set; }
        public string? Status { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<ProductImageDto> Images { get; set; } = new List<ProductImageDto>();
        public List<ProductAttributeValueDto> Attributes { get; set; } = new List<ProductAttributeValueDto>();
    }

    public class ProductListDto
    {
        public string ProductId { get; set; } = null!;
        public string? Name { get; set; }
        public string? CategoryName { get; set; }
        public decimal? Price { get; set; }
        public int? StockQuantity { get; set; }
        public string? Status { get; set; }
        public string? MainImageUrl { get; set; }
        public string? SellerName { get; set; }
        public string? SellerId { get; set; }
        public string? Condition { get; set; }
        public DateTime? CreatedAt { get; set; }
    }

    public class ProductSearchQueryDto
    {
        public string? CategoryId { get; set; }
        public string? SearchTerm { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public string? Condition { get; set; }
        public string? Status { get; set; }
        public string? SellerId { get; set; }
        public string? SortBy { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 12;
        public bool? IsPriorityOnly { get; set; }
    }

    public class PagedResultDto<T>
    {
        public List<T> Items { get; set; } = new List<T>();
        public int TotalItems { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }

    public class AdminProductApprovalDto
    {
        public bool IsApproved { get; set; }
        public string? RejectReason { get; set; }
    }
}
