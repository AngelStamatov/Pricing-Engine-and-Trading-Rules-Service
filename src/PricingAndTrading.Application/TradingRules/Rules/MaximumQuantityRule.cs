using PricingAndTrading.Domain.Orders;
using PricingAndTrading.Domain.Prices;
using TradingRulesConfiguration = PricingAndTrading.Domain.TradingRules.TradingRules;

namespace PricingAndTrading.Application.TradingRules.Rules;

public sealed class MaximumQuantityRule : ITradingRule
{
    public RejectionReason? Evaluate(
        Order order,
        MarketPrice marketPrice,
        TradingRulesConfiguration tradingRules)
    {
        ArgumentNullException.ThrowIfNull(order);
        ArgumentNullException.ThrowIfNull(marketPrice);
        ArgumentNullException.ThrowIfNull(tradingRules);

        if (order.Quantity <= tradingRules.MaximumQuantity)
        {
            return null;
        }

        string message = FormattableString.Invariant(
            $"Order quantity {order.Quantity} exceeds the maximum {tradingRules.MaximumQuantity}.");

        return new RejectionReason("MaximumQuantityExceeded", message);
    }
}
