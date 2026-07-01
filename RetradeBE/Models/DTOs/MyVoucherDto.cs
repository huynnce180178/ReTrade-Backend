using System;

namespace RetradeBE.Models.DTOs
{
    public class MyVoucherDto
    {
        public string UserVoucherId { get; set; } = null!;
        public string? UserId { get; set; }
        public string? VoucherId { get; set; }
        public string? Status { get; set; }
        public DateTime? UsedAt { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Flattened properties from Voucher
        public string? Code { get; set; }
        public string? DiscountType { get; set; }
        public decimal? DiscountValue { get; set; }
        public decimal? MinOrderValue { get; set; }
        public decimal? MaxDiscountValue { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? ExpirationDate { get; set; }
        public string? VoucherStatus { get; set; }
        public string? SellerId { get; set; }
        public string? SellerName { get; set; }
    }
}
