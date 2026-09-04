using TradingRulesConfiguration = PricingAndTrading.Domain.TradingRules.TradingRules;

namespace PricingAndTrading.Application.TradingRules;

public interface ITradingRulesUpdater
{
    Task UpdateAsync(
        TradingRulesConfiguration tradingRules,
        CancellationToken cancellationToken = default);
}
