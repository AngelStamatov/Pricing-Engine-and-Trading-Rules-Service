using PricingAndTrading.Domain.Prices;

namespace PricingAndTrading.UnitTests.Pricing;

public sealed class MarketPriceTests
{
    private static readonly DateTimeOffset Timestamp =
        new(2026, 9, 2, 12, 30, 0, TimeSpan.Zero);

    [Fact]
    public void From_PriceTick_CalculatesCurrentMarketPrice()
    {
        var priceTick = new PriceTick("EURUSD", 99m, 101m, Timestamp);

        MarketPrice marketPrice = MarketPrice.From(priceTick);

        Assert.Equal(100m, marketPrice.CurrentMarketPrice);
    }

    [Fact]
    public void From_PriceTick_CalculatesSpread()
    {
        var priceTick = new PriceTick("EURUSD", 99m, 101m, Timestamp);

        MarketPrice marketPrice = MarketPrice.From(priceTick);

        Assert.Equal(2m, marketPrice.Spread);
    }

    [Fact]
    public void From_PriceTick_CalculatesSpreadPercent()
    {
        var priceTick = new PriceTick("EURUSD", 99m, 101m, Timestamp);

        MarketPrice marketPrice = MarketPrice.From(priceTick);

        Assert.Equal(2m, marketPrice.SpreadPercent);
    }

    [Fact]
    public void From_PriceTick_PreservesSourceValues()
    {
        var priceTick = new PriceTick("EURUSD", 99m, 101m, Timestamp);

        MarketPrice marketPrice = MarketPrice.From(priceTick);

        Assert.Equal(priceTick.Symbol, marketPrice.Symbol);
        Assert.Equal(priceTick.BidPrice, marketPrice.BidPrice);
        Assert.Equal(priceTick.AskPrice, marketPrice.AskPrice);
        Assert.Equal(priceTick.Timestamp, marketPrice.Timestamp);
    }
}
