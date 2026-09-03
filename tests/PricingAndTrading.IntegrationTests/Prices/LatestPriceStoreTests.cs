using PricingAndTrading.Domain.Prices;
using PricingAndTrading.Infrastructure.RuntimeState;

namespace PricingAndTrading.IntegrationTests.Prices;

public sealed class LatestPriceStoreTests
{
    private static readonly DateTimeOffset Timestamp =
        new(2026, 9, 3, 12, 30, 0, TimeSpan.Zero);

    [Fact]
    public void GetLatest_UnknownSymbol_ReturnsNull()
    {
        var store = new LatestPriceStore();

        MarketPrice? result = store.GetLatest("EURUSD");

        Assert.Null(result);
    }

    [Fact]
    public void Update_NewSymbol_StoresPriceAndReturnsNull()
    {
        var store = new LatestPriceStore();
        MarketPrice price = CreateMarketPrice("EURUSD", 99m, 101m);

        MarketPrice? previous = store.Update(price);

        Assert.Null(previous);
        Assert.Same(price, store.GetLatest("EURUSD"));
    }

    [Fact]
    public void Update_ExistingSymbol_ReplacesPriceAndReturnsPreviousPrice()
    {
        var store = new LatestPriceStore();
        MarketPrice first = CreateMarketPrice("EURUSD", 99m, 101m);
        MarketPrice second = CreateMarketPrice("EURUSD", 100m, 102m);
        store.Update(first);

        MarketPrice? previous = store.Update(second);

        Assert.Same(first, previous);
        Assert.Same(second, store.GetLatest("EURUSD"));
    }

    [Fact]
    public void Update_DifferentSymbols_StoresPricesIndependently()
    {
        var store = new LatestPriceStore();
        MarketPrice eurUsd = CreateMarketPrice("EURUSD", 99m, 101m);
        MarketPrice gbpUsd = CreateMarketPrice("GBPUSD", 119m, 121m);

        store.Update(eurUsd);
        store.Update(gbpUsd);

        Assert.Same(eurUsd, store.GetLatest("EURUSD"));
        Assert.Same(gbpUsd, store.GetLatest("GBPUSD"));
    }

    [Fact]
    public void GetLatest_UnnormalizedSymbol_ReturnsNormalizedSymbolPrice()
    {
        var store = new LatestPriceStore();
        MarketPrice price = CreateMarketPrice("EURUSD", 99m, 101m);
        store.Update(price);

        MarketPrice? result = store.GetLatest("  eurusd  ");

        Assert.Same(price, result);
    }

    [Fact]
    public async Task Update_ConcurrentUpdates_PreservesAtomicReplacementSemantics()
    {
        const int updateCount = 20;
        var store = new LatestPriceStore();
        MarketPrice[] prices = Enumerable.Range(0, updateCount)
            .Select(index => CreateMarketPrice("EURUSD", 100m + index, 101m + index))
            .ToArray();

        Task<MarketPrice?>[] updates = prices
            .Select(price => Task.Run(() => store.Update(price)))
            .ToArray();

        MarketPrice?[] previousPrices = await Task.WhenAll(updates);

        Assert.Single(previousPrices, price => price is null);
        Assert.Contains(store.GetLatest("EURUSD"), prices);
    }

    private static MarketPrice CreateMarketPrice(
        string symbol,
        decimal bidPrice,
        decimal askPrice) =>
        MarketPrice.From(new PriceTick(symbol, bidPrice, askPrice, Timestamp));
}
