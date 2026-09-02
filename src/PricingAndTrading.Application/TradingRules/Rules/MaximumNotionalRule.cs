using PricingAndTrading.Domain.Orders;
using PricingAndTrading.Domain.Prices;
using TradingRulesConfiguration = PricingAndTrading.Domain.TradingRules.TradingRules;

namespace PricingAndTrading.Application.TradingRules.Rules;

public sealed class MaximumNotionalRule : ITradingRule
{
    public RejectionReason? Evaluate(
        Order order,
        MarketPrice marketPrice,
        TradingRulesConfiguration tradingRules)
    {
        ArgumentNullException.ThrowIfNull(order);
        ArgumentNullException.ThrowIfNull(marketPrice);
        ArgumentNullException.ThrowIfNull(tradingRules);

        decimal notionalAmount = order.Price * order.Quantity;

        if (notionalAmount <= tradingRules.MaximumNotionalAmount)
        {
            return null;
        }

        string message = FormattableString.Invariant(
            $"Order notional amount {notionalAmount} exceeds the maximum {tradingRules.MaximumNotionalAmount}.");

        return new RejectionReason("MaximumNotionalExceeded", message);
    }
}
