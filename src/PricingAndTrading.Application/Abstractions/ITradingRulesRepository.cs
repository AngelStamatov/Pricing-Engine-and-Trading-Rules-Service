using TradingRulesConfiguration = PricingAndTrading.Domain.TradingRules.TradingRules;

namespace PricingAndTrading.Application.Abstractions;

public interface ITradingRulesRepository
{
    Task<TradingRulesConfiguration?> GetAsync(
        CancellationToken cancellationToken);

    Task SaveAsync(
        TradingRulesConfiguration tradingRules,
        CancellationToken cancellationToken);
}
