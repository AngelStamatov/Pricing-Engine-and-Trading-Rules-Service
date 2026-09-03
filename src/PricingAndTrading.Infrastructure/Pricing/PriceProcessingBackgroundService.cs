using Microsoft.Extensions.Hosting;
using PricingAndTrading.Application.Pricing;

namespace PricingAndTrading.Infrastructure.Pricing;

public sealed class PriceProcessingBackgroundService : BackgroundService
{
    private readonly IPriceFeed _priceFeed;
    private readonly IPriceProcessor _priceProcessor;

    public PriceProcessingBackgroundService(
        IPriceFeed priceFeed,
        IPriceProcessor priceProcessor)
    {
        ArgumentNullException.ThrowIfNull(priceFeed);
        ArgumentNullException.ThrowIfNull(priceProcessor);

        _priceFeed = priceFeed;
        _priceProcessor = priceProcessor;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var priceTick in _priceFeed.ReadAllAsync(stoppingToken))
        {
            await _priceProcessor.ProcessAsync(priceTick, stoppingToken);
        }
    }
}
