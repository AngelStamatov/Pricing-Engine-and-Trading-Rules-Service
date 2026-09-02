using PricingAndTrading.Domain.Prices;

namespace PricingAndTrading.UnitTests.Pricing;

public sealed class PriceTickTests
{
    private static readonly DateTimeOffset Timestamp =
        new(2026, 9, 2, 12, 30, 0, TimeSpan.Zero);

    [Fact]
    public void Constructor_ValidValues_CreatesPriceTick()
    {
        var priceTick = new PriceTick("EURUSD", 1.10m, 1.12m, Timestamp);

        Assert.Equal("EURUSD", priceTick.Symbol);
        Assert.Equal(1.10m, priceTick.BidPrice);
        Assert.Equal(1.12m, priceTick.AskPrice);
        Assert.Equal(Timestamp, priceTick.Timestamp);
    }

    [Fact]
    public void Constructor_BidPriceEqualsAskPrice_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(
            () => new PriceTick("EURUSD", 1.10m, 1.10m, Timestamp));
    }

    [Fact]
    public void Constructor_BidPriceExceedsAskPrice_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(
            () => new PriceTick("EURUSD", 1.11m, 1.10m, Timestamp));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_NonPositiveBidPrice_ThrowsArgumentOutOfRangeException(
        int invalidBidPrice)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new PriceTick("EURUSD", invalidBidPrice, 1.10m, Timestamp));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_NonPositiveAskPrice_ThrowsArgumentOutOfRangeException(
        int invalidAskPrice)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new PriceTick("EURUSD", 1.10m, invalidAskPrice, Timestamp));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_InvalidSymbol_ThrowsArgumentException(string? invalidSymbol)
    {
        Assert.ThrowsAny<ArgumentException>(
            () => new PriceTick(invalidSymbol!, 1.10m, 1.12m, Timestamp));
    }
}
