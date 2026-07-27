using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RetradeBE.Models;
using RetradeBE.Repositories;

namespace RetradeBE.Services
{
    public class SubscriptionVoucherService : ISubscriptionVoucherService
    {
        private readonly IVoucherRepository _voucherRepo;
        private readonly IMyVoucherRepository _myVoucherRepo;

        public SubscriptionVoucherService(
            IVoucherRepository voucherRepo,
            IMyVoucherRepository myVoucherRepo)
        {
            _voucherRepo = voucherRepo;
            _myVoucherRepo = myVoucherRepo;
        }

        private class VoucherTemplate
        {
            public string CodePrefix { get; set; } = null!;
            public string DiscountType { get; set; } = null!; // "Fixed" or "Percentage"
            public decimal DiscountValue { get; set; }
            public decimal MinOrderValue { get; set; }
            public decimal MaxDiscountValue { get; set; }
            public int StartOffsetDays { get; set; }
            public int ValidityDays { get; set; }
        }

        public async Task<List<Voucher>> GenerateSubscriptionVouchersAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return new List<Voucher>();

            // Avoid duplicating subscription vouchers for the same user
            var existingCount = await _myVoucherRepo.CountByUserIdAsync(userId);

            if (existingCount >= 30)
            {
                return new List<Voucher>();
            }

            var subStartDate = DateTime.UtcNow;
            var subEndDate = subStartDate.AddDays(30);

            // 30 Vouchers Configuration with high discount caps (Min 50k for % vouchers, 30k-50k for Freeship)
            var templates = new List<VoucherTemplate>
            {
                // Day 1 Immediate Vouchers (12 items)
                new VoucherTemplate { CodePrefix = "FS30K-1D", DiscountType = "Fixed", DiscountValue = 30000, MinOrderValue = 150000, MaxDiscountValue = 30000, StartOffsetDays = 0, ValidityDays = 1 },
                new VoucherTemplate { CodePrefix = "FS40K-1D", DiscountType = "Fixed", DiscountValue = 40000, MinOrderValue = 250000, MaxDiscountValue = 40000, StartOffsetDays = 0, ValidityDays = 1 },
                new VoucherTemplate { CodePrefix = "SUB2P-50K-1D", DiscountType = "Percentage", DiscountValue = 2, MinOrderValue = 300000, MaxDiscountValue = 50000, StartOffsetDays = 0, ValidityDays = 1 },
                new VoucherTemplate { CodePrefix = "FS30K-3D", DiscountType = "Fixed", DiscountValue = 30000, MinOrderValue = 200000, MaxDiscountValue = 30000, StartOffsetDays = 0, ValidityDays = 3 },
                new VoucherTemplate { CodePrefix = "FS40K-3D", DiscountType = "Fixed", DiscountValue = 40000, MinOrderValue = 300000, MaxDiscountValue = 40000, StartOffsetDays = 0, ValidityDays = 3 },
                new VoucherTemplate { CodePrefix = "FS45K-5D", DiscountType = "Fixed", DiscountValue = 45000, MinOrderValue = 400000, MaxDiscountValue = 45000, StartOffsetDays = 0, ValidityDays = 5 },
                new VoucherTemplate { CodePrefix = "FS50K-7D", DiscountType = "Fixed", DiscountValue = 50000, MinOrderValue = 500000, MaxDiscountValue = 50000, StartOffsetDays = 0, ValidityDays = 7 },
                new VoucherTemplate { CodePrefix = "SUB2P-50K-3D", DiscountType = "Percentage", DiscountValue = 2, MinOrderValue = 400000, MaxDiscountValue = 50000, StartOffsetDays = 0, ValidityDays = 3 },
                new VoucherTemplate { CodePrefix = "SUB5P-50K-3D", DiscountType = "Percentage", DiscountValue = 5, MinOrderValue = 350000, MaxDiscountValue = 50000, StartOffsetDays = 0, ValidityDays = 3 },
                new VoucherTemplate { CodePrefix = "SUB5P-60K-5D", DiscountType = "Percentage", DiscountValue = 5, MinOrderValue = 500000, MaxDiscountValue = 60000, StartOffsetDays = 0, ValidityDays = 5 },
                new VoucherTemplate { CodePrefix = "SUB7P-70K-5D", DiscountType = "Percentage", DiscountValue = 7, MinOrderValue = 600000, MaxDiscountValue = 70000, StartOffsetDays = 0, ValidityDays = 5 },
                new VoucherTemplate { CodePrefix = "FS35K-30D", DiscountType = "Fixed", DiscountValue = 35000, MinOrderValue = 200000, MaxDiscountValue = 35000, StartOffsetDays = 0, ValidityDays = 30 },

                // Progressive Unlock Vouchers (18 items)
                // Day 3 Unlocks (3 items)
                new VoucherTemplate { CodePrefix = "UP-FS30K-3D", DiscountType = "Fixed", DiscountValue = 30000, MinOrderValue = 200000, MaxDiscountValue = 30000, StartOffsetDays = 3, ValidityDays = 4 },
                new VoucherTemplate { CodePrefix = "UP-FS40K-3D", DiscountType = "Fixed", DiscountValue = 40000, MinOrderValue = 300000, MaxDiscountValue = 40000, StartOffsetDays = 3, ValidityDays = 7 },
                new VoucherTemplate { CodePrefix = "UP-SUB2P-50K", DiscountType = "Percentage", DiscountValue = 2, MinOrderValue = 500000, MaxDiscountValue = 50000, StartOffsetDays = 3, ValidityDays = 7 },

                // Day 5 Unlocks (3 items)
                new VoucherTemplate { CodePrefix = "UP-FS40K-5D", DiscountType = "Fixed", DiscountValue = 40000, MinOrderValue = 350000, MaxDiscountValue = 40000, StartOffsetDays = 5, ValidityDays = 7 },
                new VoucherTemplate { CodePrefix = "UP-FS45K-5D", DiscountType = "Fixed", DiscountValue = 45000, MinOrderValue = 500000, MaxDiscountValue = 45000, StartOffsetDays = 5, ValidityDays = 10 },
                new VoucherTemplate { CodePrefix = "UP-SUB5P-60K", DiscountType = "Percentage", DiscountValue = 5, MinOrderValue = 600000, MaxDiscountValue = 60000, StartOffsetDays = 5, ValidityDays = 10 },

                // Day 7 Unlocks (3 items)
                new VoucherTemplate { CodePrefix = "UP-FS45K-7D", DiscountType = "Fixed", DiscountValue = 45000, MinOrderValue = 500000, MaxDiscountValue = 45000, StartOffsetDays = 7, ValidityDays = 10 },
                new VoucherTemplate { CodePrefix = "UP-SUB5P-70K", DiscountType = "Percentage", DiscountValue = 5, MinOrderValue = 700000, MaxDiscountValue = 70000, StartOffsetDays = 7, ValidityDays = 10 },
                new VoucherTemplate { CodePrefix = "UP-SUB7P-80K", DiscountType = "Percentage", DiscountValue = 7, MinOrderValue = 800000, MaxDiscountValue = 80000, StartOffsetDays = 7, ValidityDays = 13 },

                // Day 10 Unlocks (3 items)
                new VoucherTemplate { CodePrefix = "UP-FS45K-10D", DiscountType = "Fixed", DiscountValue = 45000, MinOrderValue = 600000, MaxDiscountValue = 45000, StartOffsetDays = 10, ValidityDays = 10 },
                new VoucherTemplate { CodePrefix = "UP-FS50K-10D", DiscountType = "Fixed", DiscountValue = 50000, MinOrderValue = 700000, MaxDiscountValue = 50000, StartOffsetDays = 10, ValidityDays = 12 },
                new VoucherTemplate { CodePrefix = "UP-SUB2P-50K-10D", DiscountType = "Percentage", DiscountValue = 2, MinOrderValue = 600000, MaxDiscountValue = 50000, StartOffsetDays = 10, ValidityDays = 15 },

                // Day 15 Unlocks (3 items)
                new VoucherTemplate { CodePrefix = "UP-FS50K-15D", DiscountType = "Fixed", DiscountValue = 50000, MinOrderValue = 800000, MaxDiscountValue = 50000, StartOffsetDays = 15, ValidityDays = 15 },
                new VoucherTemplate { CodePrefix = "UP-SUB7P-90K", DiscountType = "Percentage", DiscountValue = 7, MinOrderValue = 1000000, MaxDiscountValue = 90000, StartOffsetDays = 15, ValidityDays = 13 },
                new VoucherTemplate { CodePrefix = "UP-SUB10P-100K", DiscountType = "Percentage", DiscountValue = 10, MinOrderValue = 1000000, MaxDiscountValue = 100000, StartOffsetDays = 15, ValidityDays = 15 }, // Exclusive 10%

                // Day 20 Unlocks (3 items)
                new VoucherTemplate { CodePrefix = "UP-FS40K-20D", DiscountType = "Fixed", DiscountValue = 40000, MinOrderValue = 400000, MaxDiscountValue = 40000, StartOffsetDays = 20, ValidityDays = 10 },
                new VoucherTemplate { CodePrefix = "UP-FS45K-20D", DiscountType = "Fixed", DiscountValue = 45000, MinOrderValue = 600000, MaxDiscountValue = 45000, StartOffsetDays = 20, ValidityDays = 10 },
                new VoucherTemplate { CodePrefix = "UP-FS50K-20D", DiscountType = "Fixed", DiscountValue = 50000, MinOrderValue = 900000, MaxDiscountValue = 50000, StartOffsetDays = 20, ValidityDays = 10 }
            };

