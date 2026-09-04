using System.Collections.Concurrent;
using PricingAndTrading.Application.Pricing;
using PricingAndTrading.Domain.Prices;

namespace PricingAndTrading.Infrastructure.RuntimeState;

public sealed class LatestPriceStore :
    ILatestPriceStore,
    ILatestPriceSnapshotProvider
{
    private readonly ConcurrentDictionary<string, MarketPrice> _prices =
        new(StringComparer.Ordinal);

    public MarketPrice? GetLatest(string symbol)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);

        string normalizedSymbol = symbol.Trim().ToUpperInvariant();
        return _prices.TryGetValue(normalizedSymbol, out MarketPrice? price)
            ? price
            : null;
    }

    public MarketPrice? Update(MarketPrice marketPrice)
    {
        ArgumentNullException.ThrowIfNull(marketPrice);

        while (true)
        {
            if (_prices.TryGetValue(marketPrice.Symbol, out MarketPrice? previous))
            {
                if (_prices.TryUpdate(marketPrice.Symbol, marketPrice, previous))
                {
                    return previous;
                }

                continue;
            }

            if (_prices.TryAdd(marketPrice.Symbol, marketPrice))
            {
                return null;
            }
        }
    }

    public IReadOnlyCollection<MarketPrice> GetSnapshot() =>
        _prices.Values.ToArray();
}
