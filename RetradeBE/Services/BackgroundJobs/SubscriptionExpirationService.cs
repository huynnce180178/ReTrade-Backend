using Microsoft.EntityFrameworkCore;
using RetradeBE.Data;
using RetradeBE.Models;
using RetradeBE.Models.DTOs;
using RetradeBE.Models.Enums;

namespace RetradeBE.Services.BackgroundJobs
{
    public class SubscriptionExpirationService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<SubscriptionExpirationService> _logger;

        public SubscriptionExpirationService(IServiceProvider serviceProvider, ILogger<SubscriptionExpirationService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("SubscriptionExpirationService is starting.");

            // Loop until the application is stopped
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckAndNotifyExpiringSubscriptionsAsync(stoppingToken);
                    await CheckAndExpireSubscriptionsAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while executing SubscriptionExpirationService.");
                }

                // Wait for 1 hour before checking again. 
                // For testing purposes, you might want to lower this (e.g., TimeSpan.FromMinutes(1))
                try
                {
                    await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }

            _logger.LogInformation("SubscriptionExpirationService is stopping.");
        }

        private async Task CheckAndNotifyExpiringSubscriptionsAsync(CancellationToken stoppingToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

            var now = DateTime.UtcNow;
            var tomorrow = now.AddDays(1);

            // Find active subscriptions expiring in the next 24 hours
            var expiringSubscriptions = await context.MyService
                .Include(s => s.Service)
                .Where(s => s.Status == "Active" && s.EndDate.HasValue && s.EndDate.Value > now && s.EndDate.Value <= tomorrow)
                .ToListAsync(stoppingToken);

            if (!expiringSubscriptions.Any())
            {
                return;
            }

            foreach (var subscription in expiringSubscriptions)
            {
                if (string.IsNullOrWhiteSpace(subscription.UserId)) continue;

                // Check if a warning notification was already sent for this subscription
                var alreadyNotified = await context.Notification
                    .AnyAsync(n => n.ReferenceId == subscription.UserSubId && n.Title == "Subscription Expiring Soon", stoppingToken);

                if (!alreadyNotified)
                {
                    var serviceName = subscription.Service?.Name ?? "Service package";
                    try
                    {
                        await notificationService.CreateAndSendAsync(new CreateNotificationDto
                        {
                            UserId = subscription.UserId,
                            Title = "Subscription Expiring Soon",
                            Message = $"Your {serviceName} will expire tomorrow. Please renew it to continue enjoying the benefits.",
                            Type = nameof(NotificationTypeEnum.Subscription),
                            ReferenceId = subscription.UserSubId
                        });
                        _logger.LogInformation($"Sent expiration warning to UserId {subscription.UserId} for subscription {subscription.UserSubId}.");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Failed to send expiration warning for subscription {subscription.UserSubId}.");
                    }
                }
            }
        }

        private async Task CheckAndExpireSubscriptionsAsync(CancellationToken stoppingToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var now = DateTime.UtcNow;

            // Find all active subscriptions that have passed their EndDate
            var expiredSubscriptions = await context.MyService
                .Where(s => s.Status == "Active" && s.EndDate.HasValue && s.EndDate.Value <= now)
                .ToListAsync(stoppingToken);

            if (!expiredSubscriptions.Any())
            {
                return;
            }

            _logger.LogInformation($"Found {expiredSubscriptions.Count} expired subscriptions. Processing...");

            foreach (var subscription in expiredSubscriptions)
            {
                subscription.Status = "Expired";
                subscription.UpdatedAt = now;

                if (subscription.ServiceId == "SERVICE_UPGRADE_SELLER" || subscription.ServiceId == "sub_20260701_100001")
                {
                    // Find the account associated with the user
                    var account = await context.Account
                        .FirstOrDefaultAsync(a => a.UserId == subscription.UserId, stoppingToken);

                    if (account != null)
                    {
                        var targetRole = await context.Role
                            .AsNoTracking()
                            .FirstOrDefaultAsync(r => r.Name != null && r.Name.ToLower() == "seller", stoppingToken);

                        if (targetRole == null)
                        {
                            targetRole = await context.Role.AsNoTracking().FirstOrDefaultAsync(r => r.RoleId == 3, stoppingToken);
                        }

                        if (targetRole != null)
                        {
                            // Check if the user has this role and remove it
                            var accountRole = await context.AccountRole
                                .FirstOrDefaultAsync(ar => ar.AccountId == account.AccountId && ar.RoleId == targetRole.RoleId, stoppingToken);

                            if (accountRole != null)
                            {
                                context.AccountRole.Remove(accountRole);
                                _logger.LogInformation($"Revoked role 'Seller' from AccountId {account.AccountId} due to expired subscription {subscription.UserSubId}.");
                            }
                        }
                    }
                }
            }

            await context.SaveChangesAsync(stoppingToken);
            _logger.LogInformation("Finished processing expired subscriptions.");
        }
    }
}
