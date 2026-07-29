using Microsoft.EntityFrameworkCore;
using RetradeBE.Data;
using RetradeBE.Models;
using RetradeBE.Models.DTOs;
using RetradeBE.Models.Enums;

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
            var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

            await CheckAndExpireVouchersStaticAsync(context, stoppingToken);
            await CheckAndNotifyExpiringVouchersAsync(context, notificationService, stoppingToken);
        }

        private async Task CheckAndNotifyExpiringVouchersAsync(AppDbContext context, INotificationService notificationService, CancellationToken stoppingToken)
        {
            var now = DateTime.UtcNow;
            var tomorrow = now.AddDays(1);

            // Find Active MyVouchers that expire in the next 24 hours
            var expiringVouchers = await context.MyVoucher
                .Include(mv => mv.Voucher)
                .Where(mv => mv.Status == "Active" && mv.UsedAt == null && mv.Voucher != null && mv.Voucher.ExpirationDate.HasValue && mv.Voucher.ExpirationDate.Value > now && mv.Voucher.ExpirationDate.Value <= tomorrow)
                .ToListAsync(stoppingToken);

            foreach (var mv in expiringVouchers)
            {
                if (string.IsNullOrWhiteSpace(mv.UserId)) continue;

                var alreadyNotified = await context.Notification
                    .AnyAsync(n => n.ReferenceId == mv.UserVoucherId && n.Title == "Voucher Expiring Soon", stoppingToken);

                if (!alreadyNotified)
                {
                    try
                    {
                        await notificationService.CreateAndSendAsync(new CreateNotificationDto
                        {
                            UserId = mv.UserId,
                            Title = "Voucher Expiring Soon",
                            Message = "You have a voucher expiring tomorrow. Use it now before it's gone!",
                            Type = nameof(NotificationTypeEnum.Voucher),
                            ReferenceId = mv.UserVoucherId
                        });
                        _logger.LogInformation($"Sent voucher expiration warning to UserId {mv.UserId} for voucher {mv.UserVoucherId}.");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Failed to send voucher expiration notification to UserId {mv.UserId}");
                    }
                }
            }
        }
    }
}
