using System;
using System.Collections.Generic;

namespace RetradeBE.Models;

public partial class Voucher
{
    public string VoucherId { get; set; } = null!;

    public string? Code { get; set; }

    public string? DiscountType { get; set; }

    public decimal? DiscountValue { get; set; }

    public decimal? MinOrderValue { get; set; }

    public decimal? MaxDiscountValue { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? ExpirationDate { get; set; }

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<MyVoucher> MyVouchers { get; set; } = new List<MyVoucher>();
}
