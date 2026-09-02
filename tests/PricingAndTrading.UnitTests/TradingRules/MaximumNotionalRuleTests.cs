using PricingAndTrading.Application.TradingRules.Rules;
using PricingAndTrading.Domain.Orders;
using PricingAndTrading.Domain.Prices;
using TradingRulesConfiguration = PricingAndTrading.Domain.TradingRules.TradingRules;

namespace PricingAndTrading.UnitTests.TradingRules;

public sealed class MaximumNotionalRuleTests
{
    private readonly MaximumNotionalRule _rule = new();
    private readonly MarketPrice _marketPrice =
        TradingRuleTestData.CreateMarketPrice();

    [Fact]
    public void Evaluate_NotionalBelowLimit_ReturnsNoRejection()
    {
        Order order = TradingRuleTestData.CreateOrder(price: 99m, quantity: 10m);
        TradingRulesConfiguration rules =
            TradingRuleTestData.CreateTradingRules(maximumNotionalAmount: 1_000m);

        RejectionReason? rejection = _rule.Evaluate(order, _marketPrice, rules);

        Assert.Null(rejection);
    }

    [Fact]
    public void Evaluate_NotionalEqualsLimit_ReturnsNoRejection()
    {
        Order order = TradingRuleTestData.CreateOrder(price: 100m, quantity: 10m);
        TradingRulesConfiguration rules =
            TradingRuleTestData.CreateTradingRules(maximumNotionalAmount: 1_000m);

        RejectionReason? rejection = _rule.Evaluate(order, _marketPrice, rules);

        Assert.Null(rejection);
    }

    [Fact]
    public void Evaluate_NotionalExceedsLimit_ReturnsStructuredRejection()
    {
        Order order = TradingRuleTestData.CreateOrder(
            price: 100.01m,
            quantity: 10m);
        TradingRulesConfiguration rules =
            TradingRuleTestData.CreateTradingRules(maximumNotionalAmount: 1_000m);

        RejectionReason rejection = Assert.IsType<RejectionReason>(
            _rule.Evaluate(order, _marketPrice, rules));

        Assert.Equal("MaximumNotionalExceeded", rejection.Code);
        Assert.NotEmpty(rejection.Message);
    }
}
