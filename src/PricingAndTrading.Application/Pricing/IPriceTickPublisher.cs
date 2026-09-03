using PricingAndTrading.Domain.Prices;

namespace PricingAndTrading.Application.Pricing;

public interface IPriceTickPublisher
{
    ValueTask PublishAsync(
        PriceTick priceTick,
        CancellationToken cancellationToken = default);
}
