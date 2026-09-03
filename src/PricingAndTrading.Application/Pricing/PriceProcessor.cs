using PricingAndTrading.Domain.Prices;

namespace PricingAndTrading.Application.Pricing;

public sealed class PriceProcessor : IPriceProcessor
{
    private readonly ILatestPriceStore _latestPriceStore;

    public PriceProcessor(ILatestPriceStore latestPriceStore)
    {
        ArgumentNullException.ThrowIfNull(latestPriceStore);

        _latestPriceStore = latestPriceStore;
    }

    public ValueTask ProcessAsync(
        PriceTick priceTick,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(priceTick);
        cancellationToken.ThrowIfCancellationRequested();

        MarketPrice marketPrice = MarketPrice.From(priceTick);
        _latestPriceStore.Update(marketPrice);

        return ValueTask.CompletedTask;
    }
}
