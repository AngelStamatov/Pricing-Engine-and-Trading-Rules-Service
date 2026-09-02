using PricingAndTrading.Application.TradingRules;
using PricingAndTrading.Domain.Orders;
using PricingAndTrading.Domain.Prices;
using TradingRulesConfiguration = PricingAndTrading.Domain.TradingRules.TradingRules;

namespace PricingAndTrading.UnitTests.TradingRules;

public sealed class TradingRulesEngineTests
{
    private readonly ITradingRulesEngine _engine = new TradingRulesEngine();
    private readonly MarketPrice _marketPrice =
        TradingRuleTestData.CreateMarketPrice();

    [Fact]
    public void Evaluate_AllRulesPass_ReturnsValidResultWithoutRejectionReasons()
    {
        Order order = TradingRuleTestData.CreateOrder(price: 100m, quantity: 5m);
        TradingRulesConfiguration rules =
            TradingRuleTestData.CreateTradingRules(
                maximumNotionalAmount: 500m,
                maximumQuantity: 5m,
                maximumPriceDeviationPercent: 0.8m,
                symbolWhitelistEnabled: true,
                symbolWhitelist: ["EURUSD"]);

        TradeValidationResult result =
            _engine.Evaluate(order, _marketPrice, rules);

        Assert.True(result.IsValid);
        Assert.Empty(result.RejectionReasons);
    }

    [Fact]
    public void Evaluate_OneRuleFails_ReturnsInvalidResultWithCorrectReason()
    {
        Order order = TradingRuleTestData.CreateOrder(price: 100m, quantity: 5m);
        TradingRulesConfiguration rules =
            TradingRuleTestData.CreateTradingRules(
                maximumNotionalAmount: 10_000m,
                maximumQuantity: 4m,
                maximumPriceDeviationPercent: 0.8m);

        TradeValidationResult result =
            _engine.Evaluate(order, _marketPrice, rules);

        Assert.False(result.IsValid);
        RejectionReason reason = Assert.Single(result.RejectionReasons);
        Assert.Equal("MaximumQuantityExceeded", reason.Code);
    }

    [Fact]
    public void Evaluate_MultipleRulesFail_ReturnsAllReasonsInDeterministicOrder()
    {
        Order order = TradingRuleTestData.CreateOrder(price: 102m, quantity: 10m);
        TradingRulesConfiguration rules =
            TradingRuleTestData.CreateTradingRules(
                maximumNotionalAmount: 500m,
                maximumQuantity: 4m,
                maximumPriceDeviationPercent: 0.5m,
                symbolWhitelistEnabled: true,
                symbolWhitelist: ["MSFT"]);

        TradeValidationResult result =
            _engine.Evaluate(order, _marketPrice, rules);

        Assert.False(result.IsValid);
        Assert.Equal(
            [
                "MaximumNotionalExceeded",
                "MaximumQuantityExceeded",
                "PriceDeviationExceeded",
                "SymbolNotWhitelisted"
            ],
            result.RejectionReasons.Select(static reason => reason.Code));
    }

    [Fact]
    public void Evaluate_MismatchedSymbols_ThrowsArgumentException()
    {
        Order order = TradingRuleTestData.CreateOrder(symbol: "EURUSD");
        MarketPrice marketPrice =
            TradingRuleTestData.CreateMarketPrice(symbol: "MSFT");
        TradingRulesConfiguration rules =
            TradingRuleTestData.CreateTradingRules();

        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => _engine.Evaluate(order, marketPrice, rules));

        Assert.Equal("marketPrice", exception.ParamName);
    }
}
