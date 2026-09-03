using PricingAndTrading.Domain.Prices;
using PricingAndTrading.Infrastructure.Pricing;

namespace PricingAndTrading.IntegrationTests.Prices;

public sealed class SimulatedPriceGeneratorTests
{
    [Fact]
    public void Generate_ValidMarketPrice_ReturnsValidTickForRequestedSymbol()
    {
        var generator = new SimulatedPriceGenerator();
        decimal marketPrice = 100m;

        PriceTick result = generator.Generate("EURUSD", marketPrice);

        Assert.Equal("EURUSD", result.Symbol);
        Assert.True(result.BidPrice > 0m);
        Assert.True(result.AskPrice > 0m);
        Assert.True(result.BidPrice < result.AskPrice);
        Assert.NotEqual(default, result.Timestamp);
        Assert.Equal(TimeSpan.Zero, result.Timestamp.Offset);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Generate_NonPositiveMarketPrice_ThrowsArgumentOutOfRangeException(
        decimal marketPrice)
    {
        var generator = new SimulatedPriceGenerator();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            generator.Generate("EURUSD", marketPrice));
    }
}
