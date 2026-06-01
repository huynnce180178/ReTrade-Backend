using System;
using System.Collections.Generic;

namespace RetradeBE.Models;

public partial class Category
{
    public string CategoryId { get; set; } = null!;

    public string? Name { get; set; }

    public string? Description { get; set; }

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Attribute> Attributes { get; set; } = new List<Attribute>();

    public virtual ICollection<CategoryImage> CategoryImages { get; set; } = new List<CategoryImage>();

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();

    public virtual ICollection<UserFavorite> UserFavorites { get; set; } = new List<UserFavorite>();

    public virtual ICollection<UserSearch> UserSearches { get; set; } = new List<UserSearch>();
}
