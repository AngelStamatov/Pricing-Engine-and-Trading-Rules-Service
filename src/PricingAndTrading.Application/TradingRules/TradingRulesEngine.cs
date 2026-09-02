using PricingAndTrading.Application.TradingRules.Rules;
using PricingAndTrading.Domain.Orders;
using PricingAndTrading.Domain.Prices;
using TradingRulesConfiguration = PricingAndTrading.Domain.TradingRules.TradingRules;

namespace PricingAndTrading.Application.TradingRules;

public sealed class TradingRulesEngine : ITradingRulesEngine
{
    private static readonly ITradingRule[] OrderedRules =
    [
        new MaximumNotionalRule(),
        new MaximumQuantityRule(),
        new PriceDeviationRule(),
        new SymbolWhitelistRule()
    ];

    public TradeValidationResult Evaluate(
        Order order,
        MarketPrice marketPrice,
        TradingRulesConfiguration tradingRules)
    {
        ArgumentNullException.ThrowIfNull(order);
        ArgumentNullException.ThrowIfNull(marketPrice);
        ArgumentNullException.ThrowIfNull(tradingRules);

        var rejectionReasons = new List<RejectionReason>(OrderedRules.Length);

        foreach (ITradingRule rule in OrderedRules)
        {
            RejectionReason? rejectionReason =
                rule.Evaluate(order, marketPrice, tradingRules);

            if (rejectionReason is not null)
            {
                rejectionReasons.Add(rejectionReason);
            }
        }

        return rejectionReasons.Count == 0
            ? TradeValidationResult.Valid()
            : TradeValidationResult.Invalid(rejectionReasons.ToArray());
    }
}
