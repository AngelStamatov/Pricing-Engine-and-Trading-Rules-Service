using PricingAndTrading.Domain.Prices;

namespace PricingAndTrading.Application.Pricing;

public interface ILatestPriceSnapshotProvider
{
    IReadOnlyCollection<MarketPrice> GetSnapshot();
}
