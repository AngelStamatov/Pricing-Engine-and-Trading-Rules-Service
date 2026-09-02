using PricingAndTrading.Application.TradingRules.Rules;
using PricingAndTrading.Domain.Orders;
using PricingAndTrading.Domain.Prices;
using TradingRulesConfiguration = PricingAndTrading.Domain.TradingRules.TradingRules;

namespace PricingAndTrading.UnitTests.TradingRules;

public sealed class PriceDeviationRuleTests
{
    private readonly PriceDeviationRule _rule = new();
    private readonly MarketPrice _marketPrice =
        TradingRuleTestData.CreateMarketPrice();

    [Fact]
    public void Evaluate_DeviationBelowThreshold_ReturnsNoRejection()
    {
        Order order = TradingRuleTestData.CreateOrder(price: 100.79m);
        TradingRulesConfiguration rules =
            TradingRuleTestData.CreateTradingRules(
                maximumPriceDeviationPercent: 0.8m);

        RejectionReason? rejection = _rule.Evaluate(order, _marketPrice, rules);

        Assert.Null(rejection);
    }

    [Fact]
    public void Evaluate_DeviationEqualsThreshold_ReturnsNoRejection()
    {
        Order order = TradingRuleTestData.CreateOrder(price: 100.8m);
        TradingRulesConfiguration rules =
            TradingRuleTestData.CreateTradingRules(
                maximumPriceDeviationPercent: 0.8m);

        RejectionReason? rejection = _rule.Evaluate(order, _marketPrice, rules);

        Assert.Null(rejection);
    }

    [Fact]
    public void Evaluate_PriceAboveMarketAndDeviationExceedsThreshold_ReturnsStructuredRejection()
    {
        Order order = TradingRuleTestData.CreateOrder(price: 100.81m);
        TradingRulesConfiguration rules =
            TradingRuleTestData.CreateTradingRules(
                maximumPriceDeviationPercent: 0.8m);

        RejectionReason rejection = Assert.IsType<RejectionReason>(
            _rule.Evaluate(order, _marketPrice, rules));

        Assert.Equal("PriceDeviationExceeded", rejection.Code);
        Assert.NotEmpty(rejection.Message);
    }

    [Fact]
    public void Evaluate_PriceBelowMarketAndDeviationExceedsThreshold_ReturnsStructuredRejection()
    {
        Order order = TradingRuleTestData.CreateOrder(price: 99.19m);
        TradingRulesConfiguration rules =
            TradingRuleTestData.CreateTradingRules(
                maximumPriceDeviationPercent: 0.8m);

        RejectionReason rejection = Assert.IsType<RejectionReason>(
            _rule.Evaluate(order, _marketPrice, rules));

        Assert.Equal("PriceDeviationExceeded", rejection.Code);
        Assert.NotEmpty(rejection.Message);
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
            () => _rule.Evaluate(order, marketPrice, rules));

        Assert.Equal("marketPrice", exception.ParamName);
    }
}
