using RetradeBE.Services;

namespace RetradeBE.Services.BackgroundJobs
{
    public class AuctionClosingService : BackgroundService
    {
        private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(30);
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AuctionClosingService> _logger;

        public AuctionClosingService(IServiceScopeFactory scopeFactory, ILogger<AuctionClosingService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var auctionService = scope.ServiceProvider.GetRequiredService<IAuctionService>();
                    var processed = await auctionService.ProcessDueAuctionsAsync(stoppingToken);
                    if (processed > 0)
                    {
                        _logger.LogInformation("Processed {Count} due auctions.", processed);
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process due auctions.");
                }

                await Task.Delay(PollInterval, stoppingToken);
            }
        }
    }
}
