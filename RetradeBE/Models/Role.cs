using System;
using System.Collections.Generic;

namespace RetradeBE.Models;

public partial class Role
{
    public string RoleId { get; set; } = null!;

    public string? Name { get; set; }

    public string? Description { get; set; }

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<AccountRole> AccountRole { get; set; } = new List<AccountRole>();
}
