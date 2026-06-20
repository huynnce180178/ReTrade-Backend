using RetradeBE.Services;

namespace RetradeBE.Services.BackgroundJobs
{
    public class ShippingOutcomeSimulationService : BackgroundService
    {
        private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(5);

        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ShippingOutcomeSimulationService> _logger;

        public ShippingOutcomeSimulationService(
            IServiceProvider serviceProvider,
            ILogger<ShippingOutcomeSimulationService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(CheckInterval);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessDueShippingOutcomesAsync(stoppingToken);
                    await timer.WaitForNextTickAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while simulating shipping outcomes.");
                    await Task.Delay(CheckInterval, stoppingToken);
                }
            }
        }

        private async Task ProcessDueShippingOutcomesAsync(CancellationToken stoppingToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();

            var processedCount = await orderService.ProcessDueShippingOutcomesAsync(stoppingToken);
            if (processedCount > 0)
            {
                _logger.LogInformation("Processed {ProcessedCount} simulated shipping outcomes.", processedCount);
            }
        }
    }
}
