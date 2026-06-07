using System;
using System.Collections.Generic;

namespace RetradeBE.Models;

public partial class Category
{
    public string CategoryId { get; set; } = null!;

    public string? ParentId { get; set; }

    public string? Name { get; set; }

    public string? Description { get; set; }

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Attributes> Attributes { get; set; } = new List<Attributes>();

    public virtual ICollection<CategoryImage> CategoryImage { get; set; } = new List<CategoryImage>();

    public virtual ICollection<Category> InverseParent { get; set; } = new List<Category>();

    public virtual Category? Parent { get; set; }

    public virtual ICollection<Product> Product { get; set; } = new List<Product>();

    public virtual ICollection<UserFavorite> UserFavorite { get; set; } = new List<UserFavorite>();

    public virtual ICollection<UserSearch> UserSearch { get; set; } = new List<UserSearch>();
}
