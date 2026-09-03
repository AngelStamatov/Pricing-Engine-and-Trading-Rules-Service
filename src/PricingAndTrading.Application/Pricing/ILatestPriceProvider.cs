using PricingAndTrading.Domain.Prices;

namespace PricingAndTrading.Application.Pricing;

public interface ILatestPriceProvider
{
    MarketPrice? GetLatest(string symbol);
}
