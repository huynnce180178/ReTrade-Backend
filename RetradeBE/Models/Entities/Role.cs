using System;
using System.Collections.Generic;

namespace RetradeBE.Models;

public partial class Role
{
    public int RoleId { get; set; }


    public string? Name { get; set; }

    public virtual ICollection<AccountRole> AccountRole { get; set; } = new List<AccountRole>();
}
