using System;
using System.Collections.Generic;

namespace RetradeBE.Models;

public partial class AccountRole
{
    public string AccountId { get; set; } = null!;

    public int RoleId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Account Account { get; set; } = null!;

    public virtual Role Role { get; set; } = null!;
}
