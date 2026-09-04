using TradingRulesConfiguration = PricingAndTrading.Domain.TradingRules.TradingRules;

namespace PricingAndTrading.Application.TradingRules;

public interface ITradingRulesStore : ITradingRulesProvider
{
    void Update(TradingRulesConfiguration tradingRules);
}
