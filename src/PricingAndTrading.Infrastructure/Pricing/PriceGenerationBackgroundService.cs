using Microsoft.Extensions.Hosting;
using PricingAndTrading.Application.Pricing;

namespace PricingAndTrading.Infrastructure.Pricing;

public sealed class PriceGenerationBackgroundService : BackgroundService
{
    public static readonly TimeSpan GenerationInterval = TimeSpan.FromMilliseconds(100);

    private static readonly (string Symbol, decimal InitialMarketPrice)[] Markets =
    [
        ("EURUSD", 1.085m),
        ("GBPUSD", 1.275m),
        ("USDJPY", 147.5m),
        ("USDCHF", 0.88m),
        ("AUDUSD", 0.66m),
        ("USDCAD", 1.35m),
        ("NZDUSD", 0.61m),
        ("EURGBP", 0.85m),
        ("EURJPY", 160m),
        ("GBPJPY", 188m)
    ];

    private readonly IPriceTickPublisher _pricePublisher;
    private readonly SimulatedPriceGenerator _priceGenerator;
    private readonly ILatestPriceProvider _latestPriceProvider;

    public PriceGenerationBackgroundService(
        IPriceTickPublisher pricePublisher,
        SimulatedPriceGenerator priceGenerator,
        ILatestPriceProvider latestPriceProvider)
    {
        ArgumentNullException.ThrowIfNull(pricePublisher);
        ArgumentNullException.ThrowIfNull(priceGenerator);
        ArgumentNullException.ThrowIfNull(latestPriceProvider);

        _pricePublisher = pricePublisher;
        _priceGenerator = priceGenerator;
        _latestPriceProvider = latestPriceProvider;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        IEnumerable<Task> producerTasks = Markets.Select(market =>
            GeneratePricesAsync(
                market.Symbol,
                _latestPriceProvider.GetLatest(market.Symbol)?.CurrentMarketPrice
                    ?? market.InitialMarketPrice,
                stoppingToken));

        return Task.WhenAll(producerTasks);
    }

    private async Task GeneratePricesAsync(
        string symbol,
        decimal currentMarketPrice,
        CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var priceTick = _priceGenerator.Generate(symbol, currentMarketPrice);
            await _pricePublisher.PublishAsync(priceTick, cancellationToken);

            currentMarketPrice = (priceTick.BidPrice + priceTick.AskPrice) / 2m;
            await Task.Delay(GenerationInterval, cancellationToken);
        }
    }
}
