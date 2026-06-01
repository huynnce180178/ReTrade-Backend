using System;
using System.Collections.Generic;

namespace RetradeBE.Models;

public partial class UserSearch
{
    public string SearchId { get; set; } = null!;

    public string? UserId { get; set; }

    public string? Keyword { get; set; }

    public string? CategoryId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Category? Category { get; set; }

    public virtual User? User { get; set; }
}
