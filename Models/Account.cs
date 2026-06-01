using System;
using System.Collections.Generic;

namespace RetradeBE.Models;

public partial class Account
{
    public string AccountId { get; set; } = null!;

    public string? UserId { get; set; }

    public string? Provider { get; set; }

    public string? Username { get; set; }

    public string? ProviderUserId { get; set; }

    public string? PasswordHash { get; set; }

    public string? Status { get; set; }

    public DateTime? LastLoginAt { get; set; }

    public bool? IsDeleted { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<AccountRole> AccountRoles { get; set; } = new List<AccountRole>();

    public virtual User? User { get; set; }
}
