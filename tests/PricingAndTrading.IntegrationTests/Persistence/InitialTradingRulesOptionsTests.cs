using PricingAndTrading.Infrastructure.Persistence;
using TradingRulesConfiguration = PricingAndTrading.Domain.TradingRules.TradingRules;

namespace PricingAndTrading.IntegrationTests.Persistence;

public sealed class InitialTradingRulesOptionsTests
{
    [Fact]
    public void ToDomain_ValidConfiguration_CreatesDomainSnapshot()
    {
        InitialTradingRulesOptions options = CreateValidOptions();

        TradingRulesConfiguration rules = options.ToDomain();

        Assert.Equal(100_000m, rules.MaximumNotionalAmount);
        Assert.Equal(10_000m, rules.MaximumQuantity);
        Assert.Equal(0.8m, rules.MaximumPriceDeviationPercent);
        Assert.True(rules.DuplicateOrderIdCheckEnabled);
        Assert.False(rules.SymbolWhitelistEnabled);
        Assert.Equal(["EURUSD", "GBPUSD"], rules.SymbolWhitelist);
        Assert.Equal(0.02m, rules.AutoTradingSpreadThresholdPercent);
    }

    [Fact]
    public void ToDomain_MissingRequiredConfiguration_ThrowsInvalidOperationException()
    {
        var options = new InitialTradingRulesOptions
        {
            MaximumNotionalAmount = 100_000m,
            MaximumQuantity = null,
            MaximumPriceDeviationPercent = 0.8m,
            DuplicateOrderIdCheckEnabled = true,
            SymbolWhitelistEnabled = false,
            SymbolWhitelist = ["EURUSD"],
            AutoTradingSpreadThresholdPercent = 0.02m
        };

        Assert.Throws<InvalidOperationException>(options.ToDomain);
    }

    [Fact]
    public void ToDomain_InvalidDomainValue_ThrowsArgumentOutOfRangeException()
    {
        InitialTradingRulesOptions options = CreateValidOptions();
        options = new InitialTradingRulesOptions
        {
            MaximumNotionalAmount = -1m,
            MaximumQuantity = options.MaximumQuantity,
            MaximumPriceDeviationPercent = options.MaximumPriceDeviationPercent,
            DuplicateOrderIdCheckEnabled = options.DuplicateOrderIdCheckEnabled,
            SymbolWhitelistEnabled = options.SymbolWhitelistEnabled,
            SymbolWhitelist = options.SymbolWhitelist,
            AutoTradingSpreadThresholdPercent =
                options.AutoTradingSpreadThresholdPercent
        };

        Assert.Throws<ArgumentOutOfRangeException>(options.ToDomain);
    }

    private static InitialTradingRulesOptions CreateValidOptions() =>
        new()
        {
            MaximumNotionalAmount = 100_000m,
            MaximumQuantity = 10_000m,
            MaximumPriceDeviationPercent = 0.8m,
            DuplicateOrderIdCheckEnabled = true,
            SymbolWhitelistEnabled = false,
            SymbolWhitelist = ["EURUSD", "GBPUSD"],
            AutoTradingSpreadThresholdPercent = 0.02m
        };
}
