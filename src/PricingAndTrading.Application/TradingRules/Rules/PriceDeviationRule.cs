using PricingAndTrading.Domain.Orders;
using PricingAndTrading.Domain.Prices;
using TradingRulesConfiguration = PricingAndTrading.Domain.TradingRules.TradingRules;

namespace PricingAndTrading.Application.TradingRules.Rules;

public sealed class PriceDeviationRule : ITradingRule
{
    public RejectionReason? Evaluate(
        Order order,
        MarketPrice marketPrice,
        TradingRulesConfiguration tradingRules)
    {
        ArgumentNullException.ThrowIfNull(order);
        ArgumentNullException.ThrowIfNull(marketPrice);
        ArgumentNullException.ThrowIfNull(tradingRules);

        if (!string.Equals(
                order.Symbol,
                marketPrice.Symbol,
                StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "Order and market price symbols must match.",
                nameof(marketPrice));
        }

        decimal deviationPercent =
            Math.Abs(order.Price - marketPrice.CurrentMarketPrice)
            / marketPrice.CurrentMarketPrice
            * 100m;

        if (deviationPercent <= tradingRules.MaximumPriceDeviationPercent)
        {
            return null;
        }

        string message = FormattableString.Invariant(
            $"Order price deviation {deviationPercent}% exceeds the maximum {tradingRules.MaximumPriceDeviationPercent}%.");

        return new RejectionReason("PriceDeviationExceeded", message);
    }
}
