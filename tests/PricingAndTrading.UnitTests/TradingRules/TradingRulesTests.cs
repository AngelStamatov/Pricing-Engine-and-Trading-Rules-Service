using TradingRulesConfiguration = PricingAndTrading.Domain.TradingRules.TradingRules;

namespace PricingAndTrading.UnitTests.TradingRules;

public sealed class TradingRulesTests
{
    [Fact]
    public void Constructor_ValidValues_CreatesConfiguration()
    {
        var rules = new TradingRulesConfiguration(
            maximumNotionalAmount: 100_000m,
            maximumQuantity: 1_000m,
            duplicateOrderIdCheckEnabled: true,
            symbolWhitelistEnabled: true,
            symbolWhitelist: ["EURUSD", "MSFT"],
            autoTradingSpreadThresholdPercent: 0.25m,
            maximumPriceDeviationPercent: 1.5m);

        Assert.Equal(100_000m, rules.MaximumNotionalAmount);
        Assert.Equal(1_000m, rules.MaximumQuantity);
        Assert.Equal(1.5m, rules.MaximumPriceDeviationPercent);
        Assert.True(rules.DuplicateOrderIdCheckEnabled);
        Assert.True(rules.SymbolWhitelistEnabled);
        Assert.Equal(["EURUSD", "MSFT"], rules.SymbolWhitelist);
        Assert.Equal(0.25m, rules.AutoTradingSpreadThresholdPercent);
    }

    [Fact]
    public void Constructor_MaximumPriceDeviationPercentNotProvided_DefaultsToPointEightPercent()
    {
        var rules = new TradingRulesConfiguration(
            maximumNotionalAmount: 100_000m,
            maximumQuantity: 1_000m,
            duplicateOrderIdCheckEnabled: true,
            symbolWhitelistEnabled: false,
            symbolWhitelist: null,
            autoTradingSpreadThresholdPercent: 0.25m);

        Assert.Equal(0.8m, rules.MaximumPriceDeviationPercent);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_NonPositiveMaximumNotionalAmount_ThrowsArgumentOutOfRangeException(
        int invalidMaximumNotionalAmount)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new TradingRulesConfiguration(
                invalidMaximumNotionalAmount,
                1_000m,
                true,
                false,
                null,
                0.25m));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_NonPositiveMaximumQuantity_ThrowsArgumentOutOfRangeException(
        int invalidMaximumQuantity)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new TradingRulesConfiguration(
                100_000m,
                invalidMaximumQuantity,
                true,
                false,
                null,
                0.25m));
    }

    [Fact]
    public void Constructor_NegativeMaximumPriceDeviationPercent_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new TradingRulesConfiguration(
                100_000m,
                1_000m,
                true,
                false,
                null,
                0.25m,
                -0.1m));
    }

    [Fact]
    public void Constructor_NegativeAutoTradingSpreadThresholdPercent_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new TradingRulesConfiguration(
                100_000m,
                1_000m,
                true,
                false,
                null,
                -0.1m));
    }

    [Fact]
    public void Constructor_EnabledWhitelistWithoutSymbols_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(
            () => new TradingRulesConfiguration(
                100_000m,
                1_000m,
                true,
                true,
                [],
                0.25m));
    }

    [Fact]
    public void Constructor_UnnormalizedWhitelistSymbols_NormalizesAndDeduplicatesSymbols()
    {
        var rules = new TradingRulesConfiguration(
            100_000m,
            1_000m,
            true,
            true,
            [" msft ", "EURusd", "MSFT"],
            0.25m);

        Assert.Equal(["EURUSD", "MSFT"], rules.SymbolWhitelist);
    }

    [Fact]
    public void Constructor_SourceWhitelistChanges_PreservesImmutableSnapshot()
    {
        var sourceWhitelist = new List<string> { "EURUSD" };
        var rules = new TradingRulesConfiguration(
            100_000m,
            1_000m,
            true,
            true,
            sourceWhitelist,
            0.25m);

        sourceWhitelist[0] = "MSFT";
        sourceWhitelist.Add("AAPL");

        Assert.Equal(["EURUSD"], rules.SymbolWhitelist);
    }
}
