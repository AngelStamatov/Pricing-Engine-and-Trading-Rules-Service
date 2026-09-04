using PricingAndTrading.Domain.Orders;
using PricingAndTrading.Domain.Prices;
using TradingRulesConfiguration = PricingAndTrading.Domain.TradingRules.TradingRules;

namespace PricingAndTrading.Application.AutoTrading;

public interface IAutoTradingEngine
{
    Order? Evaluate(
        MarketPrice? previous,
        MarketPrice current,
        TradingRulesConfiguration tradingRules);
}
