using Microsoft.EntityFrameworkCore;
using RetradeBE.Data;
using RetradeBE.Models;

namespace RetradeBE.Services.BackgroundJobs
{
    public class VoucherExpirationService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<VoucherExpirationService> _logger;

        public VoucherExpirationService(IServiceProvider serviceProvider, ILogger<VoucherExpirationService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("VoucherExpirationService is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckAndExpireVouchersAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while executing VoucherExpirationService.");
                }

                // Check every 30 minutes
                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }

            _logger.LogInformation("VoucherExpirationService is stopping.");
        }

        public static async Task CheckAndExpireVouchersStaticAsync(AppDbContext context, CancellationToken stoppingToken = default)
        {
            var now = DateTime.UtcNow;

            // Expire master vouchers
            var expiredVouchers = await context.Voucher
                .Where(v => v.Status == "Active" && v.ExpirationDate.HasValue && v.ExpirationDate.Value <= now)
                .ToListAsync(stoppingToken);

            foreach (var v in expiredVouchers)
            {
                v.Status = "Expired";
                v.UpdatedAt = now;
            }

            // Expire MyVoucher items
            var expiredMyVouchers = await context.MyVoucher
                .Include(mv => mv.Voucher)
                .Where(mv => mv.Status == "Active" && mv.UsedAt == null && mv.Voucher != null && mv.Voucher.ExpirationDate.HasValue && mv.Voucher.ExpirationDate.Value <= now)
                .ToListAsync(stoppingToken);

            foreach (var mv in expiredMyVouchers)
            {
                mv.Status = "Expired";
            }

            if (expiredVouchers.Any() || expiredMyVouchers.Any())
            {
                await context.SaveChangesAsync(stoppingToken);
            }
        }

        private async Task CheckAndExpireVouchersAsync(CancellationToken stoppingToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            await CheckAndExpireVouchersStaticAsync(context, stoppingToken);
        }
    }
}
