using PricingAndTrading.Application.Pricing;
using PricingAndTrading.Domain.Prices;

namespace PricingAndTrading.UnitTests.Pricing;

public sealed class PriceProcessorTests
{
    private static readonly DateTimeOffset Timestamp =
        new(2026, 9, 3, 12, 30, 0, TimeSpan.Zero);

    [Fact]
    public async Task ProcessAsync_ValidPriceTick_ConvertsAndUpdatesLatestPriceOnce()
    {
        var store = new RecordingLatestPriceStore();
        var processor = new PriceProcessor(store);
        var priceTick = new PriceTick("EURUSD", 99m, 101m, Timestamp);

        await processor.ProcessAsync(priceTick);

        MarketPrice marketPrice = Assert.IsType<MarketPrice>(store.UpdatedPrice);
        Assert.Equal(1, store.UpdateCount);
        Assert.Equal(priceTick.Symbol, marketPrice.Symbol);
        Assert.Equal(priceTick.BidPrice, marketPrice.BidPrice);
        Assert.Equal(priceTick.AskPrice, marketPrice.AskPrice);
        Assert.Equal(priceTick.Timestamp, marketPrice.Timestamp);
        Assert.Equal(100m, marketPrice.CurrentMarketPrice);
        Assert.Equal(2m, marketPrice.Spread);
        Assert.Equal(2m, marketPrice.SpreadPercent);
    }

    [Fact]
    public async Task ProcessAsync_NullPriceTick_ThrowsArgumentNullException()
    {
        var processor = new PriceProcessor(new RecordingLatestPriceStore());

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            processor.ProcessAsync(null!).AsTask());
    }

    private sealed class RecordingLatestPriceStore : ILatestPriceStore
    {
        public int UpdateCount { get; private set; }

        public MarketPrice? UpdatedPrice { get; private set; }

        public MarketPrice? GetLatest(string symbol) => UpdatedPrice;

        public MarketPrice? Update(MarketPrice marketPrice)
        {
            UpdateCount++;
            UpdatedPrice = marketPrice;
            return null;
        }
    }
}
