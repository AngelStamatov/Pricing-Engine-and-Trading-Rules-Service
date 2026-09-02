using PricingAndTrading.Application.TradingRules.Rules;
using PricingAndTrading.Domain.Orders;
using PricingAndTrading.Domain.Prices;
using TradingRulesConfiguration = PricingAndTrading.Domain.TradingRules.TradingRules;

namespace PricingAndTrading.UnitTests.TradingRules;

public sealed class SymbolWhitelistRuleTests
{
    private readonly SymbolWhitelistRule _rule = new();
    private readonly MarketPrice _marketPrice =
        TradingRuleTestData.CreateMarketPrice();

    [Fact]
    public void Evaluate_WhitelistDisabledAndSymbolUnlisted_ReturnsNoRejection()
    {
        Order order = TradingRuleTestData.CreateOrder(symbol: "EURUSD");
        TradingRulesConfiguration rules =
            TradingRuleTestData.CreateTradingRules(
                symbolWhitelistEnabled: false,
                symbolWhitelist: ["MSFT"]);

        RejectionReason? rejection = _rule.Evaluate(order, _marketPrice, rules);

        Assert.Null(rejection);
    }

    [Fact]
    public void Evaluate_WhitelistEnabledAndSymbolListed_ReturnsNoRejection()
    {
        Order order = TradingRuleTestData.CreateOrder(symbol: "EURUSD");
        TradingRulesConfiguration rules =
            TradingRuleTestData.CreateTradingRules(
                symbolWhitelistEnabled: true,
                symbolWhitelist: ["EURUSD"]);

        RejectionReason? rejection = _rule.Evaluate(order, _marketPrice, rules);

        Assert.Null(rejection);
    }

    [Fact]
    public void Evaluate_WhitelistEnabledAndSymbolUnlisted_ReturnsStructuredRejection()
    {
        Order order = TradingRuleTestData.CreateOrder(symbol: "EURUSD");
        TradingRulesConfiguration rules =
            TradingRuleTestData.CreateTradingRules(
                symbolWhitelistEnabled: true,
                symbolWhitelist: ["MSFT"]);

        RejectionReason rejection = Assert.IsType<RejectionReason>(
            _rule.Evaluate(order, _marketPrice, rules));

        Assert.Equal("SymbolNotWhitelisted", rejection.Code);
        Assert.NotEmpty(rejection.Message);
    }
}
