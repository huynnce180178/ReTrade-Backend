using System;
using System.Collections.Generic;

namespace RetradeBE.Models;

public partial class MyVoucher
{
    public string UserVoucherId { get; set; } = null!;

    public string? UserId { get; set; }

    public string? VoucherId { get; set; }

    public string? Status { get; set; }

    public DateTime? UsedAt { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual User? User { get; set; }

    public virtual Voucher? Voucher { get; set; }
}
