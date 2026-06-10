using Microsoft.EntityFrameworkCore;
using RetradeBE.Data;
using RetradeBE.Models;

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
                    await CheckAndExpireSubscriptionsAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while executing SubscriptionExpirationService.");
                }

                // Wait for 1 hour before checking again. 
                // For testing purposes, you might want to lower this (e.g., TimeSpan.FromMinutes(1))
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }

            _logger.LogInformation("SubscriptionExpirationService is stopping.");
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

                if (subscription.ServiceId == "SERVICE_UPGRADE_SELLER")
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
