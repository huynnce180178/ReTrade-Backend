using System;
using System.Collections.Generic;

namespace RetradeBE.Models;

public partial class Address
{
    public string AddressId { get; set; } = null!;

    public string? UserId { get; set; }

    public string? ReceiverName { get; set; }

    public string? ReceiverPhone { get; set; }

    public string? Street { get; set; }

    public int? ProvinceId { get; set; }

    public int? DistrictId { get; set; }

    public string? WardCode { get; set; }

    public bool? IsDefault { get; set; }

    public string? Status { get; set; }

    public bool? IsDeleted { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual User? User { get; set; }
}
