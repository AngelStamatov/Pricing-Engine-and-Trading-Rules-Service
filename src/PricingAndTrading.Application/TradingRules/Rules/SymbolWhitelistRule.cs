using PricingAndTrading.Domain.Orders;
using PricingAndTrading.Domain.Prices;
using TradingRulesConfiguration = PricingAndTrading.Domain.TradingRules.TradingRules;

namespace PricingAndTrading.Application.TradingRules.Rules;

public sealed class SymbolWhitelistRule : ITradingRule
{
    public RejectionReason? Evaluate(
        Order order,
        MarketPrice marketPrice,
        TradingRulesConfiguration tradingRules)
    {
        ArgumentNullException.ThrowIfNull(order);
        ArgumentNullException.ThrowIfNull(marketPrice);
        ArgumentNullException.ThrowIfNull(tradingRules);

        if (!tradingRules.SymbolWhitelistEnabled
            || tradingRules.SymbolWhitelist.Contains(order.Symbol))
        {
            return null;
        }

        string message = FormattableString.Invariant(
            $"Symbol '{order.Symbol}' is not included in the active whitelist.");

        return new RejectionReason("SymbolNotWhitelisted", message);
    }
}
