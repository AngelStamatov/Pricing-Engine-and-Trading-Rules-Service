using TradingRulesConfiguration = PricingAndTrading.Domain.TradingRules.TradingRules;

namespace PricingAndTrading.Application.TradingRules;

public interface ITradingRulesProvider
{
    TradingRulesConfiguration Current { get; }
}
