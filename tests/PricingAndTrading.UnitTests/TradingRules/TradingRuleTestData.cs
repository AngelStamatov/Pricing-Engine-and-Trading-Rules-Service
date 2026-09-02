using PricingAndTrading.Domain.Orders;
using PricingAndTrading.Domain.Prices;
using TradingRulesConfiguration = PricingAndTrading.Domain.TradingRules.TradingRules;

namespace PricingAndTrading.UnitTests.TradingRules;

internal static class TradingRuleTestData
{
    private static readonly Guid OrderId =
        Guid.Parse("b2c78f51-d3ee-460e-897f-50605e6e918d");

    private static readonly DateTimeOffset Timestamp =
        new(2026, 9, 2, 14, 0, 0, TimeSpan.Zero);

    public static Order CreateOrder(
        decimal price = 100m,
        decimal quantity = 5m,
        string symbol = "EURUSD")
    {
        return new Order(
            OrderId,
            symbol,
            OrderSide.Buy,
            OrderType.Limit,
            price,
            quantity,
            OrderSource.Api,
            Timestamp);
    }

    public static MarketPrice CreateMarketPrice(
        string symbol = "EURUSD",
        decimal bidPrice = 99m,
        decimal askPrice = 101m)
    {
        return MarketPrice.From(
            new PriceTick(symbol, bidPrice, askPrice, Timestamp));
    }

    public static TradingRulesConfiguration CreateTradingRules(
        decimal maximumNotionalAmount = 100_000m,
        decimal maximumQuantity = 1_000m,
        decimal maximumPriceDeviationPercent = 0.8m,
        bool symbolWhitelistEnabled = false,
        IEnumerable<string>? symbolWhitelist = null)
    {
        return new TradingRulesConfiguration(
            maximumNotionalAmount,
            maximumQuantity,
            duplicateOrderIdCheckEnabled: true,
            symbolWhitelistEnabled,
            symbolWhitelist,
            autoTradingSpreadThresholdPercent: 0.25m,
            maximumPriceDeviationPercent);
    }
}
