using System;
using System.Collections.Generic;

namespace RetradeBE.Models;

public partial class UserFavorite
{
    public string FavoriteId { get; set; } = null!;

    public string? UserId { get; set; }

    public string? CategoryId { get; set; }

    public string? ProductId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Category? Category { get; set; }

    public virtual Product? Product { get; set; }

    public virtual User? User { get; set; }
}

