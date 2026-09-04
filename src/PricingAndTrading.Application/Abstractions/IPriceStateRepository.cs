using PricingAndTrading.Domain.Prices;

namespace PricingAndTrading.Application.Abstractions;

public interface IPriceStateRepository
{
    Task<IReadOnlyList<MarketPrice>> GetAllAsync(
        CancellationToken cancellationToken);

    Task UpsertLatestAsync(
        IReadOnlyCollection<MarketPrice> prices,
        CancellationToken cancellationToken);
}
