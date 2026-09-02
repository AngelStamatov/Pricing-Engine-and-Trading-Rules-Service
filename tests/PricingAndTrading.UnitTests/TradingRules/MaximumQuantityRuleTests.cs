using PricingAndTrading.Application.TradingRules.Rules;
using PricingAndTrading.Domain.Orders;
using PricingAndTrading.Domain.Prices;
using TradingRulesConfiguration = PricingAndTrading.Domain.TradingRules.TradingRules;

namespace PricingAndTrading.UnitTests.TradingRules;

public sealed class MaximumQuantityRuleTests
{
    private readonly MaximumQuantityRule _rule = new();
    private readonly MarketPrice _marketPrice =
        TradingRuleTestData.CreateMarketPrice();

    [Fact]
    public void Evaluate_QuantityBelowLimit_ReturnsNoRejection()
    {
        Order order = TradingRuleTestData.CreateOrder(quantity: 9m);
        TradingRulesConfiguration rules =
            TradingRuleTestData.CreateTradingRules(maximumQuantity: 10m);

        RejectionReason? rejection = _rule.Evaluate(order, _marketPrice, rules);

        Assert.Null(rejection);
    }

    [Fact]
    public void Evaluate_QuantityEqualsLimit_ReturnsNoRejection()
    {
        Order order = TradingRuleTestData.CreateOrder(quantity: 10m);
        TradingRulesConfiguration rules =
            TradingRuleTestData.CreateTradingRules(maximumQuantity: 10m);

        RejectionReason? rejection = _rule.Evaluate(order, _marketPrice, rules);

        Assert.Null(rejection);
    }

    [Fact]
    public void Evaluate_QuantityExceedsLimit_ReturnsStructuredRejection()
    {
        Order order = TradingRuleTestData.CreateOrder(quantity: 10.01m);
        TradingRulesConfiguration rules =
            TradingRuleTestData.CreateTradingRules(maximumQuantity: 10m);

        RejectionReason rejection = Assert.IsType<RejectionReason>(
            _rule.Evaluate(order, _marketPrice, rules));

        Assert.Equal("MaximumQuantityExceeded", rejection.Code);
        Assert.NotEmpty(rejection.Message);
    }
}
