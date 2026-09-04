using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PricingAndTrading.Application.Abstractions;
using PricingAndTrading.Application.Pricing;
using PricingAndTrading.Domain.Prices;

namespace PricingAndTrading.Infrastructure.Persistence;

internal sealed class LatestPricePersistenceBackgroundService : BackgroundService
{
    internal static readonly TimeSpan PersistenceInterval = TimeSpan.FromSeconds(1);

    private readonly ILatestPriceSnapshotProvider _snapshotProvider;
    private readonly IPriceStateRepository _priceStateRepository;
    private readonly ILogger<LatestPricePersistenceBackgroundService> _logger;

    public LatestPricePersistenceBackgroundService(
        ILatestPriceSnapshotProvider snapshotProvider,
        IPriceStateRepository priceStateRepository,
        ILogger<LatestPricePersistenceBackgroundService> logger)
    {
        ArgumentNullException.ThrowIfNull(snapshotProvider);
        ArgumentNullException.ThrowIfNull(priceStateRepository);
        ArgumentNullException.ThrowIfNull(logger);

        _snapshotProvider = snapshotProvider;
        _priceStateRepository = priceStateRepository;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(PersistenceInterval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await PersistSnapshotAsync(stoppingToken);
        }
    }

    internal async Task PersistSnapshotAsync(CancellationToken stoppingToken)
    {
        IReadOnlyCollection<MarketPrice> snapshot =
            _snapshotProvider.GetSnapshot();

        if (snapshot.Count == 0)
        {
            return;
        }

        try
        {
            await _priceStateRepository.UpsertLatestAsync(
                snapshot,
                stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Failed to persist a latest-price snapshot containing {PriceCount} prices. The next scheduled cycle will retry.",
                snapshot.Count);
        }
    }
}
