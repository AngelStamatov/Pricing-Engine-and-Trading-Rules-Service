using PricingAndTrading.Domain.Prices;

namespace PricingAndTrading.Application.Pricing;

public interface ILatestPriceStore : ILatestPriceProvider
{
    /// <summary>
    /// Atomically replaces the latest price and returns the previous value,
    /// or <see langword="null"/> when the symbol has no previous value.
    /// </summary>
    MarketPrice? Update(MarketPrice marketPrice);
}
