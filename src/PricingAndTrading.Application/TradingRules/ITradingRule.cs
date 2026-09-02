using PricingAndTrading.Domain.Orders;
using PricingAndTrading.Domain.Prices;
using TradingRulesConfiguration = PricingAndTrading.Domain.TradingRules.TradingRules;

namespace PricingAndTrading.Application.TradingRules;

public interface ITradingRule
{
    RejectionReason? Evaluate(
        Order order,
        MarketPrice marketPrice,
        TradingRulesConfiguration tradingRules);
}