            var newVouchers = new List<Voucher>();
            var newMyVouchers = new List<MyVoucher>();
            var random = new Random();

            foreach (var t in templates)
            {
                var randomSuffix = random.Next(10000, 99999).ToString("D5");
                var code = $"{t.CodePrefix}-{randomSuffix}";
                var voucherId = $"VOUCHER_{Guid.NewGuid():N}";
                var userVoucherId = $"MV_{Guid.NewGuid():N}";

                var startDate = subStartDate.AddDays(t.StartOffsetDays);
                var rawExpiry = startDate.AddDays(t.ValidityDays);
                var expirationDate = rawExpiry > subEndDate ? subEndDate : rawExpiry;

                var voucher = new Voucher
                {
                    VoucherId = voucherId,
                    SellerId = null, // Platform level subscription voucher
                    Code = code,
                    DiscountType = t.DiscountType,
                    DiscountValue = t.DiscountValue,
                    MinOrderValue = t.MinOrderValue,
                    MaxDiscountValue = t.MaxDiscountValue,
                    Quantity = 1,
                    StartDate = startDate,
                    ExpirationDate = expirationDate,
                    Status = "Active",
                    CreatedAt = subStartDate,
                    UpdatedAt = subStartDate
                };

                var myVoucher = new MyVoucher
                {
                    UserVoucherId = userVoucherId,
                    UserId = userId,
                    VoucherId = voucherId,
                    Status = "Active",
                    CreatedAt = subStartDate
                };

                newVouchers.Add(voucher);
                newMyVouchers.Add(myVoucher);
            }

            await _voucherRepo.AddRangeAsync(newVouchers);
            await _myVoucherRepo.AddRangeAsync(newMyVouchers);

            await _voucherRepo.SaveChangesAsync();
            await _myVoucherRepo.SaveChangesAsync();

            return newVouchers;
        }
    }
}
