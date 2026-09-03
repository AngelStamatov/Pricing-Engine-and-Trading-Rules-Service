using PricingAndTrading.Domain.Prices;

namespace PricingAndTrading.Application.Pricing;

public interface IPriceFeed
{
    IAsyncEnumerable<PriceTick> ReadAllAsync(
        CancellationToken cancellationToken);
}
