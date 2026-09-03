using PricingAndTrading.Domain.Prices;

namespace PricingAndTrading.Infrastructure.Pricing;

public sealed class SimulatedPriceGenerator
{
    private const decimal MaximumMovementFraction = 0.0005m;
    private const decimal HalfSpreadFraction = 0.00005m;

    public PriceTick Generate(string symbol, decimal previousMarketPrice)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(previousMarketPrice);

        decimal randomOffset = (decimal)((Random.Shared.NextDouble() * 2d) - 1d);
        decimal currentMarketPrice = previousMarketPrice *
            (1m + (randomOffset * MaximumMovementFraction));
        decimal halfSpread = currentMarketPrice * HalfSpreadFraction;

        return new PriceTick(
            symbol,
            currentMarketPrice - halfSpread,
            currentMarketPrice + halfSpread,
            DateTimeOffset.UtcNow);
    }
}
